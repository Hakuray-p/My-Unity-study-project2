using System.Collections.Generic;
using UnityEngine;

// 查询城市赛事资源与事件内容，卡牌数据从 CardListSO 获取。
public static class CampaignCatalog
{
    private static CardListSO cardDatabase; // 卡牌数据库，由 DataManager 在开场时传入

    private static CampaignCatalogData content; // 城市赛事总表资源
    private static CampaignCatalogData Content => content != null ? content :
        content = Resources.Load<CampaignCatalogData>("Campaign/FirstCityCampaign"); // 首次查询时读取配置

    // 城市里的事件点内容，位置由场景里的交互点决定
    private static readonly List<CampaignEventData> events = new List<CampaignEventData>
    {
        new CampaignEventData
        {
            eventId = "first_light_event_01",
            cityId = "first_light",
            displayName = "调查裂痕",
            startText = "南侧桥前的裂痕里夹着一张无主卡。我来查旧记录，你愿意去看看卡背和周围的光吗？拿不准的地方先记着，我们回来一起核对。",
            objectiveText = "裂痕已经出现，去南侧楼梯下的桥前调查。",
            completeText = "你将纪念卡轻轻扶正，纷乱的光点渐渐归于平静。裂痕消失了，卡背上留下了一行字：下次，一起参赛。",
            reviewText = "确认了，是旧卡残留的思念随着赛事共鸣，并不是谁在破坏比赛。共鸣已经平息。来历还有些细节要向商人核对，我把不确定的部分留出来，我们一起看。",
            goldReward = 20,
            cardRewardId = 1202,
            cardRewardName = "自奏圣乐·嬉游曲恶魔"
        },
        new CampaignEventData
        {
            eventId = "first_light_event_cat",
            cityId = "first_light",
            displayName = "混沌教官的深夜加练",
            dialogueOnly = true,
            startText = "猫姬把雫叫到训练馆，准备进行一次临时加练。",
            objectiveText = "完成猫姬的专属事件对话。",
            completeText = "猫姬的深夜加练结束了。",
            reviewText = "猫姬还是会抢着提示，不过现在会先等雫自己想一会儿。",
            cardRewardName = "无"
        },
        new CampaignEventData
        {
            eventId = "first_light_event_rebecca",
            cityId = "first_light",
            displayName = "未被定性的搜查令",
            dialogueOnly = true,
            startText = "蕾贝卡发现档案室少了两页记录，准备重新核对借阅痕迹。",
            objectiveText = "完成蕾贝卡的专属事件对话。",
            completeText = "蕾贝卡重新整理了搜查结论。",
            reviewText = "蕾贝卡已经学会先核对证据，再下结论。",
            cardRewardName = "无"
        },
        new CampaignEventData
        {
            eventId = "first_light_event_fei",
            cityId = "first_light",
            displayName = "被撕毁的赛程表",
            dialogueOnly = true,
            startText = "绯把改期通知踩进了泥里，坚持要和雫马上打一场。",
            objectiveText = "完成绯的专属事件对话。",
            completeText = "绯答应下次先确认安排，再发起挑战。",
            reviewText = "绯还是想随时切磋，不过已经会先问雫有没有空。",
            cardRewardName = "无"
        },
        new CampaignEventData
        {
            eventId = "first_light_event_merchant",
            cityId = "first_light",
            displayName = "非卖品的折旧价",
            dialogueOnly = true,
            startText = "商人不肯出售柜中的旧卡，却每天都把它擦得很干净。",
            objectiveText = "完成商人的专属事件对话。",
            completeText = "商人决定继续按约定保管那张旧卡。",
            reviewText = "商人还是把人情写进账本，却没有把所有东西都标成商品。",
            cardRewardName = "无"
        },
        new CampaignEventData
        {
            eventId = "first_light_event_kong",
            cityId = "first_light",
            displayName = "芦苇荡里的停顿音",
            dialogueOnly = true,
            startText = "空在河岸听见了旧卡传来的微弱声音，想找个人一起坐一会儿。",
            objectiveText = "完成空的专属事件对话。",
            completeText = "空终于主动邀请雫陪自己听完河岸的风声。",
            reviewText = "空还是喜欢安静，但不会再把想说的话一个人留着。",
            cardRewardName = "无"
        },
        new CampaignEventData
        {
            eventId = "first_light_event_penguin",
            cityId = "first_light",
            displayName = "被推翻的三重构想",
            dialogueOnly = true,
            startText = "企鹅的赛程表同时遇到三处变故，整套安排眼看就要失控。",
            objectiveText = "完成企鹅的专属事件对话。",
            completeText = "企鹅采用了雫的调度方案，重新排好了赛程。",
            reviewText = "企鹅仍然重视规则，但开始接受临时调整也可以有秩序。",
            cardRewardName = "无"
        },
        new CampaignEventData
        {
            eventId = "first_light_event_archer",
            cityId = "first_light",
            displayName = "未出手的第十一箭",
            dialogueOnly = true,
            startText = "神秘弓兵愿意谈起三年前那场没有等到对手的比赛。",
            objectiveText = "完成神秘弓兵的专属事件对话。",
            completeText = "神秘弓兵把旧式弓弦的标记交给了雫。",
            reviewText = "神秘弓兵仍然说话直接，但愿意把自己的失误也摆到桌面上。",
            cardRewardName = "无"
        }
    };

    public static IReadOnlyList<CityData> Cities => Content.cities; // 所有城市

    // 按 id 取城市，找不到返回 null
    public static CityData GetCity(string cityId)
    {
        return Content.cities.Find(city => city.cityId == cityId);
    }

    // 按 id 取比赛，找不到返回 null
    public static MatchData GetMatch(string matchId)
    {
        return Content.matches.Find(match => match.matchId == matchId);
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
