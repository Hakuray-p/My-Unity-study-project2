using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

// 选牌界面，把选中的卡按网格摆好并把相机移过去
public class SelectContainer : MonoBehaviour
{
    // 最大行数
    private int maxRow = 5;
    private int maxCol = 6; // 最大列数
    private float gap = 3f; // 卡牌间距

    public Vector3 cameraPos; // 相机停靠位置
    // 把待选的卡摆成网格并把相机移过去
    public void ShowSelect(List<CardController> cards)
    {
        // 设置所有卡牌的位置
        for (int i = 0; i < cards.Count; i++)
        {
            int row = i / maxCol;
            int col = i % maxCol;
            if (row >= maxRow) break; // 超过最大行数则停止摆放
            var targetPos = new Vector3(col * gap, 0, -row * gap);
            cards[i].transform.SetParent(this.transform);
            cards[i].transform.DOLocalMove(targetPos, 0.6f);

            cards[i].cardDisplay.ShowBack(false);
        }
        Camera.main.transform.DOMove(cameraPos, 0.3f);
    }
}
