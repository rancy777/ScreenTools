# 为 ScreenTools 贡献代码

**文档语言：** [English](CONTRIBUTING.md) · [Français](CONTRIBUTING.fr.md) · [简体中文](CONTRIBUTING.zh-CN.md)

感谢你愿意为项目做出贡献。开始之前，请阅读 [README.md](README.md) 和 [REMAINING_WORK.md](REMAINING_WORK.md)，查看现有问题列表，并说明你计划处理的内容。涉及架构调整时，请先介绍方案及其权衡，再开始大规模修改。

## 开发环境

请从解决方案所在目录运行以下命令：

```powershell
Push-Location .\ScreenTools
try {
    dotnet restore .\ScreenTools.sln
    dotnet build .\ScreenTools.sln -c Release
    dotnet test .\ScreenTools.sln -c Release
} finally {
    Pop-Location
}
```

修改截图、编码或回放缓存代码后，请同时运行性能基准测试：

```powershell
Push-Location .\ScreenTools
try {
    dotnet run --project ..\ScreenTools.Benchmarks\ScreenTools.Benchmarks.csproj -c Release -- all --iterations 60
} finally {
    Pop-Location
}
```

请保持修改范围清晰，不要提交生成文件或机器专属文件。不要提交 `bin`、`obj`、WPF 临时项目文件、本地压缩包、录制文件、性能测试结果、个人设置或 `ffmpeg.exe`。

## Pull Request 要求

请在提交中说明：

- 要解决的问题以及采用的方案。
- 有意不纳入本次修改的内容。
- 问题复现步骤、测试步骤和测试结果。
- 与截图相关的问题应注明 Windows 版本及必要的硬件信息。
- 涉及界面变化时，请附上截图或简短录屏。
- 涉及性能变化时，请提供测量方法以及修改前后的结果。

修改录制功能时，请测试开始、停止、暂停、恢复、连续多次录制、文件播放和资源清理。修改即时回放时，请同时报告目标片段时长和实际导出时长。修改持久化功能时，请测试正常文件、损坏的 JSON、中断写入和恢复流程，并确保测试不会触碰真实用户配置。

## 项目范围与优先级

当前阶段，可靠性比增加功能更重要。主要优先事项是真实设备验收、系统声音采集、录制稳定性、即时回放验证、多显示器与 DPI 行为，以及自动化测试。