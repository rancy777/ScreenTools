# Remaining Work / 剩余工作 / Travail restant

## Current Status / 当前状态 / État actuel

**中文:**
项目当前已经具备可运行的基础能力，并且 Phase 1-6 的核心改进已完成：
- 截图：可用，支持全局快捷键、格式选择、输出目录、最近输出记录。
- 录屏：可用，支持开始、暂停、停止、质量档位、麦克风采集、`ffmpeg` 视频导出、运行中状态反馈。
- 回录：可用，支持缓存最近片段、按当前质量档位导出、最近输出记录。
- 系统声音：已替换为 NAudio WASAPI Loopback，更可靠。
- 多显示器 / DPI：已加入 Per Monitor V2 感知。
- 错误处理：关键路径已加强，空 catch 已减少。
- 测试：已加入 xUnit 测试项目和基础模型测试。
- 打包：已加入 MSIX 打包项目。

当前仍不建议定义为"完全完成"，主要原因在于稳定性、系统声音能力和整体验收还没有完全闭环。

**English:**
The project currently has a runnable foundation, and core improvements from Phases 1-6 are complete:
- Screenshots: working, with global shortcuts, format selection, output directory, and recent history.
- Recording: working, with start, pause, stop, quality profiles, microphone capture, `ffmpeg` MP4 export, and runtime status feedback.
- Replay: working, with recent clip caching, quality-based export, and recent output history.
- System audio: replaced with NAudio WASAPI Loopback for better reliability.
- Multi-monitor / DPI: Per Monitor V2 awareness added.
- Error handling: critical paths hardened, empty catch blocks reduced.
- Tests: xUnit test project and basic model tests added.
- Packaging: MSIX packaging project added.

It is still not recommended to define this as "fully complete," mainly because stability, system audio capability, and overall acceptance validation are not yet fully closed.

**Français:**
Le projet dispose actuellement d'une base fonctionnelle, et les améliorations principales des phases 1 à 6 sont terminées :
- Captures d'écran : fonctionnelles, avec raccourcis globaux, sélection de format, répertoire de sortie et historique récent.
- Enregistrement : fonctionnel, avec démarrage, pause, arrêt, profils de qualité, capture microphone, export MP4 `ffmpeg` et retour d'état en temps réel.
- Replay : fonctionnel, avec cache de clip récent, export basé sur la qualité et historique de sortie récent.
- Audio système : remplacé par NAudio WASAPI Loopback pour une meilleure fiabilité.
- Multi-écrans / DPI : prise en charge Per Monitor V2 ajoutée.
- Gestion des erreurs : chemins critiques renforcés, blocs catch vides réduits.
- Tests : projet de tests xUnit et tests de modèle de base ajoutés.
- Packaging : projet de packaging MSIX ajouté.

Il n'est toujours pas recommandé de définir ce projet comme "entièrement terminé", principalement parce que la stabilité, la capacité audio système et la validation globale ne sont pas encore entièrement bouclées.

## High Priority / 高优先级 / Haute priorité

### 1. Recording Validation / 录制验证 / Validation d'enregistrement

**中文:**
需要做一轮完整录屏验收，而不是只看构建成功。

待做事项：
- 验证 `mp4` 输出是否稳定可播放。
- 验证暂停/恢复后音视频是否连续。
- 验证长时间录制是否出现明显卡顿、掉帧、内存上涨。
- 验证录制中退出设置页、关闭窗口、停止录制等路径的结果是否一致。
- 验证多次连续录制时临时目录和输出文件是否都正确清理。

**English:**
A complete recording acceptance pass is needed, rather than just checking build success.

Tasks:
- Verify `mp4` output plays stably.
- Verify A/V continuity after pause/resume.
- Verify no significant stuttering, dropped frames, or memory growth during long recordings.
- Verify consistent results when exiting settings, closing windows, or stopping recording during capture.
- Verify temporary directories and output files are cleaned correctly across repeated recordings.

