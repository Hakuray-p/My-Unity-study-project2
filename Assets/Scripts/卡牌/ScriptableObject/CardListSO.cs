using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ArkCardsDatabase", menuName = "RX-未知裂痕/Card List SO")]
public class CardListSO : ScriptableObject
{
    public List<CardData> cards = new();

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
