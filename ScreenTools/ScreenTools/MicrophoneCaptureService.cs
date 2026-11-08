using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace ScreenTools;

/// <summary>
/// Captures microphone audio to a WAV file and supports pause, resume, and finalization.
/// </summary>
public sealed class MicrophoneCaptureService : IAudioCaptureService
{
    private const int WaveMapper = unchecked((int)0xFFFFFFFF);
    private const int CallbackFunction = 0x00030000;
    private const int MmWimData = 0x03C0;
    private readonly object _sync = new();
    private readonly WaveInProc _waveInProc;
    private readonly List<WaveBufferHandle> _buffers = [];
    private readonly ManualResetEventSlim _stopCompleted = new(true);
    private IntPtr _waveInHandle;
    private FileStream? _outputStream;
    private bool _isRecording;
    private bool _isPaused;
    private int _dataLength;
    private int _failureRaised;
    private int _availableDeviceCount;
    private int _stopInProgress;
    private string? _outputPath;
    private TimeSpan _timelineOffset;

    /// <summary>
    /// Initializes microphone capture using the default Windows recording endpoint.
    /// </summary>
    public MicrophoneCaptureService()
    {
        _waveInProc = OnWaveInData;
        RefreshAvailability();
    }

    /// <summary>
    /// Gets whether the corresponding audio capture source is currently available.
    /// </summary>
    public bool IsSupported => _availableDeviceCount > 0;

    /// <inheritdoc />
    public bool IsCapturing => _waveInHandle != IntPtr.Zero && _isRecording && !_isPaused;

    /// <summary>
    /// Gets the current user-facing status of the audio capture source.
    /// </summary>
    public string AvailabilityMessage => IsSupported
        ? "麦克风将使用 Windows 音频采集 API 录制。"
        : "Windows 未检测到可用的麦克风输入设备。";

    /// <inheritdoc />
    public IReadOnlyList<AudioCaptureSegment> OutputSegments =>
        !string.IsNullOrWhiteSpace(_outputPath) && File.Exists(_outputPath)
            ? [new AudioCaptureSegment(_outputPath, _timelineOffset)]
            : Array.Empty<AudioCaptureSegment>();

    /// <inheritdoc />
    public event EventHandler<AudioCaptureFailedEventArgs>? CaptureFailed;

    /// <inheritdoc />
    public void RefreshAvailability()
    {
        _availableDeviceCount = checked((int)waveInGetNumDevs());
    }

    /// <summary>
    /// Releases native hooks, event subscriptions, and other resources owned by this component.
    /// </summary>
    public void Dispose()
    {
        Stop();
        _stopCompleted.Dispose();
    }

    /// <summary>
    /// Starts the requested capture operation when the service is in a state that permits it.
    /// </summary>
    public void Start(string outputPath, TimeSpan timelineOffset = default)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
            throw new ArgumentException("输出路径不能为空。", nameof(outputPath));
        if (_isRecording)
        {
            throw new InvalidOperationException("麦克风录制已经启动。");
        }

        RefreshAvailability();
        if (!IsSupported)
            throw new InvalidOperationException(AvailabilityMessage);

        _outputPath = Path.GetFullPath(outputPath);
        _timelineOffset = timelineOffset < TimeSpan.Zero ? TimeSpan.Zero : timelineOffset;
        _isPaused = false;
        Interlocked.Exchange(ref _failureRaised, 0);
        Directory.CreateDirectory(Path.GetDirectoryName(_outputPath) ?? AppContext.BaseDirectory);
        _outputStream = new FileStream(_outputPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
        WriteWaveHeader(_outputStream, 0);

        var format = CreateWaveFormat();
        var result = waveInOpen(out _waveInHandle, WaveMapper, ref format, _waveInProc, IntPtr.Zero, CallbackFunction);
        if (result != 0 || _waveInHandle == IntPtr.Zero)
        {
            if (_waveInHandle != IntPtr.Zero)
            {
                waveInClose(_waveInHandle);
                _waveInHandle = IntPtr.Zero;
            }
            _outputStream.Dispose();
            _outputStream = null;
            throw new InvalidOperationException($"麦克风设备打开失败，错误码 {result}。");
        }

        _dataLength = 0;
        _isRecording = true;

        try
        {
            for (var i = 0; i < 3; i++)
            {
                var buffer = CreateBuffer(format.nAvgBytesPerSec / 4);
                _buffers.Add(buffer);
                PrepareBuffer(buffer);
                AddBuffer(buffer);
            }

            var startResult = waveInStart(_waveInHandle);
            if (startResult != 0)
                throw new InvalidOperationException($"麦克风录制启动失败，错误码 {startResult}。");
        }
        catch
        {
            Stop();
            throw;
        }
    }

    /// <summary>
    /// Pauses the active audio capture without finalizing its output file.
    /// </summary>
    public void Pause()
    {
        if (_waveInHandle != IntPtr.Zero && _isRecording)
        {
            var result = waveInStop(_waveInHandle);
            if (result != 0)
                throw new InvalidOperationException($"麦克风暂停失败，错误码 {result}。");
            _isPaused = true;
        }
    }

