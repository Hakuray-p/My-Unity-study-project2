using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardController : MonoBehaviour
{
    public CardData cardData;
    public PlayerController player;

    public int mumberAtk;
    public int mumberHp;
    public int mumberHpMax;

    public bool ableAttack;
    //public bool inField;
    public CardState cardState;
    public CardDisplay cardDisplay;

    public bool isSlience;

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

    public void TakeDamage(int damage)
    {
        if (damage<=0) return;
        
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


    private void OnMouseDown()
    {
        if(!player.isInTurn) return;
        if(cardState != CardState.Field) return;
        if (cardData.triggerType == TriggerType.Cast) // 主动释放
        {
            GM.Ins.BM.EM.TriggerCardEffect(TriggerType.Cast, this);
        }
    }
}
