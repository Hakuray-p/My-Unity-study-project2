using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 战斗中的一张卡牌实例，记录当前攻防和所在区域
public class CardController : MonoBehaviour
{
    public CardData cardData; // 对应的卡牌数据
    public PlayerController player; // 持有者

    public int mumberAtk; // 当前攻击力
    public int mumberHp; // 当前生命值
    public int mumberHpMax; // 生命值上限

    public bool ableAttack; // 本回合能不能攻击
    //public bool inField;
    public CardState cardState; // 当前所在区域（牌堆 / 手牌 / 场上 / 墓地）
    public CardDisplay cardDisplay; // 对应的显示组件

    public bool isSlience; // 是否被沉默

    // 用卡牌数据初始化这张实例
    public void Init(CardData _cardData, PlayerController _player)
    {
        cardData = _cardData;
        player = _player;
        mumberAtk = cardData.attack;
        mumberHp = cardData.health;
        mumberHpMax = cardData.health;
        isSlience = false;
        if (cardDisplay != null)
        {
            cardDisplay.Init(this);
            cardDisplay.UpdateDisplay();
            cardDisplay.ShowBack();
        }
    }

    // 扣血并广播受伤触发，血量归零就进死亡处理
    public void TakeDamage(int damage)
    {
        if (damage <= 0) return;

        mumberHp -= damage;
        if (GM.Ins != null && GM.Ins.AM != null) GM.Ins.AM.PlayAudio(AudioType.Damage);
        if (GM.Ins != null && GM.Ins.BM != null && GM.Ins.BM.EM != null)
            GM.Ins.BM.EM.TriggerCardEffect(TriggerType.Hurt, this);
        if (mumberHp <= 0)
        {
            if (GM.Ins != null && GM.Ins.BM != null) GM.Ins.BM.MumberDied(this);
        }

        if (cardDisplay != null) cardDisplay.UpdateDisplay();
    }

    // 回血，不超过上限
    public void Heal(int num)
    {
        mumberHp += num;
        if (GM.Ins != null && GM.Ins.AM != null) GM.Ins.AM.PlayAudio(AudioType.Heal);
        if (mumberHp >= mumberHpMax)
        {
            mumberHp = mumberHpMax;
        }
        if (cardDisplay != null) cardDisplay.UpdateDisplay();
    }


    // 点击场上的自己时触发主动效果
    private void OnMouseDown()
    {
        if (!player.isInTurn) return;
        if (cardState != CardState.Field) return;
        if (cardData.HasEffect(TriggerType.Cast)) // 主动释放
        {
            GM.Ins.BM.EM.TriggerCardEffect(TriggerType.Cast, this);
        }
    }
}
