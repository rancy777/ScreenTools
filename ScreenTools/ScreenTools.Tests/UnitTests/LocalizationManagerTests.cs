using ScreenTools;
using Xunit;

namespace ScreenTools.Tests.UnitTests;

public class LocalizationManagerTests
{
    [Fact]
    public void Translate_SwitchesBetweenAllSupportedLanguages()
    {
        try
        {
            LocalizationManager.SetLanguage(LocalizationManager.English);
            Assert.Equal("Screenshot settings", LocalizationManager.Translate("截图设置"));
            Assert.Equal("Capture mode", LocalizationManager.Translate("截图模式"));
            Assert.Equal("Full screen", LocalizationManager.Translate("全屏"));
            Assert.Equal("Region", LocalizationManager.Translate("区域"));
            Assert.Equal("After-capture action", LocalizationManager.Translate("截图后行为"));
            Assert.Equal("Show quick actions", LocalizationManager.Translate("保存后显示操作条"));
            Assert.Equal("Save only", LocalizationManager.Translate("仅保存"));
            Assert.Equal("Save and pin", LocalizationManager.Translate("保存并贴到屏幕"));
            Assert.Equal("Replay audio", LocalizationManager.Translate("回录音频"));
            Assert.Equal("Video only", LocalizationManager.Translate("仅画面"));

            LocalizationManager.SetLanguage(LocalizationManager.French);
            Assert.Equal("Paramètres des captures", LocalizationManager.Translate("截图设置"));
            Assert.Equal("Mode de capture", LocalizationManager.Translate("截图模式"));
            Assert.Equal("Plein écran", LocalizationManager.Translate("全屏"));
            Assert.Equal("Zone", LocalizationManager.Translate("区域"));
            Assert.Equal("Action après capture", LocalizationManager.Translate("截图后行为"));
            Assert.Equal("Afficher les actions rapides", LocalizationManager.Translate("保存后显示操作条"));
            Assert.Equal("Enregistrer uniquement", LocalizationManager.Translate("仅保存"));
            Assert.Equal("Enregistrer et épingler", LocalizationManager.Translate("保存并贴到屏幕"));
            Assert.Equal("Audio de la relecture", LocalizationManager.Translate("回录音频"));
            Assert.Equal("Image uniquement", LocalizationManager.Translate("仅画面"));

            LocalizationManager.SetLanguage(LocalizationManager.Chinese);
            Assert.Equal("截图设置", LocalizationManager.Translate("截图设置"));
            Assert.Equal("截图设置", LocalizationManager.Translate("Paramètres des captures"));
        }
        finally
        {
            LocalizationManager.SetLanguage(LocalizationManager.DetectSystemLanguage());
        }
    }

    [Fact]
    public void Translate_CanSwitchAnAlreadyTranslatedDynamicString()
    {
        try
        {
            LocalizationManager.SetLanguage(LocalizationManager.English);
            var english = LocalizationManager.Translate("最近状态");
            Assert.Equal("Latest status", english);

            LocalizationManager.SetLanguage(LocalizationManager.French);
            Assert.Equal("Dernier état", LocalizationManager.Translate(english));

            LocalizationManager.SetLanguage(LocalizationManager.Chinese);
            Assert.Equal("最近状态", LocalizationManager.Translate("Dernier état"));
        }
        finally
        {
            LocalizationManager.SetLanguage(LocalizationManager.DetectSystemLanguage());
        }
    }

