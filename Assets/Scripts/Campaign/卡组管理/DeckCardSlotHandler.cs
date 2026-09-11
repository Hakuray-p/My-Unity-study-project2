using System;
using UnityEngine;
using UnityEngine.EventSystems;

// 卡牌格子的输入接收器，把左键、右键和拖拽分开交给回调。
public sealed class DeckCardSlotHandler : MonoBehaviour, IPointerClickHandler, IDragHandler, IEndDragHandler
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
        else if (eventData.button == PointerEventData.InputButton.Right && onRightClick != null) onRightClick(index);
    }

    // 空实现，只为了让 Unity 把这个格子认成拖拽源
    public void OnDrag(PointerEventData eventData)
    {
    }

    // 松手时落在目标区域内才算拖放成功
    public void OnEndDrag(PointerEventData eventData)
    {
        if (onDrop == null || dropArea == null) return;
        if (RectTransformUtility.RectangleContainsScreenPoint(dropArea, eventData.position, eventData.pressEventCamera)) onDrop(index);
    }
}
