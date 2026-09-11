using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ArkCardsDatabase", menuName = "RX-未知裂痕/Card List SO")]
// 全部卡牌的数据表，商店和战斗都从这里查卡
public class CardListSO : ScriptableObject
{
    public List<CardData> cards = new(); // 所有卡牌

    // 按编号取卡，找不到返回 null
    public CardData GetData(int id)
    {
        foreach (var card in cards)
        {
            if (card.index == id)
            {
                return card;
            }
        }
        return null;
    }
}
