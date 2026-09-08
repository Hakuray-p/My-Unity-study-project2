using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

public class HandContainer : MonoBehaviour
{
    public List<CardController> handCards = new ();
    [SerializeField]
    private float angleGap = 5;
    [SerializeField]
    private float radius = 15;
    [SerializeField]
    private float centerY = 15;
    
    private Sequence sequence;
    public void AddCard(CardController card)
    {
        if (card == null) return;
        if (handCards == null) handCards = new List<CardController>();
        handCards.Add(card);
        card.transform.SetParent(this.transform);
        RefreshCards();
    }

    public void RemoveCard(CardController card)
    {
        if (card == null || handCards == null) return;
        sequence?.Kill();
        card.transform.SetParent(null);
        handCards.Remove(card);
        RefreshCards();
    }

    
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
            Vector3 targetRotation = new Vector3(0f, 0f, angle);
            float rad = angle * Mathf.Deg2Rad;
            // 计算目标位置，使卡牌沿着一个圆弧分布
            Vector3 targetPosition = new Vector3(
                -radius * Mathf.Sin(rad),
                radius * Mathf.Cos(rad) - centerY,
                0f
            );

            CardController card = handCards[i];
            if (card == null) continue;
            sequence.Join(card.transform.DOLocalRotate(targetRotation, 0.5f));
            sequence.Join(card.transform.DOLocalMove(targetPosition, 0.5f));
            int sortingOrder = i + 50;
            sequence.JoinCallback(() =>
            {
                var group = card.GetComponent<SortingGroup>();
                if (group != null) group.sortingOrder = sortingOrder;
            });
        }
    }
}
