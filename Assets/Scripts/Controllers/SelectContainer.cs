using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class SelectContainer : MonoBehaviour
{
    // 最大行数
    private int maxRow = 5;
    private int maxCol = 6;
    private float gap = 3f;

    public Vector3 cameraPos;
    public void ShowSelect(List<CardController> cards)
    {
        // 设置所有卡牌的位置
        for (int i = 0; i < cards.Count; i++)
        {
            int row = i / maxCol;
            int col = i % maxCol;
            if (row >= maxRow) break; // 超过最大行数则停止摆放
            Vector3 targetPos = new Vector3(col * gap, 0, -row * gap);
            cards[i].transform.SetParent(this.transform);
            cards[i].transform.DOLocalMove(targetPos, 0.6f);
            
            cards[i].cardDisplay.ShowBack(false);
        }
        Camera.main.transform.DOMove(cameraPos, 0.3f);
    }
}
