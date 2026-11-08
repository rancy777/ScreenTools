# ScreenTools

**文档语言：** [English](README.md) · [Français](README.fr.md) · [简体中文](README.zh-CN.md)

ScreenTools 是一款实验性的 Windows 桌面工具，将屏幕截图、剪贴板历史、屏幕录制和即时回放整合到一个轻量级应用中。

> **项目状态：** 应用目前具备可用功能，但尚未达到生产就绪状态。仍需在实体硬件上执行 15/60 分钟录制矩阵、完成 Windows 10 硬件验证，并验证多显示器行为和发布流程。

## 功能

- 支持全局快捷键、格式和输出目录设置以及最近输出记录的屏幕截图。
- 剪贴板历史管理。
- 支持开始、暂停、恢复、画质档位、麦克风采集和 FFmpeg MP4 导出的屏幕录制。
- 即时回放使用循环帧缓存，内存上限为 160 MiB，并支持按时间清理旧帧和导出最近片段。
- 降低截图编码过程中的内存分配：条件允许时直接复用原始位图，缩放后的图像使用 24 位像素格式再进行 JPEG 编码。
- 设置、输出历史和剪贴板历史通过临时文件写入，再以原子替换方式更新。损坏的 JSON 文件会被单独保留，便于恢复和排查。
- 使用 NAudio/WASAPI 从 Windows 默认播放端点采集系统声音，无需 Stereo Mix。麦克风和系统声音会带时间偏移并在 MP4 导出时混合。
- 录制诊断会将已采集/跳过/失败帧数、写入的 JPEG 字节数以及进程峰值工作集/私有内存记录到录制清单。
- 支持 Per-Monitor V2 DPI 感知，区域选择为每台显示器创建独立窗口，浮动窗口会根据目标显示器定位。
- 提供用于 Microsoft Store 和侧载分发的 MSIX 打包项目。
- 提供 97 项 xUnit 测试，覆盖持久化、回放缓存限制、本地化、录制生命周期/暂停恢复、MP4 时长与音频连续性、FFmpeg 混音、音频故障隔离、真实 Windows 麦克风/WASAPI 采集，以及对每台已连接显示器的实体捕获探测。
- 提供独立性能测试工具，可测量真实桌面截图和模拟回放缓存负载。

## 尚待验证

- 在 Windows 11 和 Windows 10 实体设备上执行 15/60 分钟录制矩阵。
- 在持续负载下测量回放时长、丢帧、内存使用，以及录制与回放导出并行时的表现。
- 完成实体多显示器/高 DPI 验收矩阵（125%、150%、混合 DPI、垂直布局和 Windows 10）。
- 扩展集成测试，并自动化打包和发布流程。

WASAPI 采集设计和硬件测试见 [AUDIO_TESTING.zh-CN.md](AUDIO_TESTING.zh-CN.md)；录制稳定性矩阵见 [RECORDING_VALIDATION.zh-CN.md](RECORDING_VALIDATION.zh-CN.md)；多显示器验收矩阵和 issue #4 诊断脚本见 [MULTI_MONITOR_VALIDATION.zh-CN.md](MULTI_MONITOR_VALIDATION.zh-CN.md)；当前待办事项见 [REMAINING_WORK.md](REMAINING_WORK.md)，界面规范见 [DESIGN.md](DESIGN.md)。

## 技术栈

- C#、WPF 和面向 Windows 的 .NET 8。
- 使用 FFmpeg 编码 MP4。
- 使用 NAudio 通过 WASAPI 采集系统声音。
- 使用 xUnit 编写自动化测试。

## 构建、测试与运行

环境要求：

- Windows 10 或 Windows 11。
- .NET 8 SDK。
- MP4 导出需要 FFmpeg。应用会检查自身目录、常见安装位置（包括 Chocolatey）以及 `PATH` 中可信的目录。

请从 `ScreenTools` 解决方案目录运行以下命令，以确保项目之间的相对路径能够正确解析：

```powershell
Push-Location .\ScreenTools
try {
    dotnet restore .\ScreenTools.sln
    dotnet build .\ScreenTools.sln -c Release
    dotnet test .\ScreenTools.sln -c Release
    dotnet run --project .\ScreenTools\ScreenTools.csproj -c Release
} finally {
    Pop-Location
}
```

### 性能基准测试

从解决方案目录运行性能测试工具：

```powershell
Push-Location .\ScreenTools
try {
    dotnet run --project ..\ScreenTools.Benchmarks\ScreenTools.Benchmarks.csproj -c Release -- capture --iterations 60 --csv ..\BenchmarkResults\capture.csv
    dotnet run --project ..\ScreenTools.Benchmarks\ScreenTools.Benchmarks.csproj -c Release -- buffer --iterations 340 --csv ..\BenchmarkResults\buffer.csv
} finally {
    Pop-Location
}
```

`capture` 测量真实桌面截图、图像缩放和 JPEG 编码。`buffer` 测量回放缓存写入性能，并使用 512 KiB 的模拟帧验证内存上限。`all` 会依次运行两种测试。CSV 输出为可选项，基准测试结果不会提交到 Git。

如果没有安装 FFmpeg，应用仍可保留捕获的图像序列，但无法导出 MP4。

## 参与贡献

目前，提升可靠性比继续添加功能更有价值。请先阅读 [CONTRIBUTING.md](CONTRIBUTING.md)，查看现有问题列表，并在提交大型补丁前讨论重要的架构变更。

## 许可证

MIT。详见 [LICENSE](LICENSE)。