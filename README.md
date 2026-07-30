# ScreenTools

> [!IMPORTANT]
> ## 项目未完成，寻找接手者
>
> 由于个人时间有限，我目前没有时间继续开发这个项目。代码仍未完成，现将它公开开源，希望有兴趣的开发者能够继续完成它。
>
> 这个项目想做的是一个集 **截图、剪切板、录屏和回放录制** 于一体的 Windows 工具。如果你愿意接手、共同维护或完成其中一部分，请直接查看并认领 [Issues](https://github.com/rancy777/ScreenTools/issues)。

> **Incomplete project — contributors and a new maintainer wanted.** I currently do not have enough time to finish it, so the unfinished code is being released publicly in the hope that someone interested can continue the work.

ScreenTools is an experimental all-in-one Windows desktop utility for screenshots, clipboard history, screen recording, and instant replay recording. The goal is to keep these common capture workflows in one lightweight, native-feeling tool.

> **项目状态：未完成，暂停维护。** 原作者目前没有时间继续完成，欢迎感兴趣的开发者接手、贡献或共同维护。

ScreenTools 是一个实验性的 Windows 一体化桌面工具，目标是把截图、剪切板历史、录屏和回放录制整合到一个轻量、接近原生体验的应用中。

## What already works / 已有能力

- Screenshots with global shortcuts, output format/path settings, and recent-output history
- Clipboard history management
- Screen recording with start, pause, stop, quality profiles, microphone capture, and `ffmpeg` MP4 export
- Replay buffer with configurable recent-clip export
- Windows-oriented WPF interface and recording status HUD

## What is not finished / 未完成内容

This is not production-ready. The most important gaps are:

- Reliable system-audio loopback capture
- Real-world validation of long recordings, pause/resume, A/V continuity, and cleanup
- Replay duration, performance, and concurrency validation
- Multi-monitor and high-DPI testing
- Better error handling, automated tests, packaging, and releases

See [REMAINING_WORK.md](REMAINING_WORK.md) for the detailed backlog and [DESIGN.md](DESIGN.md) for the visual direction.

## Tech stack / 技术栈

- C# and WPF
- .NET 8 for Windows
- `ffmpeg` for MP4 encoding and the current system-audio capture experiment

## Build / 构建

Requirements:

- Windows 10 or Windows 11
- .NET 8 SDK or newer SDK capable of targeting .NET 8
- `ffmpeg` available in one of the locations detected by `FrameSequenceEncoder`, or on the machine in a standard installation path

```powershell
dotnet build .\ScreenTools\ScreenTools.sln
dotnet run --project .\ScreenTools\ScreenTools\ScreenTools.csproj
```

Without `ffmpeg`, capture frames may still be retained, but MP4 export and the current system-audio path will not work.

## Help wanted / 欢迎接手

The best first contribution is not another feature. It is to make one existing capture path reliable and measurable. In particular, help is wanted with:

1. A robust Windows audio loopback implementation, ideally based on WASAPI rather than device-name guessing.
2. Repeatable recording and replay validation, including long runs and pause/resume.
3. Multi-monitor and DPI correctness.
4. Tests, diagnostics, packaging, and documentation.

If you want to take ownership of a work item, open or comment on an issue before investing heavily so work does not get duplicated. Larger changes should begin with a short technical proposal.

## Project expectations / 项目约定

- Expect unfinished code and behavioral edge cases.
- Do not use this project for critical recording without validating it on your own hardware.
- Keep pull requests focused and include the Windows version, display setup, audio devices, and test steps when relevant.
- The project is open to a new maintainer. Sustained, careful contributors may be invited to help maintain it.

## License

MIT. See [LICENSE](LICENSE).
