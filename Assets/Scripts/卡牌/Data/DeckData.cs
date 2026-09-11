using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
// 一套卡组的数据，只存卡牌编号
public class DeckData
{
    public List<int> cardDataList = new(); // 卡牌编号列表
}