    [Fact]
    public void Translate_CoversRuntimeErrorsAndDynamicallyBuiltMessages()
    {
        try
        {
            LocalizationManager.SetLanguage(LocalizationManager.English);
            Assert.Equal("The replay is still being exported. Please wait.", LocalizationManager.Translate("回录仍在导出，请稍候。"));
            Assert.Equal("PNG image", LocalizationManager.Translate("PNG 图像"));
            Assert.Equal("Recording started. System-audio capture is disabled.", LocalizationManager.Translate("录制已开始。当前未启用系统声音。"));
            Assert.Equal("Screenshot copied to the clipboard: image.png", LocalizationManager.Translate("截图已复制到剪贴板：image.png"));
            Assert.Equal("Replay export was cancelled. The replay buffer is still available.", LocalizationManager.Translate("回放导出已取消，回放缓存仍然可用。"));
            Assert.Equal("MP4 export was cancelled; the captured frame sequence was preserved.", LocalizationManager.Translate("MP4 导出已取消，已保留采集到的帧序列。"));
            Assert.Equal("Process timeout: ffmpeg.exe after 180 seconds.", LocalizationManager.Translate("进程超时：ffmpeg.exe，超时时间为 180 秒。"));
            Assert.Equal("Shutdown cleanup failed: test failure", LocalizationManager.Translate("关闭应用时清理失败：test failure"));
            Assert.Equal("The destination directory could not be resolved.", LocalizationManager.Translate("无法解析目标目录。"));
            Assert.Equal("LOCAL", LocalizationManager.Translate("本地"));
            Assert.Equal("RECORD", LocalizationManager.Translate("录制标记"));
            Assert.Equal("ACTIVE PROFILE", LocalizationManager.Translate("当前配置"));
            Assert.Equal(
                "Screen resolution is too high (3840x2160, estimated memory use 126 MB), exceeding the memory budget of 500 MB. Reduce display scaling or capture a smaller region.",
                LocalizationManager.Translate("屏幕分辨率过高（3840x2160，预计占用 126 MB），超过内存预算 500 MB。请降低显示缩放或选择区域截图。"));

            LocalizationManager.SetLanguage(LocalizationManager.French);
            Assert.Equal("La relecture est toujours en cours d’exportation. Veuillez patienter.", LocalizationManager.Translate("回录仍在导出，请稍候。"));
            Assert.Equal("Image PNG", LocalizationManager.Translate("PNG 图像"));
            Assert.Equal("L’enregistrement a commencé. La capture du son système est désactivée.", LocalizationManager.Translate("录制已开始。当前未启用系统声音。"));
            Assert.Equal("Capture d’écran copiée dans le presse-papiers : image.png", LocalizationManager.Translate("截图已复制到剪贴板：image.png"));
            Assert.Equal("L’export de la relecture a été annulé. Le tampon de relecture est toujours disponible.", LocalizationManager.Translate("回放导出已取消，回放缓存仍然可用。"));
            Assert.Equal("L’export MP4 a été annulé ; la séquence d’images capturées a été conservée.", LocalizationManager.Translate("MP4 导出已取消，已保留采集到的帧序列。"));
            Assert.Equal("Délai d’exécution dépassé : ffmpeg.exe après 180 secondes.", LocalizationManager.Translate("进程超时：ffmpeg.exe，超时时间为 180 秒。"));
            Assert.Equal("Échec du nettoyage à la fermeture de l’application : test failure", LocalizationManager.Translate("关闭应用时清理失败：test failure"));
            Assert.Equal("Impossible de déterminer le dossier de destination.", LocalizationManager.Translate("无法解析目标目录。"));
            Assert.Equal("LOCAL", LocalizationManager.Translate("本地"));
            Assert.Equal("ENREGISTREMENT", LocalizationManager.Translate("录制标记"));
            Assert.Equal("PROFIL ACTIF", LocalizationManager.Translate("当前配置"));
            Assert.Equal(
                "La résolution de l’écran est trop élevée (3840x2160, mémoire estimée à 126 Mo), au-delà de la limite mémoire de 500 Mo. Réduisez la mise à l’échelle de l’affichage ou capturez une zone plus petite.",
                LocalizationManager.Translate("屏幕分辨率过高（3840x2160，预计占用 126 MB），超过内存预算 500 MB。请降低显示缩放或选择区域截图。"));
        }
        finally
        {
            LocalizationManager.SetLanguage(LocalizationManager.DetectSystemLanguage());
        }
    }

    [Theory]
    [InlineData("fr-FR", LocalizationManager.French)]
    [InlineData("fr", LocalizationManager.French)]
    [InlineData("zh-Hans", LocalizationManager.Chinese)]
    [InlineData("zh-CN", LocalizationManager.Chinese)]
    [InlineData("unknown", LocalizationManager.English)]
    public void NormalizeLanguage_UsesSupportedLanguageCodes(string input, string expected)
    {
        Assert.Equal(expected, LocalizationManager.NormalizeLanguage(input));
    }
}