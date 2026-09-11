using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

// 手牌容器，把卡牌沿一段圆弧摊开
public class HandContainer : MonoBehaviour
{
    public List<CardController> handCards = new(); // 手牌
    [SerializeField]
    private float angleGap = 5; // 相邻卡牌的角度间隔
    [SerializeField]
    private float radius = 15; // 圆弧半径
    [SerializeField]
    private float centerY = 15; // 圆弧中心的纵向位置

    private Sequence sequence; // 摆牌动画序列
    // 把手牌放进手牌区并重新摆牌
    public void AddCard(CardController card)
    {
        if (card == null) return;
        if (handCards == null) handCards = new List<CardController>();
        handCards.Add(card);
        card.transform.SetParent(this.transform);
        RefreshCards();
    }

    // 把手牌移出手牌区并重新摆牌
    public void RemoveCard(CardController card)
    {
        if (card == null || handCards == null) return;
        sequence?.Kill();
        card.transform.SetParent(null);
        handCards.Remove(card);
        RefreshCards();
    }


    // 按圆弧重新摆好所有手牌
    public void RefreshCards()
    {
        if (handCards == null || handCards.Count == 0) return;

        // 终止上一个动画
        sequence?.Kill();
        sequence = DOTween.Sequence();

        int cardCount = handCards.Count;
        float totalAngle = (cardCount - 1) * angleGap;//总的旋转角度
        float startAngle = totalAngle / 2f;//起始角度，使得卡牌在中心对称分布

        for (int i = 0; i < cardCount; i++)
        {
            // 计算每张卡牌的目标旋转角度
            float angle = startAngle - i * angleGap;
            var targetRotation = new Vector3(0f, 0f, angle);
            float rad = angle * Mathf.Deg2Rad;
            // 计算目标位置，使卡牌沿着一个圆弧分布
            var targetPosition = new Vector3(
                -radius * Mathf.Sin(rad),
                radius * Mathf.Cos(rad) - centerY,
                0f
            );

            CardController card = handCards[i];
            sequence.Join(card.transform.DOLocalRotate(targetRotation, 0.5f));
            sequence.Join(card.transform.DOLocalMove(targetPosition, 0.5f));
            int sortingOrder = i + 50;
            sequence.JoinCallback(() =>
            {
                SortingGroup group = card.GetComponent<SortingGroup>();
                if (group != null) group.sortingOrder = sortingOrder;
            });
        }
    }
}
