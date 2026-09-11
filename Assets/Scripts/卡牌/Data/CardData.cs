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
    public CardRarity rarity = CardRarity.Common; // 稀有度
    public string archetype; // 职业 / 阵营
    public int shopPrice; // 商店售价
    public int unlockPoints; // 解锁需要的积分门槛
    public int attack;// 攻击力
    public int health;              // 生命值
    public Sprite image;             // 图片
    [TextArea]
    public string effectDescription; // 效果文本
    public TriggerType triggerType; // 效果触发类型
    public EffectType effectType;   // 效果种类
    public int[] effectValue;       // 效果参数 
    public ConditionType effectCondition;     // 效果发动条件
    public PassiveType passiveType; // 被动效果类型
    public AudioClip attackAudio;// 攻击音效
    public AudioClip enterAudio;// 进场音效
}
