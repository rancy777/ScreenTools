using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace ScreenTools;

public sealed class MicrophoneCaptureService : IDisposable
{
    private const int WaveMapper = unchecked((int)0xFFFFFFFF);
    private const int CallbackFunction = 0x00030000;
    private const int MmWimData = 0x03C0;
    private readonly object _sync = new();
    private readonly WaveInProc _waveInProc;
    private readonly List<WaveBufferHandle> _buffers = [];
    private IntPtr _waveInHandle;
    private FileStream? _outputStream;
    private bool _isRecording;
    private int _dataLength;

    public MicrophoneCaptureService()
    {
        _waveInProc = OnWaveInData;
    }

    public void Dispose()
    {
        if (_isRecording)
        {
            Stop();
        }
    }

    public void Start(string outputPath)
    {
        if (_isRecording)
        {
            throw new InvalidOperationException("麦克风录制已经启动。");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? AppContext.BaseDirectory);
        _outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
        WriteWaveHeader(_outputStream, 0);

        var format = CreateWaveFormat();
        var result = waveInOpen(out _waveInHandle, WaveMapper, ref format, _waveInProc, IntPtr.Zero, CallbackFunction);
        if (result != 0 || _waveInHandle == IntPtr.Zero)
        {
            _outputStream.Dispose();
            _outputStream = null;
            throw new InvalidOperationException($"麦克风设备打开失败，错误码 {result}。");
        }

        _dataLength = 0;
        _isRecording = true;

        for (var i = 0; i < 3; i++)
        {
            var buffer = CreateBuffer(format.nAvgBytesPerSec / 4);
            _buffers.Add(buffer);
            PrepareBuffer(buffer);
            AddBuffer(buffer);
        }

        var startResult = waveInStart(_waveInHandle);
        if (startResult != 0)
        {
            Stop();
            throw new InvalidOperationException($"麦克风录制启动失败，错误码 {startResult}。");
        }
    }

    public void Pause()
    {
        if (_waveInHandle != IntPtr.Zero && _isRecording)
        {
            waveInStop(_waveInHandle);
        }
    }

    public void Resume()
    {
        if (_waveInHandle != IntPtr.Zero && _isRecording)
        {
            waveInStart(_waveInHandle);
        }
    }

    public void Stop()
    {
        if (_waveInHandle == IntPtr.Zero)
        {
            return;
        }

        _isRecording = false;
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

        if (_outputStream is not null)
        {
            _outputStream.Seek(0, SeekOrigin.Begin);
            WriteWaveHeader(_outputStream, _dataLength);
            _outputStream.Dispose();
            _outputStream = null;
        }
    }

    private void OnWaveInData(IntPtr hdrvr, uint uMsg, IntPtr dwUser, IntPtr dwParam1, IntPtr dwParam2)
    {
        if (uMsg != MmWimData || dwParam1 == IntPtr.Zero)
        {
            return;
        }

        var header = Marshal.PtrToStructure<WAVEHDR>(dwParam1);
        if (header.dwBytesRecorded > 0 && _outputStream is not null)
        {
            var recordedBytes = new byte[header.dwBytesRecorded];
            Marshal.Copy(header.lpData, recordedBytes, 0, recordedBytes.Length);

            lock (_sync)
            {
                _outputStream.Write(recordedBytes, 0, recordedBytes.Length);
                _dataLength += recordedBytes.Length;
            }
        }

        if (!_isRecording || _waveInHandle == IntPtr.Zero)
        {
            return;
        }

        header.dwBytesRecorded = 0;
        Marshal.StructureToPtr(header, dwParam1, false);
        waveInAddBuffer(_waveInHandle, dwParam1, Marshal.SizeOf<WAVEHDR>());
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
        public ushort wFormatTag;
        public short nChannels;
        public int nSamplesPerSec;
        public int nAvgBytesPerSec;
        public short nBlockAlign;
        public short wBitsPerSample;
        public short cbSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WAVEHDR
    {
        public IntPtr lpData;
        public uint dwBufferLength;
        public uint dwBytesRecorded;
        public IntPtr dwUser;
        public uint dwFlags;
        public uint dwLoops;
        public IntPtr lpNext;
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
}
