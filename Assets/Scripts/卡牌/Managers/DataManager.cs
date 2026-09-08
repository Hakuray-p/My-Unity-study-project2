using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataManager : MonoBehaviour
{
    public CardListSO cardListSO;
    //public List<DeckData> deckDataList = new();
    
    
    public DeckData LoadDecks(int deckId)
    {
        DeckData deckData =
            JsonTool.LoadJson<DeckData>($"{Application.streamingAssetsPath}/Decks/Deck0{deckId}.json");
        return deckData;
    }

    public MatchData GetMatch(string matchId)
    {
        return CampaignCatalog.GetMatch(matchId);
    }

    public CityData GetCity(string cityId)
    {
        return CampaignCatalog.GetCity(cityId);
    }

    public List<int> GetPlayerDeck()
    {
        return CampaignSession.Instance.GetBattleDeck();
    }

    public List<int> GetDeck(int deckId)
    {
        DeckData authored = LoadDecks(deckId);
        return authored != null && authored.cardDataList != null && authored.cardDataList.Count > 0
            ? new List<int>(authored.cardDataList)
            : CampaignCatalog.GetDeck(deckId);
    }
    
}
