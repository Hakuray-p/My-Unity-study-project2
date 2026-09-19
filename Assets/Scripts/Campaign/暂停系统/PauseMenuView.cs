using DG.Tweening;
using UnityEngine;

// 显示静态暂停界面，并让菜单从屏幕上方滑入中央。
public sealed class PauseMenuView : MonoBehaviour
{
    [SerializeField] private RectTransform canvasRect; // 暂停画布的可见范围
    [SerializeField] private RectTransform menuPanel; // 随动画移动的菜单内容
    [SerializeField] private float slideDuration = 0.35f; // 菜单滑入所需时间
    [SerializeField] private PauseSettingsView settingsView; // 场景预置的设置页面
    private Tween slideTween; // 本次滑入动画

    public bool SettingsOpen => settingsView.gameObject.activeSelf; // 设置页面是否打开

    // 激活画布并在暂停状态下播放滑入动画。
    public void Show()
    {
        CloseSettings();
        gameObject.SetActive(true);
        Canvas.ForceUpdateCanvases();
        slideTween?.Kill();
        menuPanel.anchoredPosition = new Vector2(0f, (canvasRect.rect.height + menuPanel.rect.height) * 0.5f);
        slideTween = menuPanel.DOAnchorPosY(0f, slideDuration).SetEase(Ease.OutCubic).SetUpdate(true);
    }

    // 关闭暂停画布，立即停止接收菜单点击。
    public void Hide()
    {
        gameObject.SetActive(false);
    }

    // 在暂停状态下切换到设置，不播放第二次入场动画。
    public void ShowSettings()
    {
        slideTween?.Kill();
        menuPanel.anchoredPosition = Vector2.zero;
        menuPanel.gameObject.SetActive(false);
        settingsView.gameObject.SetActive(true);
    }

    // 返回主暂停菜单，继续保持游戏暂停。
    public void CloseSettings()
    {
        settingsView.gameObject.SetActive(false);
        menuPanel.gameObject.SetActive(true);
    }

    // 关闭或卸载界面时停止动画，并恢复编辑时的中心位置。
    private void OnDisable()
    {
        slideTween?.Kill();
        slideTween = null;
        menuPanel.anchoredPosition = Vector2.zero;
    }
}