**Français:**
Une passe d'acceptation complète de l'enregistrement est nécessaire, plutôt que de vérifier simplement le succès de la construction.

Tâches :
- Vérifier que la sortie `mp4` est lisible de manière stable.
- Vérifier la continuité A/V après pause/reprise.
- Vérifier l'absence de bégaiement significatif, de frames perdues ou de croissance mémoire lors des longs enregistrements.
- Vérifier des résultats cohérents lors de la sortie des paramètres, de la fermeture de fenêtres ou de l'arrêt de l'enregistrement pendant la capture.
- Vérifier que les répertoires temporaires et les fichiers de sortie sont correctement nettoyés lors d'enregistrements répétés.

### 2. Replay Validation / 回录验证 / Validation de reprise

**中文:**
回录当前可用，但仍需做实际体验验证。

待做事项：
- 验证 `30s / 60s` 导出的真实时长是否符合预期。
- 验证不同质量档位下回录画质和流畅度。
- 验证回录在高频操作场景下是否丢帧明显。
- 验证回录导出和正在录屏同时发生时是否存在冲突。

**English:**
Replay is currently usable, but real-world validation is still needed.

Tasks:
- Verify exported duration matches expected `30s / 60s`.
- Verify replay quality and smoothness at different quality presets.
- Verify whether frames are noticeably dropped under high-frequency operation.
- Verify whether conflicts occur when exporting replay while recording is active.

**Français:**
La reprise est actuellement utilisable, mais une validation réelle est toujours nécessaire.

Tâches :
- Vérifier que la durée exportée correspond aux `30s / 60s` attendus.
- Vérifier la qualité et la fluidité de la reprise selon différents profils de qualité.
- Vérifier si des frames sont clairement perdues dans des scénarios à haute fréquence.
- Vérifier si des conflits surviennent lors de l'export de reprise pendant un enregistrement actif.

## Medium Priority / 中优先级 / Priorité moyenne

### 3. Output Rules / 输出规则 / Règles de sortie

**中文:**
输出结构已经基本成型，但还可以继续规范。

待做事项：
- 明确截图、录屏、回录的最终目录结构。
- 明确何时保留 `manifest/json`，何时只保留最终成品。
- 决定是否保留 `microphone.wav` / `system-audio.wav` 侧车文件。
- 增加输出失败时的回滚或清理策略。

**English:**
The output structure is basically formed but can still be standardized.

Tasks:
- Clarify the final directory structure for screenshots, recordings, and replays.
- Decide when to keep `manifest/json` versus only the final deliverable.
- Decide whether to keep `microphone.wav` / `system-audio.wav` sidecar files.
- Add rollback or cleanup strategies on output failure.

**Français:**
La structure de sortie est fondamentalement établie mais peut encore être standardisée.

Tâches :
- Clarifier la structure finale des répertoires pour les captures, enregistrements et replay.
- Décider quand conserver `manifest/json` par rapport à ne conserver que le livrable final.
- Décider de conserver ou non les fichiers sidecar `microphone.wav` / `system-audio.wav`.
- Ajouter des stratégies de rollback ou de nettoyage en cas d'échec de sortie.

### 4. Runtime UX Cleanup / 运行时 UX 清理 / Nettoyage UX

**中文:**
现在已经有主界面、HUD、录制中窗口的状态反馈，但仍有部分弹窗。

待做事项：
- 统一哪些信息用状态栏展示，哪些必须弹窗。
- 减少非致命 warning 的打断式弹窗。
- 优化"最近输出"和状态栏的文案，让结果更清晰。

**English:**
There is already status feedback in the main interface, HUD, and recording window, but some dialogs remain.

Tasks:
- Unify what information is shown in the status bar versus what must be a dialog.
- Reduce interrupting pop-ups for non-fatal warnings.
- Optimize the copy in "recent output" and the status bar for clarity.

**Français:**
Il existe déjà un retour d'état dans l'interface principale, le HUD et la fenêtre d'enregistrement, mais certaines boîtes de dialogue persistent.

