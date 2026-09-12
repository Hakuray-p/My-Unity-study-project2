using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 卡图右下角的同名卡数量角标。
public static class DeckCardCountBadge
{
    // 在卡图右下角写 X几，传 null 时把角标收起来
    public static void Set(Transform cardArt, int? count)
    {
        Transform badge = cardArt.Find("CountText");
        if (count == null)
        {
            if (badge != null) badge.gameObject.SetActive(false);
            return;
        }

        if (badge == null) badge = Create(cardArt);
        badge.GetComponent<TMP_Text>().text = "X" + count.Value;
        badge.gameObject.SetActive(true);
    }

    // 在卡图右下角建一个角标文字
    private static Transform Create(Transform cardArt)
    {
        var badgeObject = new GameObject("CountText", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform badgeRect = badgeObject.GetComponent<RectTransform>();
        badgeRect.SetParent(cardArt, false);
        badgeRect.anchorMin = new Vector2(1f, 0f);
        badgeRect.anchorMax = new Vector2(1f, 0f);
        badgeRect.pivot = new Vector2(1f, 0f);
        badgeRect.anchoredPosition = new Vector2(-10f, 10f);
        badgeRect.sizeDelta = new Vector2(170f, 90f);

        TextMeshProUGUI badgeText = badgeObject.GetComponent<TextMeshProUGUI>();
        badgeText.fontSize = 56f;
        badgeText.fontStyle = FontStyles.Bold;
        badgeText.color = Color.white;
        badgeText.alignment = TextAlignmentOptions.BottomRight;
        badgeText.raycastTarget = false;
        badgeText.enableWordWrapping = false;
        return badgeRect;
    }
}
