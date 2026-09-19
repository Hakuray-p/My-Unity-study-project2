using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 让主城和战斗共用静态设置页面，调整后立即显示当前数值。
public sealed class PauseSettingsView : MonoBehaviour
{
    [SerializeField] private PauseMenuView pauseMenu; // 返回的暂停菜单
    [SerializeField] private Slider volumeSlider; // 总音量百分比
    [SerializeField] private Slider frameRateSlider; // 帧率档位
    [SerializeField] private Slider brightnessSlider; // 画面亮度百分比
    [SerializeField] private Toggle fullHdToggle; // 1920×1080 分辨率选项
    [SerializeField] private Toggle quadHdToggle; // 2560×1440 分辨率选项
    [SerializeField] private TMP_Text volumeText; // 音量数值
    [SerializeField] private TMP_Text frameRateText; // 帧率上限说明
    [SerializeField] private TMP_Text brightnessText; // 亮度数值
    [SerializeField] private Button backButton; // 返回暂停菜单
    [SerializeField] private Button resetButton; // 恢复默认设置

    // 接入场景中预置的控件。
    private void Awake()
    {
        volumeSlider.onValueChanged.AddListener(ChangeVolume);
        frameRateSlider.onValueChanged.AddListener(ChangeFrameRate);
        brightnessSlider.onValueChanged.AddListener(ChangeBrightness);
        fullHdToggle.onValueChanged.AddListener(ChangeFullHd);
        quadHdToggle.onValueChanged.AddListener(ChangeQuadHd);
        backButton.onClick.AddListener(pauseMenu.CloseSettings);
        resetButton.onClick.AddListener(ResetSettings);
    }

    // 每次打开时同步跨场景共享的设置。
    private void OnEnable()
    {
        Refresh();
    }

    // 刷新滑条、勾选状态与数值，不重复触发设置写入。
    private void Refresh()
    {
        volumeSlider.SetValueWithoutNotify(GameSettings.MasterVolume * 100f);
        frameRateSlider.SetValueWithoutNotify(GameSettings.FrameRateIndex);
        brightnessSlider.SetValueWithoutNotify(GameSettings.Brightness * 100f);
        volumeText.text = Mathf.RoundToInt(volumeSlider.value) + "%";
        int frameRate = GameSettings.FrameRates[GameSettings.FrameRateIndex];
        frameRateText.text = frameRate < 0 ? "不限" : frameRate + " FPS";
        brightnessText.text = Mathf.RoundToInt(brightnessSlider.value) + "%";
        Toggle selectedResolution = GameSettings.ResolutionIndex == 0 ? fullHdToggle : quadHdToggle;
        selectedResolution.SetIsOnWithoutNotify(true);
    }

    // 更新游戏总音量。
    private void ChangeVolume(float value)
    {
        GameSettings.SetMasterVolume(value / 100f);
        Refresh();
    }

    // 更新游戏帧率上限。
    private void ChangeFrameRate(float value)
    {
        GameSettings.SetFrameRate(Mathf.RoundToInt(value));
        Refresh();
    }

    // 更新场景亮度，暂停中的画面也立即生效。
    private void ChangeBrightness(float value)
    {
        GameSettings.SetBrightness(value / 100f);
        Refresh();
    }

    // 勾选全高清时应用对应分辨率。
    private void ChangeFullHd(bool selected)
    {
        if (selected) GameSettings.SetResolution(0);
    }

    // 勾选二倍高清时应用对应分辨率。
    private void ChangeQuadHd(bool selected)
    {
        if (selected) GameSettings.SetResolution(1);
    }

    // 恢复完整音量、六十帧上限、原始亮度和默认分辨率。
    private void ResetSettings()
    {
        GameSettings.SetMasterVolume(1f);
        GameSettings.SetFrameRate(1);
        GameSettings.SetBrightness(1f);
        GameSettings.SetResolution(0);
        Refresh();
    }
}