    /// <summary>
    /// Resumes a previously paused audio capture.
    /// </summary>
    public void Resume(TimeSpan timelineOffset = default)
    {
        if (_waveInHandle != IntPtr.Zero && _isRecording)
        {
            var result = waveInStart(_waveInHandle);
            if (result != 0)
                throw new InvalidOperationException($"麦克风恢复失败，错误码 {result}。");
            _isPaused = false;
        }
    }

    /// <summary>
    /// Stops the capture loop and releases its active capture resources.
    /// </summary>
    public void Stop()
    {
        _stopCompleted.Reset();
        if (Interlocked.CompareExchange(ref _stopInProgress, 1, 0) != 0)
        {
            _stopCompleted.Wait(TimeSpan.FromSeconds(5));
            return;
        }

        try
        {
        if (_waveInHandle == IntPtr.Zero)
        {
            lock (_sync)
            {
                _outputStream?.Dispose();
                _outputStream = null;
            }
            return;
        }

        _isRecording = false;
        _isPaused = false;
        waveInStop(_waveInHandle);
        waveInReset(_waveInHandle);

        foreach (var buffer in _buffers)
        {
            waveInUnprepareHeader(_waveInHandle, buffer.HeaderPtr, Marshal.SizeOf<WAVEHDR>());
            Marshal.FreeHGlobal(buffer.DataPtr);
            Marshal.FreeHGlobal(buffer.HeaderPtr);
        }

        _buffers.Clear();
        waveInClose(_waveInHandle);
        _waveInHandle = IntPtr.Zero;

        lock (_sync)
        {
            if (_outputStream is not null)
            {
                _outputStream.Seek(0, SeekOrigin.Begin);
                WriteWaveHeader(_outputStream, _dataLength);
                _outputStream.Dispose();
                _outputStream = null;
            }
        }
        }
        finally
        {
            _stopCompleted.Set();
            Interlocked.Exchange(ref _stopInProgress, 0);
        }
    }

