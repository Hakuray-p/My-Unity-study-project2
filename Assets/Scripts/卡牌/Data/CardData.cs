using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
// 一张卡牌的静态数据
public class CardData
{
    public int index;// 卡牌编号
    public CardType cardType;// 卡牌类型
    public string name;// 卡牌名字
    public int cost; // 部署费用
    public CardRarity rarity = CardRarity.Common; //实例化R卡
    public string archetype; // 阵营
    public int shopPrice; // 商店售价
    public int attack;// 攻击力
    public int health;  // 生命值
    public Sprite image;  // 图片
    [TextArea]
    public string effectDescription; // 效果文本
    public List<CardEffect> effects = new List<CardEffect>(); // 按列表顺序结算的效果
    public PassiveType passiveType; // 被动效果类型
    public AudioClip attackAudio;// 攻击音效
    public AudioClip enterAudio;// 进场音效

    // 这张卡有没有某个时点的效果
    public bool HasEffect(TriggerType triggerType)
    {
        foreach (CardEffect effect in effects)
        {
            if (effect.triggerType == triggerType) return true;
        }

        return false;
    }
}

[System.Serializable]
// 卡牌上的一个效果，在列表里的先后就是结算顺序
public class CardEffect
{
    public TriggerType triggerType; // 触发时点
    public EffectType effectType; // 效果种类
    public int[] effectValue; // 效果参数
    public ConditionType effectCondition; // 发动条件
}