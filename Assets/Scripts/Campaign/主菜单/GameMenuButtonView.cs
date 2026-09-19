using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 显示主菜单按钮被选中时的文字放大和下划线。
public sealed class GameMenuButtonView : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler
{
    private Button button; // 当前按钮
    private TMP_Text label; // 当前按钮文字
    private RectTransform underline; // 当前按钮的选中横线
    private Vector3 normalLabelScale; // 未选中时的文字缩放

    // 初始化按钮文字和选中横线。
    private void Awake()
    {
        button = GetComponent<Button>();
        label = GetComponentInChildren<TMP_Text>(true);
        normalLabelScale = label.transform.localScale;
        CreateUnderline();
        ApplySelected(false);
    }

    // 隐藏失效按钮的选中状态。
    private void OnDisable()
    {
        if (underline != null) underline.gameObject.SetActive(false);
        if (label != null) label.transform.localScale = normalLabelScale;
    }

    // 显示当前按钮的选中状态。
    public void OnSelect(BaseEventData eventData)
    {
        ApplySelected(true);
    }

    // 隐藏当前按钮的选中状态。
    public void OnDeselect(BaseEventData eventData)
    {
        ApplySelected(false);
    }

    // 鼠标移入时同步键盘导航的选中按钮。
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button.interactable) button.Select();
    }

    // 创建显示在按钮文字下方的横线。
    private void CreateUnderline()
    {
        GameObject underlineObject = new GameObject("SelectionUnderline", typeof(RectTransform), typeof(Image));
        underlineObject.transform.SetParent(transform, false);
        underline = underlineObject.GetComponent<RectTransform>();
        underline.anchorMin = new Vector2(0.5f, 0f);
        underline.anchorMax = new Vector2(0.5f, 0f);
        underline.pivot = new Vector2(0.5f, 1f);
        underline.anchoredPosition = new Vector2(0f, -1f);
        underline.sizeDelta = new Vector2(112f, 3f);

        Image underlineImage = underlineObject.GetComponent<Image>();
        underlineImage.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
        underlineImage.color = new Color(1f, 0.78f, 0.25f, 1f);
        underlineImage.raycastTarget = false;
        underlineObject.transform.SetAsLastSibling();
    }

    // 应用文字缩放和横线显示状态。
    private void ApplySelected(bool selected)
    {
        if (label == null) return;
        label.transform.localScale = selected ? normalLabelScale * 1.08f : normalLabelScale;
        if (underline != null) underline.gameObject.SetActive(selected);
    }
}
