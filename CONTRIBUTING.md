# Contributing to ScreenTools / 参与贡献 / Contribuer à ScreenTools

**中文 / Chinese:**
感谢你考虑为这个未完成的项目做出贡献。在开始之前，请阅读 `README.md` 和 `REMAINING_WORK.md`，查看现有问题，并评论你打算处理的项目。对于架构性更改，请在编写大型补丁之前描述方法和权衡。

**English:**
Thanks for considering work on this unfinished project. Before starting, read `README.md` and `REMAINING_WORK.md`, check existing issues and comment on the item you intend to work on. For architectural changes, describe the approach and tradeoffs before writing a large patch.

**Français:**
Merci de considérer travailler sur ce projet inachevé. Avant de commencer, lisez `README.md` et `REMAINING_WORK.md`, consultez les issues existants et commentez l'élément que vous avez l'intention de traiter. Pour les modifications architecturales, décrivez l'approche et les compromis avant d'écrire un correctif important.

## Development / 开发 / Développement

**中文:**
```powershell
dotnet restore .\ScreenTools\ScreenTools.sln
dotnet build .\ScreenTools\ScreenTools.sln
```
使用聚焦的分支，保持生成文件不在提交中。永远不要提交 `bin`、`obj`、本地存档、录制内容、用户特定项目设置或 `ffmpeg.exe` 二进制文件。

**English:**
```powershell
dotnet restore .\ScreenTools\ScreenTools.sln
dotnet build .\ScreenTools\ScreenTools.sln
```
Use a focused branch and keep generated files out of commits. Never commit `bin`, `obj`, local archives, recordings, user-specific project settings, or an `ffmpeg.exe` binary.

**Français:**
```powershell
dotnet restore .\ScreenTools\ScreenTools.sln
dotnet build .\ScreenTools\ScreenTools.sln
```
Utilisez une branche focalisée et gardez les fichiers générés hors des commits. Ne commettez jamais `bin`, `obj`, les archives locales, les enregistrements, les paramètres de projet spécifiques à l'utilisateur ou un binaire `ffmpeg.exe`.

## Pull requests / 拉取请求 / Pull requests

**中文:**
请包括：
- 问题和所选方法
- 有意排除的内容
- 测试步骤和结果
- 捕获错误的 Windows 版本和相关硬件详细信息
- 可见 UI 更改的屏幕截图或短录制

对于录制更改，至少测试开始/停止、暂停/恢复、重复录制、输出播放和清理。对于回录更改，报告请求和实际导出时长。

**English:**
Please include:
- The problem and the chosen approach
- What is intentionally left out
- Test steps and results
- Windows version and relevant hardware details for capture bugs
- Screenshots or short recordings for visible UI changes

For recording changes, test at least start/stop, pause/resume, repeated recordings, output playback, and cleanup. For replay changes, report the requested and actual exported duration.

**Français:**
Veuillez inclure :
- Le problème et l'approche choisie
- Ce qui est intentionnellement exclu
- Les étapes de test et les résultats
- La version Windows et les détails matériels pertinents pour les bugs de capture
- Des captures d'écran ou de courts enregistrements pour les modifications UI visibles

Pour les modifications d'enregistrement, testez au moins démarrage/arrêt, pause/reprise, enregistrements répétés, lecture de sortie et nettoyage. Pour les modifications de reprise, signalez la durée demandée et la durée exportée réelle.

## Scope / 范围 / Portée

**中文:**
目前可靠性工作比添加功能更有价值。最高优先级是系统音频、录制稳定性、回录验证、多显示器/DPI 行为和自动化测试。

**English:**
Reliability work is more valuable than adding features right now. The highest priorities are system audio, recording stability, replay validation, multi-monitor/DPI behavior, and automated tests.

**Français:**
Le travail de fiabilité est plus valuable que l'ajout de fonctionnalités pour l'instant. Les priorités les plus élevées sont l'audio système, la stabilité d'enregistrement, la validation de reprise, le comportement multi-écrans/DPI et les tests automatisés.
