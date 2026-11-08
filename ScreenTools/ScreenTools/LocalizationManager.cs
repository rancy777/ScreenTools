using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using ScreenTools.Controls;

namespace ScreenTools;

/// <summary>Runtime localization for all application windows. English is the fallback language.</summary>
public static class LocalizationManager
{
    /// <summary>
    /// Defines the English value used by this component.
    /// </summary>
    public const string English = "en";
    /// <summary>
    /// Defines the French value used by this component.
    /// </summary>
    public const string French = "fr";
    /// <summary>
    /// Defines the Chinese value used by this component.
    /// </summary>
    public const string Chinese = "zh-CN";

    private static readonly Dictionary<string, (string En, string Fr)> Catalog = new(StringComparer.Ordinal)
    {
        ["正在录入快捷键"] = ("Recording shortcut", "Enregistrement du raccourci"),
        ["按下键盘或鼠标组合，Esc 取消，Backspace 清空。"] = ("Press a keyboard or mouse combination. Esc cancels; Backspace clears.", "Appuyez sur une combinaison clavier ou souris. Échap annule ; Retour arrière efface."),
        ["让高频操作更安静地待在前台"] = ("Keep frequent actions quietly within reach", "Gardez les actions fréquentes discrètement à portée de main"),
        ["围绕截图、录制与回录保留必要控制，让界面退后，让动作留在前面。"] = ("Keep essential controls for screenshots, recording and instant replay close at hand.", "Gardez à portée de main les commandes essentielles des captures, de l’enregistrement et de la relecture instantanée."),
        ["就绪"] = ("Ready", "Prêt"), ["等待下一次截图、录制或回录。"] = ("Waiting for your next screenshot, recording or replay.", "En attente de votre prochaine capture, de votre prochain enregistrement ou de votre prochaine relecture."),
        ["截图设置"] = ("Screenshot settings", "Paramètres des captures"), ["输出格式与保存位置"] = ("Output format and save location", "Format et emplacement d’enregistrement"),
        ["保存格式"] = ("Image format", "Format d’image"), ["选择默认输出格式"] = ("Choose the default output format", "Choisissez le format de sortie par défaut"),
        ["截图模式"] = ("Capture mode", "Mode de capture"), ["全屏截图或先框选区域"] = ("Capture the full screen or select a region", "Capturez tout l’écran ou sélectionnez une zone"),
        ["全屏"] = ("Full screen", "Plein écran"), ["区域"] = ("Region", "Zone"), ["快捷键"] = ("Shortcut", "Raccourci"),
        ["点击后按下键盘或鼠标组合"] = ("Click, then press a keyboard or mouse combination", "Cliquez, puis appuyez sur une combinaison clavier ou souris"),
        ["截图后行为"] = ("After-capture action", "Action après capture"), ["决定截图后是只保存、直接贴图，还是先显示操作条"] = ("Choose whether to save, pin, or show quick actions after a capture", "Choisissez d’enregistrer, d’épingler ou d’afficher les actions rapides après une capture"),
        ["保存后显示操作条"] = ("Show quick actions", "Afficher les actions rapides"), ["仅保存"] = ("Save only", "Enregistrer uniquement"), ["保存并贴到屏幕"] = ("Save and pin", "Enregistrer et épingler"),
        ["复制到剪贴板"] = ("Copy to clipboard", "Copier dans le presse-papiers"), ["截图完成后自动复制图像"] = ("Automatically copy the image after capture", "Copier automatiquement l’image après la capture"),
        ["保存路径"] = ("Save location", "Emplacement d’enregistrement"), ["更改位置"] = ("Change location", "Modifier l’emplacement"), ["贴纸管理"] = ("Pinned screenshots", "Captures épinglées"),
        ["关闭当前所有贴到屏幕上的截图"] = ("Close all screenshots currently pinned on screen", "Fermer toutes les captures actuellement épinglées à l’écran"), ["关闭全部贴纸"] = ("Close all pins", "Tout fermer"),
        ["录屏设置"] = ("Recording settings", "Paramètres d’enregistrement"), ["采集输入与画质"] = ("Capture inputs and quality", "Sources audio et qualité"),
        ["麦克风"] = ("Microphone", "Microphone"), ["包含麦克风音频"] = ("Include microphone audio", "Inclure le son du microphone"),
        ["系统声音"] = ("System audio", "Audio système"), ["包含系统播放音频"] = ("Include system playback audio", "Inclure le son du système"),
        ["画质"] = ("Quality", "Qualité"), ["默认录制档位"] = ("Default recording preset", "Préréglage d’enregistrement par défaut"),
        ["回录设置"] = ("Instant replay settings", "Paramètres de relecture instantanée"), ["缓存开关与时长控制"] = ("Buffer and duration controls", "Contrôle du tampon et de la durée"),
        ["后台回录缓存"] = ("Replay buffer", "Tampon de relecture"), ["启用后持续保留最近一段画面，关闭后不占用回录资源"] = ("Continuously keep recent footage while enabled; disable it to free replay resources", "Conservez en continu les dernières images tant que cette option est activée ; désactivez-la pour libérer la mémoire"),
        ["回录时长"] = ("Replay duration", "Durée de la relecture"), ["保留触发前片段"] = ("Keep footage from before the trigger", "Conserver les images précédant le déclenchement"),
        ["回录音频"] = ("Replay audio", "Audio de la relecture"), ["当前回录仅保存画面，不包含麦克风或系统声音"] = ("Replay currently saves video only, without microphone or system audio", "La relecture enregistre uniquement l’image, sans microphone ni son système"), ["仅画面"] = ("Video only", "Image uniquement"),
        ["剪贴板"] = ("Clipboard", "Presse-papiers"), ["最近复制结果、失败原因与重试入口"] = ("Recent copies, failure reasons and retry actions", "Copies récentes, causes d’échec et actions de nouvelle tentative"), ["还没有剪贴板记录。"] = ("There is no clipboard history yet.", "L’historique du presse-papiers est vide."),
        ["最近输出"] = ("Recent output", "Fichiers récents"), ["最近几次截图、录制与回录"] = ("Your latest screenshots, recordings and replays", "Vos dernières captures, enregistrements et relectures"), ["还没有输出记录。"] = ("There are no output files yet.", "Aucun fichier de sortie pour le moment."),
        ["常规"] = ("General", "Général"), ["低频偏好项"] = ("Other preferences", "Autres préférences"), ["开机自启动"] = ("Run at startup", "Lancer au démarrage"), ["登录后自动运行"] = ("Start automatically when you sign in", "Démarrer automatiquement à la connexion"),
        ["准备开始录制"] = ("Ready to record", "Prêt à enregistrer"), ["截图"] = ("Screenshot", "Capture d’écran"), ["录制"] = ("Recording", "Enregistrement"), ["回录"] = ("Instant replay", "Relecture instantanée"),
        ["录制设置"] = ("Recording controls", "Commandes d’enregistrement"), ["只保留开始前的必要控制。"] = ("Only the essential controls are shown before recording starts.", "Seules les commandes essentielles sont affichées avant le début de l’enregistrement."), ["等待开始录制。"] = ("Waiting for recording to start.", "En attente du démarrage de l’enregistrement."),
        ["录制麦克风"] = ("Record microphone", "Enregistrer le microphone"), ["保留旁白输入"] = ("Include voice input", "Inclure la voix"), ["同步系统音频"] = ("Capture system audio", "Capturer l’audio système"), ["开始录制"] = ("Start recording", "Démarrer l’enregistrement"),
        ["拖拽选择截图区域"] = ("Drag to select a capture region", "Faites glisser pour sélectionner une zone"), ["松开鼠标完成，Esc 取消"] = ("Release the mouse to finish; Esc cancels", "Relâchez la souris pour terminer ; Échap annule"), ["未开始框选"] = ("No region selected", "Aucune zone sélectionnée"),
        ["截图已保存"] = ("Screenshot saved", "Capture enregistrée"), ["贴到屏幕"] = ("Pin on screen", "Épingler à l’écran"), ["打开"] = ("Open", "Ouvrir"), ["定位"] = ("Show in folder", "Afficher dans le dossier"), ["默认:"] = ("Default:", "Par défaut :"),
        ["以后只保存"] = ("Save only next time", "Enregistrer uniquement ensuite"), ["以后默认贴图"] = ("Pin by default next time", "Épingler par défaut ensuite"), ["截图贴纸"] = ("Screenshot pin", "Capture épinglée"), ["置顶"] = ("Pinned", "Épinglé"),
        ["复制"] = ("Copy", "Copier"), ["另存为"] = ("Save as", "Enregistrer sous"), ["透明"] = ("Opacity", "Opacité"), ["关闭全部"] = ("Close all", "Tout fermer"),
        ["正在录入快捷键。按下键盘或鼠标组合，按 Esc 取消，按 Backspace 清空。"] = ("Recording a shortcut. Press a keyboard or mouse combination. Esc cancels; Backspace clears.", "Enregistrement d’un raccourci. Appuyez sur une combinaison clavier ou souris. Échap annule ; Retour arrière efface."),
        ["请按组合"] = ("Press a combination", "Appuyez sur une combinaison"), ["再次复制"] = ("Copy again", "Copier à nouveau"), ["重试复制"] = ("Retry copy", "Réessayer la copie"),
        ["已关闭所有截图贴纸。"] = ("All pinned screenshots have been closed.", "Toutes les captures épinglées ont été fermées."), ["快捷键已更新。"] = ("Shortcut updated.", "Raccourci mis à jour."), ["已取消快捷键录入。"] = ("Shortcut recording cancelled.", "Enregistrement du raccourci annulé."),
        ["麦克风 关闭 · 系统声音 关闭"] = ("Microphone off · System audio off", "Microphone désactivé · Audio système désactivé"), ["等待开始录制"] = ("Waiting to record", "En attente d’enregistrement"), ["尚未开始"] = ("Not started", "Pas encore démarré"),
        ["屏幕捕获工作区"] = ("Screen capture workspace", "Espace de capture d’écran"), ["区域选择"] = ("Region selection", "Sélection de zone"), ["截图操作"] = ("Screenshot actions", "Actions de capture"),
        ["录制控制面板"] = ("Recording HUD controls", "Commandes HUD d’enregistrement"), ["录制状态栏"] = ("Recording status HUD", "Indicateur d’enregistrement"), ["正在完成录制"] = ("Finalizing recording…", "Finalisation de l’enregistrement…"), ["设置"] = ("SETTINGS", "PARAMÈTRES"),
        ["1080P 60FPS · 输出到 LensSnap"] = ("1080P 60FPS · Output to LensSnap", "1080P 60FPS · Enregistrement dans LensSnap"),
        ["未设置"] = ("Not set", "Non défini"),
        ["找不到应用程序路径。"] = ("The application path could not be found.", "Impossible de trouver le chemin de l’application."),
        ["源文件不存在，请重新截图后再试。"] = ("The source file no longer exists. Take the screenshot again and retry.", "Le fichier source n’existe plus. Reprenez la capture, puis réessayez."),
        ["已复制到系统剪贴板。"] = ("Copied to the system clipboard.", "Copié dans le presse-papiers système."),
        ["系统剪贴板正被其他程序占用，请重试一次。"] = ("The system clipboard is busy. Please try again.", "Le presse-papiers système est occupé. Veuillez réessayer."),
        ["写入系统剪贴板失败。"] = ("Could not write to the system clipboard.", "Impossible d’écrire dans le presse-papiers système."),
        ["麦克风将使用 Windows 音频采集 API 录制。"] = ("The microphone will be recorded through the Windows audio capture API.", "Le microphone sera enregistré via l’API de capture audio de Windows."),
        ["系统声音将使用 WASAPI 回环采集，无需启用立体声混音。"] = ("System audio is captured through WASAPI loopback; Stereo Mix is not required.", "Le son système est capturé via WASAPI en boucle ; Stereo Mix n’est pas nécessaire."),
        ["Windows 未检测到活动的音频输出设备。"] = ("Windows did not detect an active audio output device.", "Windows n’a détecté aucun périphérique de sortie audio actif."),
        ["WASAPI 回环采集在五秒内未能确认停止。"] = ("WASAPI loopback did not confirm that capture stopped within five seconds.", "La capture en boucle WASAPI n’a pas confirmé son arrêt dans les cinq secondes."),
        ["WASAPI 回环采集意外停止，视频录制将继续，但不再包含系统声音。"] = ("WASAPI loopback stopped unexpectedly. Video recording will continue without system audio.", "La capture en boucle WASAPI s’est arrêtée de manière inattendue. L’enregistrement vidéo continuera sans le son système."),
        ["系统声音输出路径或播放设备尚未初始化。"] = ("The system-audio output path or playback endpoint has not been initialized.", "Le chemin de sortie du son système ou le périphérique de lecture n’a pas été initialisé."),
        ["未启用系统声音采集。"] = ("System-audio capture was not enabled.", "La capture du son système n’était pas activée."),
        ["系统声音采集不可用或已中断。"] = ("System-audio capture was unavailable or was interrupted.", "La capture du son système était indisponible ou a été interrompue."),
        ["麦克风录制已经启动。"] = ("Microphone recording has already started.", "L’enregistrement du microphone a déjà démarré."),
        ["系统声音采集已经启动。"] = ("System-audio capture has already started.", "La capture du son système a déjà démarré."),
        ["系统声音输出路径未初始化。"] = ("The system-audio output path has not been initialized.", "Le chemin de sortie du son système n’a pas été initialisé."),
        ["无法获取默认音频输出设备，请检查音频驱动是否正常。"] = ("Could not access the default audio output device. Check that the audio driver is working.", "Impossible d’accéder au périphérique de sortie audio par défaut. Vérifiez le pilote audio."),
        ["当前没有检测到可用于 WASAPI 回环采集的音频输出设备。"] = ("No audio output device available for WASAPI loopback capture was found.", "Aucun périphérique de sortie compatible avec la capture en boucle WASAPI n’a été détecté."),
        ["已取消截图。"] = ("Screenshot cancelled.", "Capture annulée."),
        ["回录仍在导出，请稍候。"] = ("The replay is still being exported. Please wait.", "La relecture est toujours en cours d’exportation. Veuillez patienter."),
        ["后台回录缓存已关闭，开启后才会保留可回录片段。"] = ("The replay buffer is disabled. Enable it to retain footage for instant replay.", "Le tampon de relecture est désactivé. Activez-le pour conserver les séquences à relire."),
        ["当前还没有足够的回录缓存。"] = ("There is not enough replay footage yet.", "Le tampon ne contient pas encore assez d’images pour créer une relecture."),
        ["正在后台导出回录，仅保存画面。"] = ("Exporting the replay in the background. Video only; no audio is included.", "Exportation de la relecture en arrière-plan. La vidéo est enregistrée sans audio."),
        ["快捷键不能只包含 Ctrl、Alt、Shift 或 Win。"] = ("A shortcut cannot consist only of Ctrl, Alt, Shift or Win.", "Un raccourci ne peut pas contenir uniquement Ctrl, Alt, Shift ou Win."),
        ["当前内容"] = ("Current item", "Élément actuel"),
        ["输出目录为空，请在设置中指定一个有效的输出路径。"] = ("The output folder is empty. Choose a valid output folder in Settings.", "Le dossier de sortie est vide. Choisissez un emplacement valide dans les paramètres."),
        ["输出目录路径无效，请选择一个本地驱动器上的文件夹。"] = ("The output folder path is invalid. Choose a folder on a local drive.", "Le chemin du dossier de sortie est invalide. Choisissez un dossier sur un lecteur local."),
        ["截图后默认行为已改为仅保存。"] = ("The default after-capture action is now Save only.", "L’action par défaut après capture est désormais « Enregistrer uniquement »."),
        ["截图后默认行为已改为保存并贴到屏幕。"] = ("The default after-capture action is now Save and pin.", "L’action par défaut après capture est désormais « Enregistrer et épingler »."),
        ["录制已开始。当前未启用系统声音。"] = ("Recording started. System-audio capture is disabled.", "L’enregistrement a commencé. La capture du son système est désactivée."),
        ["设置文件已损坏，已隔离损坏文件并恢复默认设置。"] = ("The settings file was corrupted. The damaged file was preserved separately and default settings were restored.", "Le fichier de paramètres était endommagé. Il a été mis de côté et les paramètres par défaut ont été restaurés."),
        ["未找到截图文件，可能已被移动或删除。"] = ("The screenshot file could not be found. It may have been moved or deleted.", "La capture d’écran est introuvable. Elle a peut-être été déplacée ou supprimée."),
        ["PNG 图像"] = ("PNG image", "Image PNG"),
        ["JPEG 图像"] = ("JPEG image", "Image JPEG"),
        ["BMP 图像"] = ("BMP image", "Image BMP"),
        ["所有文件"] = ("All files", "Tous les fichiers"),
        ["回放导出已取消，回放缓存仍然可用。"] = ("Replay export was cancelled. The replay buffer is still available.", "L’export de la relecture a été annulé. Le tampon de relecture est toujours disponible."),
        ["MP4 导出已取消，已保留采集到的帧序列。"] = ("MP4 export was cancelled; the captured frame sequence was preserved.", "L’export MP4 a été annulé ; la séquence d’images capturées a été conservée."),
        ["必须提供目标路径。"] = ("A destination path is required.", "Un chemin de destination est requis."),
        ["无法解析目标目录。"] = ("The destination directory could not be resolved.", "Impossible de déterminer le dossier de destination."),
        ["输出文件名前缀不能为空。"] = ("An output prefix is required.", "Un préfixe de nom de fichier de sortie est requis."),
        ["FFmpeg 超时时间必须大于零。"] = ("The FFmpeg timeout must be greater than zero.", "Le délai maximal de FFmpeg doit être supérieur à zéro."),
        ["进程超时时间必须大于零。"] = ("The process timeout must be greater than zero.", "Le délai maximal du processus doit être supérieur à zéro."),
        ["本地"] = ("LOCAL", "LOCAL"),
        ["录制标记"] = ("RECORD", "ENREGISTREMENT"),
        ["模式"] = ("MODE", "MODE"),
        ["当前配置"] = ("ACTIVE PROFILE", "PROFIL ACTIF"),
        ["路径"] = ("Path", "Chemin"),
        ["默认保存位置"] = ("Default save location", "Emplacement d’enregistrement par défaut")
    };