    private void OnWaveInData(IntPtr hdrvr, uint uMsg, IntPtr dwUser, IntPtr dwParam1, IntPtr dwParam2)
    {
        if (uMsg != MmWimData || dwParam1 == IntPtr.Zero)
        {
            return;
        }

        try
        {
            var header = Marshal.PtrToStructure<WAVEHDR>(dwParam1);
            if (header.dwBytesRecorded > 0)
            {
                var recordedBytes = new byte[header.dwBytesRecorded];
                Marshal.Copy(header.lpData, recordedBytes, 0, recordedBytes.Length);

                lock (_sync)
                {
                    if (_outputStream is not null)
                    {
                        _outputStream.Write(recordedBytes, 0, recordedBytes.Length);
                        _dataLength = checked(_dataLength + recordedBytes.Length);
                    }
                }
            }

            if (!_isRecording || _waveInHandle == IntPtr.Zero)
                return;

            header.dwBytesRecorded = 0;
            Marshal.StructureToPtr(header, dwParam1, false);
            var result = waveInAddBuffer(_waveInHandle, dwParam1, Marshal.SizeOf<WAVEHDR>());
            if (result != 0)
                throw new InvalidOperationException($"麦克风缓冲区重新提交失败，错误码 {result}。");
        }
        catch (Exception ex)
        {
            if (Interlocked.Exchange(ref _failureRaised, 1) == 0)
            {
                try { CaptureFailed?.Invoke(this, new AudioCaptureFailedEventArgs("麦克风", ex)); }
                catch { }
            }

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try { Stop(); }
                catch { }
            });
        }
    }

    private static WAVEFORMATEX CreateWaveFormat()
    {
        const short channels = 1;
        const short bitsPerSample = 16;
        const int samplesPerSecond = 44100;
        var blockAlign = (short)(channels * (bitsPerSample / 8));

        return new WAVEFORMATEX
        {
            wFormatTag = 1,
            nChannels = channels,
            nSamplesPerSec = samplesPerSecond,
            nAvgBytesPerSec = samplesPerSecond * blockAlign,
            nBlockAlign = blockAlign,
            wBitsPerSample = bitsPerSample,
            cbSize = 0
        };
    }

    private static WaveBufferHandle CreateBuffer(int size)
    {
        var dataPtr = Marshal.AllocHGlobal(size);
        var header = new WAVEHDR
        {
            lpData = dataPtr,
            dwBufferLength = (uint)size
        };

        var headerPtr = Marshal.AllocHGlobal(Marshal.SizeOf<WAVEHDR>());
        Marshal.StructureToPtr(header, headerPtr, false);
        return new WaveBufferHandle(headerPtr, dataPtr);
    }

    private void PrepareBuffer(WaveBufferHandle buffer)
    {
        var result = waveInPrepareHeader(_waveInHandle, buffer.HeaderPtr, Marshal.SizeOf<WAVEHDR>());
        if (result != 0)
        {
            throw new InvalidOperationException($"麦克风缓冲区准备失败，错误码 {result}。");
        }
    }

    private void AddBuffer(WaveBufferHandle buffer)
    {
        var result = waveInAddBuffer(_waveInHandle, buffer.HeaderPtr, Marshal.SizeOf<WAVEHDR>());
        if (result != 0)
        {
            throw new InvalidOperationException($"麦克风缓冲区提交失败，错误码 {result}。");
        }
    }

    private static void WriteWaveHeader(Stream stream, int dataLength)
    {
        using var writer = new BinaryWriter(stream, System.Text.Encoding.ASCII, leaveOpen: true);
        const short channels = 1;
        const short bitsPerSample = 16;
        const int samplesPerSecond = 44100;
        var blockAlign = (short)(channels * (bitsPerSample / 8));
        var byteRate = samplesPerSecond * blockAlign;

        writer.Write("RIFF"u8.ToArray());
        writer.Write(36 + dataLength);
        writer.Write("WAVE"u8.ToArray());
        writer.Write("fmt "u8.ToArray());
        writer.Write(16);
        writer.Write((short)1);
        writer.Write(channels);
        writer.Write(samplesPerSecond);
        writer.Write(byteRate);
        writer.Write(blockAlign);
        writer.Write(bitsPerSample);
        writer.Write("data"u8.ToArray());
        writer.Write(dataLength);
    }

    private sealed record WaveBufferHandle(IntPtr HeaderPtr, IntPtr DataPtr);

    private delegate void WaveInProc(IntPtr hdrvr, uint uMsg, IntPtr dwUser, IntPtr dwParam1, IntPtr dwParam2);

    [StructLayout(LayoutKind.Sequential)]
    private struct WAVEFORMATEX
    {
        /// <summary>
        /// Contains the wave format encoding identifier.
        /// </summary>
        public ushort wFormatTag;
        /// <summary>
        /// Contains the number of audio channels in the native wave format structure.
        /// </summary>
        public short nChannels;
        /// <summary>
        /// Contains the sample rate, in samples per second, in the native wave format structure.
        /// </summary>
        public int nSamplesPerSec;
        /// <summary>
        /// Contains the average byte rate in the native wave format structure.
        /// </summary>
        public int nAvgBytesPerSec;
        /// <summary>
        /// Contains the block alignment, in bytes, in the native wave format structure.
        /// </summary>
        public short nBlockAlign;
        /// <summary>
        /// Contains the sample bit depth in the native wave format structure.
        /// </summary>
        public short wBitsPerSample;
        /// <summary>
        /// Contains the size of any format-specific extension data.
        /// </summary>
        public short cbSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WAVEHDR
    {
        /// <summary>
        /// Points to the native audio buffer data.
        /// </summary>
        public IntPtr lpData;
        /// <summary>
        /// Contains the allocated size of the native audio buffer in bytes.
        /// </summary>
        public uint dwBufferLength;
        /// <summary>
        /// Contains the number of valid audio bytes in the native buffer.
        /// </summary>
        public uint dwBytesRecorded;
        /// <summary>
        /// Contains the application-defined user value associated with the native audio buffer.
        /// </summary>
        public IntPtr dwUser;
        /// <summary>
        /// Contains the native audio-buffer status flags.
        /// </summary>
        public uint dwFlags;
        /// <summary>
        /// Contains the remaining playback-loop count in the native audio buffer header.
        /// </summary>
        public uint dwLoops;
        /// <summary>
        /// Points to the next native audio buffer header in a linked list.
        /// </summary>
        public IntPtr lpNext;
        /// <summary>
        /// Reserved native audio-buffer field required by the Windows API layout.
        /// </summary>
        public IntPtr reserved;
    }

    [DllImport("winmm.dll")]
    private static extern int waveInOpen(out IntPtr phwi, int uDeviceID, ref WAVEFORMATEX pwfx, WaveInProc dwCallback, IntPtr dwInstance, int fdwOpen);

    [DllImport("winmm.dll")]
    private static extern int waveInPrepareHeader(IntPtr hWaveIn, IntPtr lpWaveInHdr, int uSize);

    [DllImport("winmm.dll")]
    private static extern int waveInUnprepareHeader(IntPtr hWaveIn, IntPtr lpWaveInHdr, int uSize);

    [DllImport("winmm.dll")]
    private static extern int waveInAddBuffer(IntPtr hWaveIn, IntPtr lpWaveInHdr, int uSize);

    [DllImport("winmm.dll")]
    private static extern int waveInStart(IntPtr hWaveIn);

    [DllImport("winmm.dll")]
    private static extern int waveInStop(IntPtr hWaveIn);

    [DllImport("winmm.dll")]
    private static extern int waveInReset(IntPtr hWaveIn);

    [DllImport("winmm.dll")]
    private static extern int waveInClose(IntPtr hWaveIn);

    [DllImport("winmm.dll")]
    private static extern uint waveInGetNumDevs();
}
