using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 战斗侧的数据入口，卡组和赛事数据都从这里取
public class DataManager : MonoBehaviour
{
    public CardListSO cardListSO; // 卡牌数据库
    //public List<DeckData> deckDataList = new();


    // 从 StreamingAssets 读卡组文件
    public DeckData LoadDecks(int deckId)
    {
        DeckData deckData =
            JsonTool.LoadJson<DeckData>($"{Application.streamingAssetsPath}/Decks/Deck0{deckId}.json");
        return deckData;
    }

    // 按 id 取赛事数据
    public MatchData GetMatch(string matchId)
    {
        return CampaignCatalog.GetMatch(matchId);
    }

    // 按 id 取城市数据
    public CityData GetCity(string cityId)
    {
        return CampaignCatalog.GetCity(cityId);
    }

    // 取当前玩家的出战卡组
    public List<int> GetPlayerDeck()
    {
        return CampaignSession.Instance.GetBattleDeck();
    }

    // 优先读卡组文件，没有就退回内置卡组
    public List<int> GetDeck(int deckId)
    {
        DeckData authored = LoadDecks(deckId);
        return authored != null && authored.cardDataList != null && authored.cardDataList.Count > 0
            ? new List<int>(authored.cardDataList)
            : CampaignCatalog.GetDeck(deckId);
    }

}