    private static readonly (string Source, string En, string Fr)[] DynamicFragments =
    [
        ("最近状态", "Latest status", "Dernier état"), ("无预览", "No preview", "Aucun aperçu"),
        ("已复制", "Copied", "Copié"), ("复制失败", "Copy failed", "Échec de la copie"),
        ("请按下键盘或鼠标组合", "Press a keyboard or mouse combination", "Appuyez sur une combinaison clavier ou souris"),
        ("请按组合", "Press a combination", "Appuyez sur une combinaison"),
        ("Esc 取消", "Esc cancels", "Échap annule"), ("Backspace 清空", "Backspace clears", "Retour arrière efface"),
        ("选区:", "Region:", "Zone :"), ("透明", "Opacity", "Opacité"),
        ("视频输出", "Video output", "Sortie vidéo"), ("单次输出", "Single image", "Image unique"),
        ("帧", "frames", "images"), ("当前未找到 ffmpeg，已保留帧序列。", "FFmpeg was not found; the captured frame sequence was kept.", "FFmpeg est introuvable ; la séquence d’images capturées a été conservée."),
        ("回录", "Instant replay", "Relecture instantanée"), ("录制", "Recording", "Enregistrement"),
        ("麦克风", "Microphone", "Microphone"), ("系统声音", "System audio", "Audio système"),
        ("关闭", "Off", "Désactivé"), ("运行中", "Running", "En cours"), ("不可用", "Unavailable", "Indisponible"),
        ("录制状态栏", "Recording status HUD", "Indicateur d’enregistrement"),
        ("输出到", "Output to", "Sortie vers"), ("已暂停", "Paused", "En pause"),
        ("截图", "Screenshot", "Capture d’écran"),
        ("图片加载失败：", "Image loading failed: ", "Échec du chargement de l’image : "),
        ("贴纸截图", "Pinned screenshot", "Capture épinglée"),
        ("无法开始录制：", "Could not start recording: ", "Impossible de démarrer l’enregistrement : "),
        ("截图已贴到屏幕：", "Screenshot pinned on screen: ", "Capture épinglée à l’écran : "),
        ("已重新复制到剪贴板：", "Copied to the clipboard again: ", "Copie effectuée à nouveau dans le presse-papiers : "),
        ("复制失败：", "Copy failed: ", "Échec de la copie : "),
        ("设置目录", "the folder configured in Settings", "le dossier défini dans les paramètres"),
        ("正在录入", "Recording ", "Saisie de "),
        ("快捷键", " shortcut", " raccourci"),
        ("已复制到剪贴板：", " copied to the clipboard: ", " copiée dans le presse-papiers : "),
        ("录制已开始。", "Recording started. ", "Enregistrement démarré. "),
        ("帧工作目录不存在：", "Frame workspace does not exist: ", "Le dossier de travail des images est introuvable : "),
        ("无法启动进程：", "The process could not be started: ", "Impossible de démarrer le processus : "),
        ("进程超时：", "Process timeout: ", "Délai d’exécution dépassé : "),
        ("，超时时间为 ", " after ", " après "),
        (" 秒。", " seconds.", " secondes."),
        ("FFmpeg 导出 MP4 失败（退出代码 ", "ffmpeg failed to export MP4 (exit code ", "Échec de l’export MP4 par FFmpeg (code de sortie "),
        ("）。", ").", ")."),
        ("关闭应用时清理失败：", "Shutdown cleanup failed: ", "Échec du nettoyage à la fermeture de l’application : "),
        ("麦克风采集未能正常结束：", "Microphone capture could not be finalized cleanly: ", "La capture du microphone ne s’est pas terminée correctement : "),
        ("系统声音采集未能正常结束：", "System-audio capture could not be finalized cleanly: ", "La capture du son système ne s’est pas terminée correctement : "),
        ("设置保存失败：", "Could not save settings: ", "Impossible d’enregistrer les paramètres : "),
        ("输出历史记录保存失败：", "Could not save output history: ", "Impossible d’enregistrer l’historique des fichiers : "),
        ("剪贴板历史记录保存失败：", "Could not save clipboard history: ", "Impossible d’enregistrer l’historique du presse-papiers : "),
        ("图片读取失败：", "Could not load the image: ", "Impossible de charger l’image : "),
        ("麦克风设备打开失败，错误码 ", "Could not open the microphone device. Error code ", "Impossible d’ouvrir le microphone. Code d’erreur "),
        ("麦克风录制启动失败，错误码 ", "Could not start microphone recording. Error code ", "Impossible de démarrer l’enregistrement du microphone. Code d’erreur "),
        ("麦克风缓冲区准备失败，错误码 ", "Could not prepare the microphone buffer. Error code ", "Impossible de préparer le tampon du microphone. Code d’erreur "),
        ("麦克风缓冲区提交失败，错误码 ", "Could not submit the microphone buffer. Error code ", "Impossible de transmettre le tampon du microphone. Code d’erreur "),
        ("麦克风采集未启动：", "Microphone capture did not start: ", "La capture du microphone n’a pas démarré : "),
        ("系统声音采集未启动：", "System-audio capture did not start: ", "La capture du son système n’a pas démarré : "),
        ("WASAPI 回环采集不可用：", "WASAPI loopback capture is unavailable: ", "La capture en boucle WASAPI est indisponible : "),
        ("录制期间", "During recording, ", "Pendant l’enregistrement, "),
        ("采集失败，视频录制将继续：", "capture failed; video recording will continue: ", "la capture a échoué ; l’enregistrement vidéo va continuer : "),
        ("录制内存占用过高（约 ", "Recording memory usage is high (about ", "La mémoire utilisée par l’enregistrement est élevée (environ "),
        (" MB），建议尽快结束录制。", " MB); consider stopping the recording soon.", " Mo) ; il est conseillé d’arrêter bientôt l’enregistrement."),
        ("录制已暂停于 ", "Recording paused at ", "Enregistrement mis en pause à "),
        ("录制已恢复于 ", "Recording resumed at ", "Enregistrement repris à "),
        ("录制帧采集失败：", "Recording frame capture failed: ", "Échec de la capture d’une image vidéo : "),
        ("采集诊断：请求 ", "Capture diagnostics: requests ", "Diagnostics de capture : demandes "),
        (" 次 · 跳过 ", " · skipped ", " · ignorées "),
        (" 帧 · 失败 ", " frames · failures ", " images · échecs "),
        (" 次 · JPEG ", " · JPEG ", " · JPEG "),
        (" · 峰值工作集 ", " · peak working set ", " · mémoire de travail maximale "),
        (" · 峰值私有内存 ", " · peak private memory ", " · mémoire privée maximale "),
        ("MP4 导出失败，已保留帧序列：", "MP4 export failed; the frame sequence was preserved: ", "L’export MP4 a échoué ; la séquence d’images a été conservée : "),
        ("录制时长 ", "Recording duration ", "Durée de l’enregistrement "),
        ("输出目录不可写：", "The output folder is not writable: ", "Le dossier de sortie n’est pas accessible en écriture : "),
        ("。请检查磁盘空间和文件夹权限。", ". Check available disk space and folder permissions.", ". Vérifiez l’espace disque disponible et les autorisations du dossier."),
        ("输出目录验证失败：", "Output folder validation failed: ", "La validation du dossier de sortie a échoué : "),
        ("截图失败：无法捕获屏幕画面。", "Screenshot failed: the screen could not be captured. ", "Échec de la capture d’écran : impossible de capturer l’image. "),
        ("区域截图失败：无法捕获屏幕区域。", "Region capture failed: the selected screen area could not be captured. ", "Échec de la capture de zone : impossible de capturer la zone sélectionnée. "),
        ("输出路径不能为空。", "The output path cannot be empty.", "Le chemin de sortie ne peut pas être vide."),
        ("质量档位不能为空。", "The quality preset cannot be empty.", "Le préréglage de qualité ne peut pas être vide."),
        ("录制帧采集失败：无法写入 ", "Recording frame capture failed: could not write ", "Échec de la capture vidéo : impossible d’écrire "),
        ("录制帧采集失败：无法捕获或编码画面。", "Recording frame capture failed: the screen could not be captured or encoded. ", "Échec de la capture vidéo : impossible de capturer ou d’encoder l’image. "),
        ("屏幕分辨率过高（", "Screen resolution is too high (", "La résolution de l’écran est trop élevée ("),
        ("，预计占用 ", ", estimated memory use ", ", mémoire estimée à "),
        (" MB），超过内存预算 ", " MB), exceeding the memory budget of ", " Mo), au-delà de la limite mémoire de "),
        (" MB。请降低显示缩放或选择区域截图。", " MB. Reduce display scaling or capture a smaller region.", " Mo. Réduisez la mise à l’échelle de l’affichage ou capturez une zone plus petite."),
        ("系统声音将通过 “", "System audio will be captured through ", "Le son système sera capturé via « "),
        ("” 使用 WASAPI 回环采集。", " using WASAPI loopback.", " » via la capture en boucle WASAPI."),
        ("当前枚举到 ", "Detected ", "Périphériques détectés : "),
        (" 等设备。", " devices in total.", " au total."),
        ("视频已输出", "video exported", "vidéo exportée"),
        ("结果已输出", "output saved", "fichier enregistré"),
        ("剪贴板失败：", "Clipboard error: ", "Erreur du presse-papiers : "),
        ("快捷键冲突：", "Shortcut conflict: ", "Conflit de raccourci : "),
        (" 已用于", " is already assigned to ", " est déjà attribué à "),
        ("录入中:", "Recording:", "Saisie en cours :"),
        ("透明 ", "Opacity ", "Opacité "),
        ("视频输出 · ", "Video output · ", "Vidéo exportée · "),
        ("帧 · 目标 ", " frames · target ", " images · cible "),
        (" FPS · 实际约 ", " FPS · actual about ", " FPS · réel environ "),
        ("回录实际覆盖约 ", "Replay footage covers about ", "La séquence en mémoire couvre environ "),
        ("，短于目标 ", ", shorter than the requested ", ", soit moins que les "),
        ("导出视频约 ", "Exported video is about ", "La vidéo exportée dure environ "),
        ("，缓存覆盖约 ", "; buffered footage covers about ", " ; le tampon couvre environ ")
    ];

