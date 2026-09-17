using System;
using System.Collections.Generic;
using UnityEngine;

// 猫姬教学的固定步骤，也是续教时保存的位置。
public enum PracticeTutorialStep
{
    None, // 普通比赛不进行教学
    Health, // 认识双方生命
    Cost, // 认识部署费用
    Hand, // 认识手牌
    Stats, // 认识角色攻防
    Summon, // 召唤第一名角色
    EndFirstTurn, // 第一次结束回合
    EnemyGuard, // 猫姬召唤守卫
    AttackGuard, // 攻击守护角色
    CastSpell, // 发动法术
    Discard, // 为法术选择弃牌
    ViewGrave, // 打开墓地
    CloseGrave, // 关闭墓地查看
    EndSecondTurn, // 第二次结束回合
    EnemyPass, // 猫姬配合让过
    SummonCaster, // 召唤带有主动技能的角色
    CastAbility, // 点击主动技能
    Revive, // 选择复活目标
    AttackHero, // 直接攻击对手
    Completed, // 教学完成说明
    FreePlay // 自由完成练习赛
}

// 教学检查的实际战斗操作。
public enum TutorialAction
{
    Summon, // 角色召唤
    Spell, // 法术发动
    AttackCard, // 攻击角色
    AttackPlayer, // 攻击头像
    Ability, // 主动技能
    EndTurn, // 结束回合
    ViewGrave, // 查看墓地
    SelectTarget // 选择效果目标
}

// 一个教学步骤的说明。
[Serializable]
public class TutorialLesson
{
    public PracticeTutorialStep step; // 对应的步骤
    public string title; // 本步标题
    [TextArea(3, 8)] public string text; // 猫姬的教学说明
}

// 保存教学专用卡牌、固定牌序和说明，不进入正式收藏数据库。
[CreateAssetMenu(fileName = "CatPracticeTutorial", menuName = "RX-未知裂痕/猫姬教程")]
public sealed class PracticeTutorialData : ScriptableObject
{
    public CardData guardCard; // 仅在教学牌局使用的守护卡
    public List<int> playerDeck; // 玩家临时卡组及抽牌顺序
    public List<int> enemyDeck; // 猫姬临时卡组及抽牌顺序
    public TutorialLesson[] lessons; // 按步骤配置的中文说明
}
