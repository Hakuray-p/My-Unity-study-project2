using System;
using UnityEngine;

// 保存并应用独立于战役进度的音量、帧率、亮度和分辨率设置。
public static class GameSettings
{
    private const string volumeKey = "ArkCard.Settings.Volume"; // 总音量的保存键
    private const string frameRateKey = "ArkCard.Settings.FrameRate"; // 帧率选项的保存键
    private const string brightnessKey = "ArkCard.Settings.Brightness"; // 画面亮度的保存键
    private const string resolutionKey = "ArkCard.Settings.Resolution"; // 分辨率选项的保存键
    public static readonly Vector2Int[] Resolutions = { new Vector2Int(1920, 1080), new Vector2Int(2560, 1440) }; // 可选择的分辨率
    public static int ResolutionIndex => PlayerPrefs.GetInt(resolutionKey, 0); // 当前分辨率选项
    public static readonly int[] FrameRates = { 30, 60, 90, 120, 144, -1 }; // 可选择的帧率上限
    public static event Action BrightnessChanged; // 通知场景更新画面亮度
    public static float MasterVolume => PlayerPrefs.GetFloat(volumeKey, 1f); // 当前总音量
    public static int FrameRateIndex => PlayerPrefs.GetInt(frameRateKey, 1); // 当前帧率选项
    public static float Brightness => PlayerPrefs.GetFloat(brightnessKey, 1f); // 相对原画面的亮度

    // 每次启动时清理上一次运行的场景监听。
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetListeners()
    {
        BrightnessChanged = null;
    }

    // 进入任何场景前应用已保存的音量、帧率和分辨率。
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ApplySavedSettings()
    {
        AudioListener.volume = MasterVolume;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = FrameRates[FrameRateIndex];
        ApplyResolution(ResolutionIndex);
    }

    // 调整所有游戏声音，并保留选择。
    public static void SetMasterVolume(float volume)
    {
        AudioListener.volume = volume;
        PlayerPrefs.SetFloat(volumeKey, volume);
        PlayerPrefs.Save();
    }

    // 关闭垂直同步的帧率接管，并设置所选上限。
    public static void SetFrameRate(int index)
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = FrameRates[index];
        PlayerPrefs.SetInt(frameRateKey, index);
        PlayerPrefs.Save();
    }

    // 保存分辨率选择并应用到游戏窗口。
    public static void SetResolution(int index)
    {
        ApplyResolution(index);
        PlayerPrefs.SetInt(resolutionKey, index);
        PlayerPrefs.Save();
    }

    // 调整分辨率并保留当前窗口或全屏模式。
    private static void ApplyResolution(int index)
    {
        Vector2Int resolution = Resolutions[index];
        Screen.SetResolution(resolution.x, resolution.y, Screen.fullScreenMode);
    }

    // 保存画面亮度并通知正在显示的场景。
    public static void SetBrightness(float brightness)
    {
        PlayerPrefs.SetFloat(brightnessKey, brightness);
        PlayerPrefs.Save();
        BrightnessChanged?.Invoke();
    }
}
