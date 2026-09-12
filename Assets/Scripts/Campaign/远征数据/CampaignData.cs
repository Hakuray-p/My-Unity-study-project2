using System;
using System.Collections.Generic;
using UnityEngine;

// 比赛类型
public enum MatchType
{
    Practice = 0, // 练习赛
    Public = 1, // 公开赛
    Champion = 2 // 冠军赛
}


public enum CardRarity
{
    Common = 0, // R
    Rare = 1, // SR
    Limited = 2 // UR
}

// 场景里可交互对象的种类
public enum WorldInteractionType
{
    Match = 0, // 比赛入口
    Shop = 1, // 商店
    Event = 2 // 事件点
}

// 战斗结果
public enum BattleOutcome
{
    None = 0,
    PlayerWin = 1,
    PlayerLoss = 2
}

// 一座城市的静态配置，来自 CampaignCatalog，存档里不保存
[Serializable]
public class CityData
{
    public string cityId; // 城市标识，和存档里的 currentCityId 对应
    public string displayName; // 界面上显示的城市名
    public string sceneName; // 这座城市的场景名，进城时按它加载
    public int requiredPoints; // 打冠军赛需要的赛事积分
    public string championMatchId; // 冠军赛的比赛 id
    public List<string> availableMatchIds = new List<string>(); // 这座城市里能打的比赛
}

// 一场比赛的静态配置
[Serializable]
public class MatchData
{
    public string matchId; // 比赛标识
    public string cityId; // 属于哪座城市
    public string displayName; // 界面上显示的比赛名
    public MatchType matchType; // 练习赛 / 公开赛 / 冠军赛
    public string opponentId; // 对手标识
    public int pointReward; // 赢了给多少赛事积分
    public int goldReward; // 赢了给多少金币
    public int enemyDeckId; // 敌方卡组编号
    public string prerequisiteMatchId; // 前置比赛，没打完不能进
    public bool awardsBadge; // 赢了是否发徽章
    public List<int> rewardCardIds = new List<int>(); // 首胜奖励的卡牌
}

// 一个场景事件的配置和文案
[Serializable]
public class CampaignEventData
{
    public string eventId; // 事件标识，存档里按它记录做没做过
    public string cityId; // 属于哪座城市
    public string displayName; // 界面上显示的事件名
    [TextArea] public string startText; // 刚开始时的描述
    [TextArea] public string objectiveText; // 当前目标的描述
    [TextArea] public string completeText; // 完成后的描述
    [TextArea] public string reviewText; // 做完之后再回来看的描述
    public int goldReward; // 完成奖励的金币
    public int cardRewardId; // 完成奖励的卡牌 id
    public string cardRewardName; // 奖励卡牌的名字，只用于显示
}

// 商店货架上的一项
[Serializable]
public class CardShopEntry
{
    public int cardId; // 卖的卡牌 id
    public CardRarity rarity; // 稀有度，决定归到哪个标签下
    public int price; // 售价
    public string archetype; // 卡组流派标签
}

// 一张卡的 UR 色相变体存档，记下买这张卡时摇到的颜色种子
[Serializable]
public class CardHoloVariantSaveData
{
    public int cardId; // 卡牌 id
    public int colorSeed; // 颜色种子，预览时按它算出这张卡的专属色相
}

// 一座城市的存档状态
[Serializable]
public class CitySaveData
{
    public string cityId; // 城市标识
    public int leaguePoints; // 在这座城市攒到的赛事积分
    public bool championDefeated; // 冠军赛过没过
    public List<string> completedMatchIds = new List<string>(); // 已经打完的比赛
    public List<string> activeEventIds = new List<string>(); // 正在进行的事件
    public List<string> resolvedEventIds = new List<string>(); // 已经解决的事件
}

// 战斗里一张卡的状态快照，用于中途退出后接着打
[Serializable]
public class BattleCardSnapshot
{
    public int cardId; // 卡牌 id
    public CardState state; // 这张卡当时在牌库 / 手牌 / 场上 / 墓地
    public int attack; // 攻击力
    public int health; // 当前生命值
    public int healthMax; // 生命值上限
    public bool ableAttack; // 当时能不能攻击
    public bool silenced; // 当时是否被沉默
}

// 战斗里一个玩家的状态快照
[Serializable]
public class BattlePlayerSnapshot
{
    public int playerId; // 玩家编号
    public int health; // 当前生命值
    public int healthMax; // 生命值上限
    public int cost; // 剩余费用
    public int costMax; // 费用上限
    public bool isInTurn; // 是不是轮到这一方
    public int fatigueLevel; // 疲劳层数
    public List<BattleCardSnapshot> cards = new List<BattleCardSnapshot>(); // 这个玩家所有的卡
}

// 一整场战斗的快照
[Serializable]
public class BattleSnapshot
{
    public string matchId; // 比赛标识
    public int turn; // 当前回合数
    public int activePlayerId; // 该谁行动
    public string randomStateJson; // 随机数生成器的状态
    public List<BattlePlayerSnapshot> players = new List<BattlePlayerSnapshot>(); // 双方玩家
}

// 从大地图进战斗时携带的上下文，战斗场景靠它知道要打哪场
[Serializable]
public class BattleLaunchContext
{
    public string matchId; // 打哪场比赛
    public string cityId; // 从哪座城市来的
    public string returnScene; // 打完回到哪个场景
    public int randomSeed; // 本场敌人AI行为
    public int enemyDeckId; // 敌方卡组编号
    public BattleSnapshot snapshot; // 有值就是中途接着打，没有就是新开一局
}

// 一场战斗打完的结算结果
[Serializable]
public class BattleResult
{
    public string matchId; // 打的是哪场
    public string cityId; // 在哪座城市打的
    public BattleOutcome outcome; // 输赢
    public int leaguePoints; // 结算后拿到多少赛事积分
    public int gold; // 结算后拿到多少金币
    public bool firstWin; // 是不是首胜
    public bool badgeAwarded; // 有没有发徽章
    public List<int> rewardCardIds = new List<int>(); // 拿到的卡牌
}

// 整份远征存档，写进 JSON 的就是这一个对象
[Serializable]
public class CampaignSaveData
{
    public int version = 7; // 存档版本号，改存档结构时要往上加
    public string currentCityId; // 玩家当前在哪座城市
    public Vector3 playerPosition; // 玩家在大地图上的位置
    public List<string> unlockedCityIds = new List<string>(); // 已经解锁的城市
    public List<CitySaveData> cityStates = new List<CitySaveData>(); // 每座城市的进度
    public List<int> collectedCardIds = new List<int>(); // 已经拥有的卡牌
    public List<int> sharedDeckCardIds = new List<int>(); // 通用卡组
    public int currency; // 金币数量
    public List<string> badgeIds = new List<string>(); // 已经拿到的徽章
    public List<int> deckDraftCardIds = new List<int>(); // 卡组管理里还没保存的草稿
    public List<int> lastValidDeckCardIds = new List<int>(); // 最近一次合法的卡组，草稿改坏时回退用
    public List<CardHoloVariantSaveData> cardHoloVariants = new List<CardHoloVariantSaveData>(); // 每张 UR 卡的颜色
    public BattleSnapshot pendingBattle; // 没打完的战斗，有值就能接着打
    public string pendingMatchId; // 没打完的是哪场比赛
}
