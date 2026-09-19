using System.Collections.Generic;

// 远征内容的静态总表：城市、事件、比赛三张表写死在代码里，卡牌数据从 CardListSO 拿。
public static class CampaignCatalog
{
    private static CardListSO cardDatabase; // 卡牌数据库，由 DataManager 在开场时传入

    // 目前只有一座城市，一张表串起这座城的比赛、支线和通关奖励
    private static readonly List<CityData> cities = new List<CityData>
    {
        new CityData
        {
            cityId = "first_light",
            displayName = "First Light",
            sceneName = "One_City_DAY",
            requiredPoints = 10,
            championMatchId = "first_light_champion",
            availableMatchIds = new List<string>
            {
                "first_light_practice",
                "first_light_public_01",
                "first_light_public_02",
                "first_light_public_03",
                "first_light_public_04",
                "first_light_champion"
            }
        }
    };

    // 城市里的事件点内容，位置由场景里的交互点决定
    private static readonly List<CampaignEventData> events = new List<CampaignEventData>
    {
        new CampaignEventData
        {
            eventId = "first_light_event_01",
            cityId = "first_light",
            displayName = "调查裂痕",
            startText = "巷口刚才出现了一道奇怪的裂痕，里面还夹着一张没有主人的卡牌。能陪我去确认一下吗？",
            objectiveText = "裂痕已经出现，去南侧楼梯下的桥前调查。",
            completeText = "裂痕消失了。",
            reviewText = "那道裂痕已经消失了。谢谢你当时愿意陪我调查。",
            goldReward = 20,
            cardRewardId = 1202,
            cardRewardName = "自奏圣乐·嬉游曲恶魔"
        }
    };

    // 第一城的五场比赛，按 prerequisiteMatchId 串成一条线
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
            enemyDeckId = 1
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
            rewardCardIds = new List<int> { 1203 }
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
            rewardCardIds = new List<int> { 1209 }
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
            rewardCardIds = new List<int> { 1205 }
        },
        new MatchData
        {
            matchId = "first_light_public_04",
            cityId = "first_light",
            displayName = "Merchant's Open",
            matchType = MatchType.Public,
            opponentId = "Merchant",
            pointReward = 5,
            goldReward = 40,
            enemyDeckId = 2,
            rewardCardIds = new List<int> { 1204 }
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
            rewardCardIds = new List<int> { 1208 }
        }
    };

    public static IReadOnlyList<CityData> Cities => cities; // 所有城市

    // 按 id 取城市，找不到返回 null
    public static CityData GetCity(string cityId)
    {
        return cities.Find(city => city.cityId == cityId);
    }

    // 按 id 取比赛，找不到返回 null
    public static MatchData GetMatch(string matchId)
    {
        return matches.Find(match => match.matchId == matchId);
    }

    // 按 id 取事件，找不到返回 null
    public static CampaignEventData GetEvent(string eventId)
    {
        return events.Find(campaignEvent => campaignEvent.eventId == eventId);
    }

    // 卡牌数据库是否已经就位
    public static bool HasCardDatabase => cardDatabase != null;

    // 由 DataManager 在开场时把卡牌数据库交进来，之后才能查卡
    public static void SetCardDatabase(CardListSO database)
    {
        cardDatabase = database;
    }

    // 按 id 取卡牌数据，数据库还没就位时返回 null
    public static CardData GetCardData(int cardId)
    {
        return cardDatabase == null ? null : cardDatabase.GetData(cardId);
    }

    // 生成第一城商店的商品列表，数据库为空时按编号区间兜底
    public static IReadOnlyList<CardShopEntry> GetFirstCityShop()
    {
        var entries = new List<CardShopEntry>();
        if (cardDatabase != null && cardDatabase.cards != null && cardDatabase.cards.Count > 0)
        {
            foreach (CardData cardData in cardDatabase.cards)
            {
                if (cardData != null) entries.Add(CreateShopEntry(cardData));
            }

            return entries;
        }

        for (int cardId = 1001; cardId <= 1017; cardId++) entries.Add(CreateShopEntry(cardId));
        for (int cardId = 1101; cardId <= 1112; cardId++)
        {
            if (cardId != 1103) entries.Add(CreateShopEntry(cardId));
        }
        return entries;
    }

    // 数据库里查不到时，按编号区间给一个兜底商品
    private static CardShopEntry CreateShopEntry(int cardId)
    {
        CardData cardData = GetCardData(cardId);
        if (cardData != null) return CreateShopEntry(cardData);

        CardRarity rarity = GetLegacyRarity(cardId);
        return new CardShopEntry
        {
            cardId = cardId,
            rarity = rarity,
            price = rarity == CardRarity.Common ? 30 : rarity == CardRarity.Rare ? 60 : 100,
            archetype = cardId >= 1100 ? "先锋" : "干员"
        };
    }

    // 用卡牌数据生成商品，卡上没填售价时按稀有度补默认值
    private static CardShopEntry CreateShopEntry(CardData cardData)
    {
        CardRarity rarity = cardData.rarity;
        int price = cardData.shopPrice > 0 ? cardData.shopPrice : GetDefaultPrice(rarity);
        return new CardShopEntry
        {
            cardId = cardData.index,
            rarity = rarity,
            price = price,
            archetype = string.IsNullOrEmpty(cardData.archetype) ?
                (cardData.index >= 1100 ? "先锋" : "干员") : cardData.archetype
        };
    }

    // 取一张卡的稀有度，卡牌数据缺失时用兜底表判断
    public static CardRarity GetRarity(int cardId)
    {
        CardData cardData = GetCardData(cardId);
        return cardData == null ? GetLegacyRarity(cardId) : cardData.rarity;
    }

    // 卡牌数据缺失时按编号区间判断稀有度
    private static CardRarity GetLegacyRarity(int cardId)
    {
        if (cardId == 1107 || (cardId >= 1006 && cardId <= 1008) || cardId == 1010 ||
            cardId == 1012 || cardId == 1017 || cardId == 1111) return CardRarity.Limited;
        if (cardId == 1005 || cardId == 1102 || cardId == 1106 || cardId == 1108) return CardRarity.Rare;
        return CardRarity.Common;
    }

    // 按稀有度给一个默认售价
    private static int GetDefaultPrice(CardRarity rarity)
    {
        return rarity == CardRarity.Common ? 30 : rarity == CardRarity.Rare ? 60 : 100;
    }

    private const int DeckCopies = 3; // 预置卡组里每种卡的份数

    private static readonly int[] starterCardIds = // 玩家初始卡组，1 费到 5 费混编
    {
        1203, 1205, 1214, 1209, 1201, 1202, 1211, 1204, 1206, 1213
    };

    private static readonly int[] enemyCardIds = // 练习和第一场用的敌方卡组，偏低费
    {
        1220, 1203, 1209, 1215, 1218, 1245, 1268, 1231, 1263, 1258
    };

    private static readonly int[] eliteEnemyCardIds = // 后面几场用的敌方卡组，1 费到 5 费铺满
    {
        1243, 1232, 1205, 1216, 1261, 1264, 1204, 1206, 1207, 1212
    };

    // 取某套预置卡组，0 是玩家初始卡组，2 是后面的敌方卡组，其余编号走初级敌方卡组
    public static List<int> GetDeck(int deckId)
    {
        int[] cardIds = deckId == 0 ? starterCardIds : deckId == 2 ? eliteEnemyCardIds : enemyCardIds;
        var deck = new List<int>();
        for (int copy = 0; copy < DeckCopies; copy++)
        {
            foreach (int cardId in cardIds) deck.Add(cardId);
        }

        return deck;
    }
}
