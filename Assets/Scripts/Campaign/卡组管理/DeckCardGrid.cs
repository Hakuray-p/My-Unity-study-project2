using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// 一块卡牌网格，构筑区和卡库都用它，格子外观取自网格里摆好的第一个格子。
public sealed class DeckCardGrid
{
    private readonly GameObject slotTemplate; // 模板格子，网格里的第一个子物体
    private readonly Action<int> onLeftClick; // 左键点格子的回调
    private readonly Action<int> onRightClick; // 右键点格子的回调，用不到时传 null
    private readonly Action<int> onDrop; // 拖拽落下的回调，用不到时传 null
    private readonly RectTransform dropArea; // 允许落下的目标区域
    private readonly List<GameObject> slots = new List<GameObject>(); // 生成出来的格子

    // 用网格里的第一个格子当格子模板
    public DeckCardGrid(RectTransform root, Action<int> onLeftClick, Action<int> onRightClick, RectTransform dropArea, Action<int> onDrop)
    {
        this.onLeftClick = onLeftClick;
        this.onRightClick = onRightClick;
        this.dropArea = dropArea;
        this.onDrop = onDrop;
        slotTemplate = root.GetChild(0).gameObject;
        AddSlot(slotTemplate);
    }

    // 按卡牌列表刷新格子，counts 是每格角标要写的张数，传 null 时不标角标
    public void Refresh(IList<int> cardIds, IList<int?> counts)
    {
        while (slots.Count < cardIds.Count) AddSlot(Object.Instantiate(slotTemplate, slotTemplate.transform.parent, false));

        for (int i = 0; i < cardIds.Count; i++)
        {
            int cardId = cardIds[i];
            int? count = counts == null ? null : counts[i];
            ApplySlot(slots[i], cardId > 0 ? CampaignCatalog.GetCardData(cardId) : null, count);
        }

        for (int i = cardIds.Count; i < slots.Count; i++) ApplySlot(slots[i], null, null);
    }

    // 把卡图刷到格子上，空位收起卡图
    private static void ApplySlot(GameObject slot, CardData card, int? count)
    {
        Transform cardArt = slot.transform.Find("CardArt");
        Image cardImage = cardArt.GetComponent<Image>();
        cardImage.enabled = card != null;
        cardImage.sprite = card == null ? null : card.image;
        DeckCardCountBadge.Set(cardArt, count);
    }

    // 给一个格子挂上输入组件，并把序号和回调填进去
    private void AddSlot(GameObject slot)
    {
        DeckCardSlotHandler handler = slot.GetComponent<DeckCardSlotHandler>() ?? slot.AddComponent<DeckCardSlotHandler>();
        handler.index = slots.Count;
        handler.onLeftClick = onLeftClick;
        handler.onRightClick = onRightClick;
        handler.dropArea = dropArea;
        handler.onDrop = onDrop;
        slots.Add(slot);
    }
}