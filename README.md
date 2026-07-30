# ScreenTools / ScreenTools / ScreenTools

> [!IMPORTANT]
> ## 项目未完成，寻找接手者 / Project incomplete — looking for maintainers / Projet incomplet — recherchons des mainteneurs
>
> 由于个人时间有限，我目前没有时间继续开发这个项目。代码仍未完成，现将它公开开源，希望有兴趣的开发者能够继续完成它。/ Due to limited personal time, I currently do not have time to continue developing this project. The code is still unfinished and is being released publicly in the hope that interested developers can continue the work./ En raison du temps personnel limité, je n'ai pas le temps de continuer à développer ce projet. Le code étant encore inachevé, il est publié en open source dans l'espoir que des développeurs intéressés puissent poursuivre le travail.
>
> 这个项目想做的是一个集 **截图、剪切板、录屏和回放录制** 于一体的 Windows 工具。如果你愿意接手、共同维护或完成其中一部分，请直接查看并认领 [Issues](https://github.com/rancy777/ScreenTools/issues)。/ This project aims to be an all-in-one Windows tool for **screenshots, clipboard, screen recording, and replay recording**. If you are willing to take over, co-maintain, or complete part of it, please check and claim [Issues](https://github.com/rancy777/ScreenTools/issues) directly./ Ce projet vise à être un outil Windows tout-en-un pour les **captures d'écran, le presse-papiers, l'enregistrement d'écran et l'enregistrement de reprise**. Si vous êtes prêt à reprendre, co-maintenir ou compléter une partie, veuillez consulter et réclamer les [Issues](https://github.com/rancy777/ScreenTools/issues) directement.

---

## About / 关于 / À propos

**中文 / Chinese:**
ScreenTools 是一个实验性的 Windows 一体化桌面工具，目标是把截图、剪切板历史、录屏和回放录制整合到一个轻量、接近原生体验的应用中。

**English:**
ScreenTools is an experimental all-in-one Windows desktop utility for screenshots, clipboard history, screen recording, and instant replay recording. The goal is to keep these common capture workflows in one lightweight, native-feeling tool.

**Français:**
ScreenTools est un utilitaire de bureau Windows expérimental tout-en-un pour les captures d'écran, l'historique du presse-papiers, l'enregistrement d'écran et l'enregistrement de reprise instantané. L'objectif est de regrouper ces flux de capture courants dans un outil léger, à l'interface native.

> **项目状态：未完成，暂停维护。/ Project status: incomplete, maintenance paused. / Statut du projet : incomplet, maintenance en pause.** 原作者目前没有时间继续完成，欢迎感兴趣的开发者接手、贡献或共同维护。/ The original author currently does not have time to finish it; interested developers are welcome to take over, contribute, or co-maintain./ L'auteur original n'a actuellement pas le temps de le terminer ; les développeurs intéressés sont les bienvenus pour reprendre, contribuer ou co-maintenir.

## What already works / 已有能力 / Fonctionnalités existantes

**中文:**
- 截图：可用，支持全局快捷键、格式选择、输出目录、最近输出记录。
- 剪切板历史管理。
- 录屏：可用，支持开始、暂停、停止、质量档位、麦克风采集、`ffmpeg` 视频导出、运行中状态反馈。
- 回录：可用，支持缓存最近片段、按当前质量档位导出、最近输出记录。
- 原生 WASAPI 回录系统音频采集（通过 NAudio）。
- Per-monitor V2 DPI 感知。
- MSIX 打包项目，支持 Windows Store / 侧载分发。
- xUnit 单元测试项目，覆盖模型与服务。

**English:**
- Screenshots with global shortcuts, output format/path settings, and recent-output history.
- Clipboard history management.
- Screen recording with start, pause, stop, quality profiles, microphone capture, and `ffmpeg` MP4 export.
- Replay buffer with configurable recent-clip export.
- Native WASAPI loopback system audio capture via NAudio.
- Per-monitor V2 DPI awareness.
- MSIX packaging project for Windows Store / sideload distribution.
- Unit test project (xUnit) with model and service coverage.

**Français:**
- Captures d'écran avec raccourcis globaux, paramètres de format/chemin de sortie et historique des sorties récentes.
- Gestion de l'historique du presse-papiers.
- Enregistrement d'écran avec démarrage, pause, arrêt, profils de qualité, capture microphone et export MP4 via `ffmpeg`.
- Tampon de reprise avec export de clip récent configurable.
- Capture audio système en boucle WASAPI native via NAudio.
- Prise en charge du DPI Per-monitor V2.
- Projet de packaging MSIX pour le Windows Store / la distribution sideload.
- Projet de tests unitaires (xUnit) couvrant les modèles et services.

## What is not finished / 未完成内容 / Ce qui n'est pas terminé

**中文:**
本项目尚未达到生产就绪状态。最重要的差距包括：
- 长时间录制、暂停/恢复、音视频连续性及清理的真实世界验证
- 回录时长、性能和并发验证
- 跨多样硬件的多显示器和高 DPI 测试
- 打包和发布自动化
- 超出当前单元测试覆盖范围的集成测试

**English:**
This is not production-ready. The most important gaps are:
- Real-world validation of long recordings, pause/resume, A/V continuity, and cleanup
- Replay duration, performance, and concurrency validation
- Multi-monitor and high-DPI testing across diverse hardware
- Packaging and release automation
- Comprehensive integration tests beyond the current unit test coverage

**Français:**
Ce projet n'est pas prêt pour la production. Les lacunes les plus importantes sont :
- Validation réelle des enregistrements longs, pause/reprise, continuité A/V et nettoyage
- Validation de la durée, des performances et de la concurrence de la reprise
- Tests multi-écrans et haute résolution sur divers matériels
- Automatisation du packaging et des versions
- Tests d'intégration complets au-delà de la couverture actuelle des tests unitaires

See [REMAINING_WORK.md](REMAINING_WORK.md) for the detailed backlog and [DESIGN.md](DESIGN.md) for the visual direction.
请参阅 [REMAINING_WORK.md](REMAINING_WORK.md) 了解详细待办事项，[DESIGN.md](DESIGN.md) 了解视觉方向。
Voir [REMAINING_WORK.md](REMAINING_WORK.md) pour la liste détaillée des tâches et [DESIGN.md](DESIGN.md) pour la direction visuelle.

## Tech stack / 技术栈 / Pile technique

**中文:**
- C# 和 WPF
- .NET 8 for Windows
- `ffmpeg` 用于 MP4 编码
- `NAudio` 用于原生 WASAPI 回录系统音频采集
- xUnit 用于单元测试

**English:**
- C# and WPF
- .NET 8 for Windows
- `ffmpeg` for MP4 encoding
- `NAudio` for native WASAPI loopback system audio capture
- xUnit for unit tests

**Français:**
- C# et WPF
- .NET 8 pour Windows
- `ffmpeg` pour l'encodage MP4
- `NAudio` pour la capture audio système en boucle WASAPI native
- xUnit pour les tests unitaires

## Build / 构建 / Compilation

**中文:**
要求：
- Windows 10 或 Windows 11
- .NET 8 SDK 或更高版本
- `ffmpeg` 位于 `FrameSequenceEncoder` 可检测到的路径之一，或机器的标准安装路径

```powershell
dotnet build .\ScreenTools\ScreenTools.sln
dotnet run --project .\ScreenTools\ScreenTools\ScreenTools.csproj
```

没有 `ffmpeg` 时，采集帧仍可保留，但 MP4 导出和系统音频路径将不可用。

**English:**
Requirements:
- Windows 10 or Windows 11
- .NET 8 SDK or newer SDK capable of targeting .NET 8
- `ffmpeg` available in one of the locations detected by `FrameSequenceEncoder`, or on the machine in a standard installation path

```powershell
dotnet build .\ScreenTools\ScreenTools.sln
dotnet run --project .\ScreenTools\ScreenTools\ScreenTools.csproj
```

Without `ffmpeg`, capture frames may still be retained, but MP4 export and the current system-audio path will not work.

**Français:**
Prérequis :
- Windows 10 ou Windows 11
- SDK .NET 8 ou version ultérieure capable de cibler .NET 8
- `ffmpeg` disponible dans l'un des emplacements détectés par `FrameSequenceEncoder`, ou sur la machine dans un chemin d'installation standard

```powershell
dotnet build .\ScreenTools\ScreenTools.sln
dotnet run --project .\ScreenTools\ScreenTools\ScreenTools.csproj
```

Sans `ffmpeg`, les frames de capture peuvent toujours être conservées, mais l'export MP4 et le chemin audio système ne fonctionneront pas.

## Help wanted / 欢迎接手 / Aide demandée

**中文:**
最好的贡献不是继续添加新功能，而是让一个现有的采集路径变得可靠且可测量。特别需要帮助的领域：
1. 真实世界的录制和回录验证，包括长时间运行和暂停/恢复。
2. 跨多样硬件的多显示器和 DPI 正确性。
3. 集成测试、打包和发布自动化。
4. 基于真实使用反馈的 UX 优化。

如果你想认领一个工作项，在大量投入之前请先开启或评论一个 issue，以避免工作重复。较大的更改应始于简短的技术提案。

**English:**
The best first contribution is not another feature. It is to make one existing capture path reliable and measurable. In particular, help is wanted with:
1. Real-world recording and replay validation, including long runs and pause/resume.
2. Multi-monitor and DPI correctness across diverse hardware.
3. Integration tests, packaging, and release automation.
4. UX polish based on real usage feedback.

If you want to take ownership of a work item, open or comment on an issue before investing heavily so work does not get duplicated. Larger changes should begin with a short technical proposal.

**Français:**
La meilleure première contribution n'est pas une autre fonctionnalité. C'est de rendre un chemin de capture existant fiable et mesurable. L'aide est particulièrement demandée pour :
1. Validation réelle de l'enregistrement et de la reprise, y compris les longues sessions et pause/reprise.
2. Correction multi-écrans et DPI sur divers matériels.
3. Tests d'intégration, packaging et automatisation des versions.
4. Polissage UX basé sur les retours d'utilisation réels.

Si vous souhaitez prendre en charge un élément de travail, ouvrez ou commentez un issue avant d'investir lourdement afin d'éviter les doublons. Les modifications importantes doivent commencer par une courte proposition technique.

## Project expectations / 项目约定 / Attentes du projet

**中文:**
- 期望不完整的代码和行为边缘情况。
- 未经在自有硬件上验证，请勿将此项目用于关键录制。
- 保持拉取请求聚焦，并在相关时包含 Windows 版本、显示设置、音频设备和测试步骤。
- 项目开放给新维护者。持续、谨慎的贡献者可能被邀请参与维护。

**English:**
- Expect unfinished code and behavioral edge cases.
- Do not use this project for critical recording without validating it on your own hardware.
- Keep pull requests focused and include the Windows version, display setup, audio devices, and test steps when relevant.
- The project is open to a new maintainer. Sustained, careful contributors may be invited to help maintain it.

**Français:**
- Attendez-vous à du code inachevé et des cas limites comportementaux.
- N'utilisez pas ce projet pour des enregistrements critiques sans l'avoir validé sur votre propre matériel.
- Gardez les pull requests focalisées et incluez la version Windows, la configuration d'affichage, les périphériques audio et les étapes de test lorsque cela est pertinent.
- Le projet est ouvert à un nouveau mainteneur. Les contributeurs constants et prudents peuvent être invités à participer à la maintenance.

## License / 许可证 / Licence

**中文 / English / Français:**
MIT. See [LICENSE](LICENSE). / MIT. 参见 [LICENSE](LICENSE)。 / MIT. Voir [LICENSE](LICENSE).
