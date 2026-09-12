

using UnityEngine;


// 触发类枚举
public enum TriggerType
{
    None = 0,
    Start = 1,
    End = 2,
    Enter = 3,
    Died = 4,
    Hurt = 5,
    BeatDown = 6,// 击倒
    Cast = 7, // 施放
}


// 效果类的枚举
public enum EffectType
{
    None = 0,
    DealDamageToEnemy = 1,   // 选择伤害的敌人
    Draw = 3,                  // 抽卡
    BuffAlly = 4,               // buff指定的队友
    BuffSelf = 5,               // buff自身
    DamageAll = 8,              // 全场伤害
    BuffAlliesAll = 9,         // buff全体友军
    AddCostMax = 10,        // 增加最大部署值
    HealAlly = 11,            // 治疗指定友军
    EnemyBackHand = 12,     // 敌方返回手牌
    DealDamageToRandomEnemy = 13,
    DamageAllEnemy = 14,
    BuffEnemy = 15,
    HeallRandomAllies = 16,
    SlienceEnemy = 17,
    OtherBackHand = 18,  // 其他干员返回手牌
    DigMumber = 19,
    DestoryEnemy = 20,
    SearchMumberCostUp = 21,// 检索1名干员，费用x以上
    SearchMumberTrigger = 23, // 检索一名带有某触发条件的干员
    Henshin = 24,
    Revive = 25, // 复活
    AddCost = 26,// 增加费用
    BackHandAddCost = 27,// 返回手牌并增加费用
    BuffLowHpAlly = 28,// 生命值低于2的干员加攻击力
    AttackAgain = 29,// 允许再次攻击
    DropEnemyHand = 30,// 使敌方随机弃牌
    DropAndDraw = 31,// 弃牌后抽牌
    HealPlayer = 32,// 治疗博士
}


// 被动效果类的枚举
public enum PassiveType
{
    None = 0,
    Rush = 1,          // 冲锋
    Guard = 2,        // 守护
    Swingle = 3,      // 旋风斩
}

// 卡牌类型
public enum CardType
{
    MUMBER = 1,   // 角色卡
    SPELL = 2,    // 法术卡
}

// 效果发动条件
public enum ConditionType
{
    None = 0,
    TwoMumber = 1,
    ThreeMoreHand = 2, // 手牌数大于等于3
    HasAmiya = 3, // 有阿米娅存在
    HasEnemy = 4,// 有任何敌人存在
    HasAlly = 5,// 有任何友军存在
    HasDiedMumber = 6, // 墓地有干员
}

// 卡牌当前所在区域
public enum CardState
{
    Deck = 0,     // 在牌堆中
    Hand = 1,     // 在手牌中
    Field = 2,    // 在场上
    Graveyard = 3,// 在墓地中
}

// 战斗音效类型
public enum AudioType
{
    DrawCard = 1,
    Shuffle = 2, //洗牌
    Destroy = 3,
    Damage = 4,
    Heal = 5,
    NextTurn = 6,
    Effect = 7,
    Summon = 8,
}
