using System;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class CardDisplay : MonoBehaviour
{
    [Header("卡牌UI显示")]
    public TMP_Text nameText;
    public TMP_Text costText;
    public TMP_Text atkText;
    public TMP_Text hpText;
    public TMP_Text effectText;
    public SpriteRenderer cardImage;
    public GameObject back;
    public GameObject front;
    public GameObject mumberCardLayout;
    public GameObject spellCardLayout;
    public GameObject summonOutline; // 召唤边框
    public GameObject atkOutline; // 攻击边框
    public GameObject specialOutline;// 特殊边框

 
    private CardController cardController;
    private int originOrder;
    
    public bool isBack => back != null && back.activeInHierarchy;

    public void Init(CardController cardController)
    {
        this.cardController = cardController;
    }
    public void UpdateDisplay()
    {
        if (cardController == null || cardController.cardData == null) return;
        // 更新基础信息

        if (nameText != null) nameText.text = cardController.cardData.name;
        if (costText != null) costText.text = cardController.cardData.cost.ToString();
        if (cardImage != null) cardImage.sprite = cardController.cardData.image;
        if (effectText != null)
        {
            effectText.alignment = TextAlignmentOptions.Center;
            effectText.enableWordWrapping = true;
            effectText.text = Regex.Unescape(cardController.cardData.effectDescription);
        }

        // 根据卡牌类型显示不同的布局
        // 更新随从卡的战斗属性
        if (cardController.cardData.cardType == CardType.MUMBER)
        {
            if (atkText != null) { atkText.gameObject.SetActive(true); atkText.text = cardController.mumberAtk.ToString(); }
            if (hpText != null) { hpText.gameObject.SetActive(true); hpText.text = cardController.mumberHp.ToString(); }
            if (mumberCardLayout != null) mumberCardLayout.SetActive(true);
            //spellCardLayout.SetActive(false);
        }
        // 如果是法术卡，可以在这里添加法术特有的显示逻辑
        else if (cardController.cardData.cardType == CardType.SPELL)
        {
            if (atkText != null) atkText.gameObject.SetActive(false);
            if (hpText != null) hpText.gameObject.SetActive(false);
            if (mumberCardLayout != null) mumberCardLayout.SetActive(false);
            //spellCardLayout.SetActive(true);
        }
    }

    public void ShowBack(bool option = true)
    {
        if (back != null) back.SetActive(option);
        if (front != null) front.SetActive(!option);
    }

    public void ShowSummoning(bool option)
    {
        if (summonOutline != null) summonOutline.SetActive(option);
    }

    public void ShowAttacking(bool option)
    {
        if (atkOutline != null) atkOutline.SetActive(option);
    }

    public void ShowSpecial(bool option)
    {
        if (specialOutline != null) specialOutline.SetActive(option);
    }

    public void DoEffect()
    {
        
    }

    public void DoBuff()
    {
        
    }
    private Tween tween;
    public void ZoomCard(bool option)
    {
        //if (locationType != LocationType.HAND) return;
        if (option)
        {
            tween.Kill();
            tween = transform.DOScale(1.4f, 0.3f);
            originOrder = GetComponent<SortingGroup>().sortingOrder;
            GetComponent<SortingGroup>().sortingOrder += 10;
        }
        else
        {
            tween.Kill();
            tween = transform.DOScale(1.0f, 0.3f);
            GetComponent<SortingGroup>().sortingOrder = originOrder;
            
        }
    }
    private void OnMouseEnter()
    {
        ZoomCard(true);
    }
    private void OnMouseExit()
    {
        ZoomCard(false);
    }
}
