using System.Collections.Generic;

public static class CampaignCatalog
{
    private static readonly List<CityData> cities = new List<CityData>
    {
        new CityData
        {
            cityId = "first_light",
            displayName = "First Light",
            sceneName = "HD_2D_Day",
            order = 0,
            requiredPoints = 10,
            championMatchId = "first_light_champion",
            availableMatchIds = new List<string>
            {
                "first_light_practice",
                "first_light_public_01",
                "first_light_public_02",
                "first_light_public_03",
                "first_light_champion"
            },
            cardRewardIds = new List<int> { 1001, 1002, 1003 }
        },
        new CityData
        {
            cityId = "glass_harbor",
            displayName = "Glass Harbor",
            sceneName = "HD_2D_Day",
            order = 1,
            requiredPoints = 18,
            championMatchId = "glass_harbor_champion",
            availableMatchIds = new List<string>
            {
                "glass_harbor_public_01",
                "glass_harbor_public_02",
                "glass_harbor_champion"
            },
            cardRewardIds = new List<int> { 1004, 1005, 1006 }
        }
    };

    private static readonly List<MatchData> matches = new List<MatchData>
    {
        new MatchData
        {
            matchId = "first_light_practice",
            cityId = "first_light",
            displayName = "Rookie Practice",
            matchType = MatchType.Practice,
            opponentId = "Mira",
            pointReward = 0,
            goldReward = 0,
            enemyDeckId = 1,
            fieldRuleId = "standard"
        },
        new MatchData
        {
            matchId = "first_light_public_01",
            cityId = "first_light",
            displayName = "Riverside Open",
            matchType = MatchType.Public,
            opponentId = "Jonah",
            pointReward = 2,
            goldReward = 20,
            prerequisiteMatchId = "first_light_practice",
            enemyDeckId = 1,
            fieldRuleId = "standard",
            rewardCardIds = new List<int> { 1001 }
        },
        new MatchData
        {
            matchId = "first_light_public_02",
            cityId = "first_light",
            displayName = "Lantern Open",
            matchType = MatchType.Public,
            opponentId = "Sera",
            pointReward = 3,
            goldReward = 30,
            prerequisiteMatchId = "first_light_public_01",
            enemyDeckId = 2,
            fieldRuleId = "lantern_field",
            rewardCardIds = new List<int> { 1002 }
        },
        new MatchData
        {
            matchId = "first_light_public_03",
            cityId = "first_light",
            displayName = "Sunset Open",
            matchType = MatchType.Public,
            opponentId = "Talia",
            pointReward = 5,
            goldReward = 40,
            prerequisiteMatchId = "first_light_public_02",
            enemyDeckId = 2,
            fieldRuleId = "standard",
            rewardCardIds = new List<int> { 1003 }
        },
        new MatchData
        {
            matchId = "first_light_champion",
            cityId = "first_light",
            displayName = "First Light Champion",
            matchType = MatchType.Champion,
            opponentId = "Captain Vale",
            pointReward = 0,
            goldReward = 80,
            prerequisiteMatchId = "first_light_public_03",
            awardsBadge = true,
            enemyDeckId = 2,
            fieldRuleId = "champion_arena",
            rewardCardIds = new List<int> { 1107 }
        },
        new MatchData
        {
            matchId = "glass_harbor_public_01",
            cityId = "glass_harbor",
            displayName = "Dockside Open",
            matchType = MatchType.Public,
            opponentId = "Ari",
            pointReward = 9,
            enemyDeckId = 2,
            fieldRuleId = "tide_field",
            rewardCardIds = new List<int> { 1004 }
        },
        new MatchData
        {
            matchId = "glass_harbor_public_02",
            cityId = "glass_harbor",
            displayName = "Moon Market Open",
            matchType = MatchType.Public,
            opponentId = "Lio",
            pointReward = 9,
            enemyDeckId = 2,
            fieldRuleId = "standard",
            rewardCardIds = new List<int> { 1005 }
        },
        new MatchData
        {
            matchId = "glass_harbor_champion",
            cityId = "glass_harbor",
            displayName = "Glass Harbor Champion",
            matchType = MatchType.Champion,
            opponentId = "The Cartographer",
            pointReward = 0,
            enemyDeckId = 2,
            fieldRuleId = "champion_arena",
            rewardCardIds = new List<int> { 1006 }
        }
    };

    public static IReadOnlyList<CityData> Cities => cities;

    public static CityData GetCity(string cityId)
    {
        return cities.Find(city => city.cityId == cityId);
    }

    public static MatchData GetMatch(string matchId)
    {
        return matches.Find(match => match.matchId == matchId);
    }

    public static IReadOnlyList<CardShopEntry> GetFirstCityShop()
    {
        List<CardShopEntry> entries = new List<CardShopEntry>();
        for (int cardId = 1001; cardId <= 1017; cardId++) entries.Add(CreateShopEntry(cardId));
        for (int cardId = 1101; cardId <= 1112; cardId++) entries.Add(CreateShopEntry(cardId));
        return entries;
    }

    private static CardShopEntry CreateShopEntry(int cardId)
    {
        CardRarity rarity = GetRarity(cardId);
        return new CardShopEntry
        {
            cardId = cardId,
            rarity = rarity,
            price = rarity == CardRarity.Common ? 30 : rarity == CardRarity.Rare ? 60 : 100,
            unlockPoints = rarity == CardRarity.Common ? 5 : rarity == CardRarity.Rare ? 10 : 15,
            archetype = cardId >= 1100 ? "先锋" : "干员"
        };
    }

    public static CardRarity GetRarity(int cardId)
    {
        if (cardId == 1103 || cardId == 1107) return CardRarity.Limited;
        if ((cardId >= 1005 && cardId <= 1008) || cardId == 1010 || cardId == 1012 || cardId == 1017 ||
            cardId == 1102 || cardId == 1106 || cardId == 1108 || cardId == 1111) return CardRarity.Rare;
        return CardRarity.Common;
    }

    public static List<int> GetDeck(int deckId)
    {
        // Deck 0 is the starter/AI fallback. Authored decks can replace this lookup later.
        return new List<int>
        {
            1112, 1016, 1001, 1002, 1003, 1004, 1005, 1006,
            1010, 1011, 1012, 1013, 1101, 1102, 1103, 1104,
            1105, 1106, 1107, 1108
        };
    }
}
