using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

// 战场上的卡牌排列，负责增删卡牌和重新居中摆放
public class FieldController : MonoBehaviour
{
    public PlayerController player; // 玩家控制器
    public List<CardController> cards; // 场上的卡牌
    private Sequence sequence; // 排列用的动画序列
    /// <summary>
    /// 重新排列所有卡牌
    /// </summary>
    public void RefreshCards()
    {
        if (cards == null || cards.Count == 0) return;
        if (sequence != null) sequence.Kill();
        sequence = DOTween.Sequence();
        // 按照固定的间距居中排列所有卡牌
        float spacing = 2.5f; // 卡牌之间的间距
        float startX = -((cards.Count - 1) * spacing) / 2; // 计算起始位置
        for (int i = 0; i < cards.Count; i++)
        {
            var targetPosition = new Vector3(startX + i * spacing, 0, 0);
            sequence.Join(
                    cards[i].transform.DOLocalMove(targetPosition, 0.5f)
            );
        }
    }

    // 把卡牌放进战场并重新排列
    public void AddCard(CardController card)
    {
        if (card == null) return;
        if (cards == null) cards = new List<CardController>();
        cards.Add(card);
        card.transform.parent = this.transform;
        card.transform.localRotation = Quaternion.Euler(Vector3.zero);
        RefreshCards();
    }

    // 把卡牌移出战场并重新排列
    public void RemoveCard(CardController card)
    {
        if (card == null || cards == null) return;
        cards.Remove(card);
        card.transform.parent = null;
        RefreshCards();
    }

    // 取这张牌左右相邻的卡牌
    public List<CardController> GetNeighborCards(CardController card)
    {
        var neighbors = new List<CardController>();
        int index = cards.IndexOf(card);
        if (index < 0) return neighbors;
        if (index > 0)
        {
            neighbors.Add(cards[index - 1]);
        }
        if (index < cards.Count - 1)
        {
            neighbors.Add(cards[index + 1]);
        }
        return neighbors;
    }
}