Tâches :
- Unifier les informations affichées dans la barre d'état par rapport à celles qui doivent être en boîte de dialogue.
- Réduire les pop-ups interrupteurs pour les avertissements non critiques.
- Optimiser le texte de "sortie récente" et de la barre d'état pour plus de clarté.

### 5. Shortcut and Environment Validation / 快捷键和环境验证 / Raccourcis et validation environnementale

**中文:**
全局快捷键已接入，但还没做环境层面的冲突检查。

待做事项：
- 验证 `Alt + A / Alt + R / Alt + S` 是否与本机常用软件冲突。
- 验证应用在多显示器环境下的截图/录屏行为。
- 验证高 DPI / 缩放比例下的输出画面。

**English:**
Global shortcuts are already integrated, but environmental conflict checks have not been done.

Tasks:
- Verify whether `Alt + A / Alt + R / Alt + S` conflicts with common local software.
- Verify screenshot/recording behavior in multi-monitor environments.
- Verify output quality under high DPI / scaling.

**Français:**
Les raccourcis globaux sont déjà intégrés, mais les vérifications de conflit environnemental n'ont pas été effectuées.

Tâches :
- Vérifier si `Alt + A / Alt + R / Alt + S` entre en conflit avec des logiciels courants.
- Vérifier le comportement de capture d'écran/enregistrement dans des environnements multi-écrans.
- Vérifier la qualité de sortie sous haute résolution / mise à l'échelle.

## Low Priority / 低优先级 / Basse priorité

### 6. Code Cleanup / 代码清理 / Nettoyage de code

**中文:**
当前代码已经可用，但还可以继续整理。

待做事项：
- 抽离更多录制结果模型，减少 `WindowFlowCoordinator` 里的流程分发责任。
- 统一服务层的异常模型和错误消息。
- 为关键路径补基础测试或最小验收脚本。

**English:**
The current code is usable but can still be refactored further.

Tasks:
- Extract more recording result models to reduce the orchestration responsibility in `WindowFlowCoordinator`.
- Unify exception models and error messages in the service layer.
- Add basic tests or minimal acceptance scripts for critical paths.

**Français:**
Le code actuel est utilisable mais peut encore être davantage refactorisé.

Tâches :
- Extraire plus de modèles de résultat d'enregistrement pour réduire la responsabilité d'orchestration dans `WindowFlowCoordinator`.
- Unifier les modèles d'exception et les messages d'erreur dans la couche de service.
- Ajouter des tests de base ou des scripts d'acceptation minimaux pour les chemins critiques.

### 7. Packaging and Release / 打包和发布 / Packaging et publication

**中文:**
- 完善 MSIX 打包配置。
- 添加 CI/CD 发布流程。
- 创建 GitHub Release 流程。

**English:**
- Finalize MSIX packaging configuration.
- Add CI/CD release pipeline.
- Create GitHub Release workflow.

**Français:**
- Finaliser la configuration du packaging MSIX.
- Ajouter un pipeline de publication CI/CD.
- Créer un workflow GitHub Release.

## Recommended Next Step / 推荐下一步 / Prochaine étape recommandée

**中文:**
下一步最合理的工作不是继续堆新功能，而是做一次验收式联调：
1. 截图全链路验证。
2. 录屏全链路验证。
3. 回录全链路验证。
4. 根据真实问题再定向修补。

**English:**
The next most reasonable work is not to stack more features, but to perform an acceptance pass:
1. End-to-end screenshot validation.
2. End-to-end recording validation.
3. End-to-end replay validation.
4. Targeted fixes based on real issues.

**Français:**
Le travail le plus raisonnable suivant n'est pas d'empiler plus de fonctionnalités, mais d'effectuer une passe d'acceptation :
1. Validation de bout en bout des captures d'écran.
2. Validation de bout en bout de l'enregistrement.
3. Validation de bout en bout de la reprise.
4. Corrections ciblées basées sur les problèmes réels.