    private static readonly List<WeakReference<Window>> Windows = [];
    private static string _language = DetectSystemLanguage();
    /// <summary>
    /// Gets the normalized language identifier currently used by the application.
    /// </summary>
    public static string CurrentLanguage => _language;
    /// <summary>
    /// Raised after the active application language changes.
    /// </summary>
    public static event EventHandler? LanguageChanged;

    /// <summary>
    /// Selects the supported application language that best matches the current Windows UI culture.
    /// </summary>
    public static string DetectSystemLanguage() => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName switch
    {
        "fr" => French,
        "zh" => Chinese,
        _ => English
    };

    /// <summary>
    /// Normalizes a language identifier to one of the languages supported by the application.
    /// </summary>
    public static string NormalizeLanguage(string? language)
    {
        var normalized = language?.Trim();
        if (string.Equals(normalized, French, StringComparison.OrdinalIgnoreCase) ||
            normalized?.StartsWith("fr-", StringComparison.OrdinalIgnoreCase) == true) return French;
        if (string.Equals(normalized, Chinese, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(normalized, "zh", StringComparison.OrdinalIgnoreCase) ||
            normalized?.StartsWith("zh-", StringComparison.OrdinalIgnoreCase) == true) return Chinese;
        return English;
    }

    /// <summary>
    /// Changes the active language and notifies registered windows so their text can refresh.
    /// </summary>
    public static void SetLanguage(string? language)
    {
        var normalized = NormalizeLanguage(language);
        if (string.Equals(_language, normalized, StringComparison.Ordinal)) return;
        _language = normalized;
        for (var i = Windows.Count - 1; i >= 0; i--)
        {
            if (Windows[i].TryGetTarget(out var window)) ApplyToWindow(window);
            else Windows.RemoveAt(i);
        }
        LanguageChanged?.Invoke(null, EventArgs.Empty);
    }

    /// <summary>
    /// Registers a WPF window for refresh when the active application language changes.
    /// </summary>
    public static void RegisterWindow(Window window)
    {
        if (!Windows.Any(reference => reference.TryGetTarget(out var existing) && ReferenceEquals(existing, window)))
        {
            Windows.Add(new WeakReference<Window>(window));
            window.Loaded += (_, _) => ApplyToWindow(window);
        }
        window.Closed += (_, _) => Windows.RemoveAll(reference => !reference.TryGetTarget(out var existing) || ReferenceEquals(existing, window));
        ApplyToWindow(window);
    }

    /// <summary>
    /// Translates a catalog key or supported dynamic message into the active application language.
    /// </summary>
    public static string Translate(string? value)
    {
        if (string.IsNullOrEmpty(value)) return value ?? string.Empty;
        if (Catalog.TryGetValue(value, out var translation))
            return _language == Chinese ? value : _language == French ? translation.Fr : translation.En;
        foreach (var entry in Catalog)
        {
            if (string.Equals(value, entry.Value.En, StringComparison.Ordinal))
                return _language == Chinese ? entry.Key : _language == French ? entry.Value.Fr : entry.Value.En;
            if (string.Equals(value, entry.Value.Fr, StringComparison.Ordinal))
                return _language == Chinese ? entry.Key : _language == French ? entry.Value.Fr : entry.Value.En;
        }
        var translated = value;
        foreach (var fragment in DynamicFragments.OrderByDescending(item => Math.Max(item.En.Length, item.Fr.Length)))
        {
            translated = translated.Replace(fragment.En, fragment.Source, StringComparison.Ordinal)
                .Replace(fragment.Fr, fragment.Source, StringComparison.Ordinal);
        }
        if (_language == Chinese) return translated;
        foreach (var fragment in DynamicFragments.OrderByDescending(item => item.Source.Length))
            translated = translated.Replace(fragment.Source, _language == French ? fragment.Fr : fragment.En, StringComparison.Ordinal);
        return translated;
    }

    private static void ApplyToWindow(Window window)
    {
        window.Title = Translate(window.Title);
        ApplyToElement(window, new HashSet<DependencyObject>());
    }

    private static void ApplyToElement(DependencyObject element, HashSet<DependencyObject> visited)
    {
        if (!visited.Add(element))
            return;

        if (element is TextBlock textBlock) textBlock.SetCurrentValue(TextBlock.TextProperty, Translate(textBlock.Text));
        if (element is SettingRowControl settingRow)
        {
            settingRow.Title = Translate(settingRow.Title);
            settingRow.Description = Translate(settingRow.Description);
        }
        if (element is HeaderedContentControl headered && headered.Header is string header)
            headered.SetCurrentValue(HeaderedContentControl.HeaderProperty, Translate(header));
        if (element is ContentControl content && content.Content is string contentText)
            content.SetCurrentValue(ContentControl.ContentProperty, Translate(contentText));

        // ItemsControl keeps ComboBoxItem instances in Items rather than reliably
        // exposing every item through the ordinary logical/visual child traversal.
        if (element is ItemsControl itemsControl)
        {
            foreach (var item in itemsControl.Items.OfType<DependencyObject>())
                ApplyToElement(item, visited);
        }

        if (element is ContentControl { Content: DependencyObject contentChild })
            ApplyToElement(contentChild, visited);

        foreach (var child in LogicalTreeHelper.GetChildren(element).OfType<DependencyObject>())
            ApplyToElement(child, visited);

        // UserControls such as SettingRowControl host their own XAML visual tree.
        // Walking it is necessary for nested labels and value chips to refresh too.
        if (element is Visual)
        {
            for (var index = 0; index < VisualTreeHelper.GetChildrenCount(element); index++)
                ApplyToElement(VisualTreeHelper.GetChild(element, index), visited);
        }

        if (element is Popup { Child: DependencyObject popupChild })
            ApplyToElement(popupChild, visited);
    }
}
