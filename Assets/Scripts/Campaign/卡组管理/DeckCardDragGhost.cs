using UnityEngine;
using UnityEngine.UI;

// 拖拽卡牌时跟着鼠标走的卡图，同一个时间只存在一张。
public static class DeckCardDragGhost
{
    private static RectTransform ghost; // 当前跟着鼠标的卡图，没在拖拽时为 null

    // 照着源卡图建一张跟着鼠标走的卡图
    public static void Begin(Image source, Vector2 screenPosition)
    {
        End();
        var ghostObject = new GameObject("DeckCardDragGhost", typeof(RectTransform), typeof(Image));
        ghost = ghostObject.GetComponent<RectTransform>();
        ghost.SetParent(source.canvas.transform, false);
        ghost.sizeDelta = source.rectTransform.rect.size;
        ghost.SetAsLastSibling();

        Image ghostImage = ghostObject.GetComponent<Image>();
        ghostImage.sprite = source.sprite;
        ghostImage.raycastTarget = false;

        Move(screenPosition);
    }

    // 让卡图跟着鼠标
    public static void Move(Vector2 screenPosition)
    {
        if (ghost != null) ghost.position = screenPosition;
    }

    // 结束拖拽，把卡图收掉
    public static void End()
    {
        if (ghost == null) return;
        Object.Destroy(ghost.gameObject);
        ghost = null;
    }
}
