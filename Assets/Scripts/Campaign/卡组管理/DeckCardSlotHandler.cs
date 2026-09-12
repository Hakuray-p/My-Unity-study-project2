using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 卡牌格子的输入接收器，把左键、右键和拖拽分开交给回调。
public sealed class DeckCardSlotHandler : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Action<int> onLeftClick; // 左键回调
    public Action<int> onRightClick; // 右键回调，用不到时为 null
    public Action<int> onDrop; // 拖拽落下的回调，用不到时为 null
    public RectTransform dropArea; // 允许落下的目标区域
    public int index; // 格子序号

    // 左键和右键分开派发
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left) onLeftClick(index);
        else if (eventData.button == PointerEventData.InputButton.Right) onRightClick?.Invoke(index);
    }

    // 开始拖拽时把卡图提起来跟着鼠标
    public void OnBeginDrag(PointerEventData eventData)
    {
        DeckCardDragGhost.Begin(transform.Find("CardArt").GetComponent<Image>(), eventData.position);
    }

    // 拖拽过程中让卡图跟着鼠标
    public void OnDrag(PointerEventData eventData)
    {
        DeckCardDragGhost.Move(eventData.position);
    }

    // 松手时落在目标区域内才算拖放成功，卡图一起收掉
    public void OnEndDrag(PointerEventData eventData)
    {
        DeckCardDragGhost.End();
        if (dropArea == null) return;
        if (RectTransformUtility.RectangleContainsScreenPoint(dropArea, eventData.position, eventData.pressEventCamera)) onDrop?.Invoke(index);
    }
}
