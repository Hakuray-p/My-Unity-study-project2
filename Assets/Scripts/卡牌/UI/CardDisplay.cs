using System;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

// 卡牌的显示组件，把卡牌数据写成界面文字和卡图
public class CardDisplay : MonoBehaviour
{
    [Header("卡牌UI显示")]
    public TMP_Text nameText; // 卡名文字
    public TMP_Text costText; // 费用文字
    public TMP_Text atkText; // 攻击力文字
    public TMP_Text hpText; // 生命值文字
    public TMP_Text effectText; // 效果文字
    public SpriteRenderer cardImage; // 卡图
    public GameObject back; // 背面节点
    public GameObject front; // 正面节点
    public GameObject mumberCardLayout; // 干员卡专用布局
    public GameObject spellCardLayout; // 法术卡专用布局
    public GameObject summonOutline; // 召唤边框
    public GameObject atkOutline; // 攻击边框
    public GameObject specialOutline;// 特殊边框


    private CardController cardController; // 对应的卡牌控制器
    private int originOrder; // 放大前的渲染层

    public bool isBack => back != null && back.activeInHierarchy;

    // 绑定所属的卡牌控制器
    public void Init(CardController cardController)
    {
        this.cardController = cardController;
    }
    // 把卡牌数据刷到界面文字和卡图上
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

    // 切换显示卡背还是正面
    public void ShowBack(bool option = true)
    {
        if (back != null) back.SetActive(option);
        if (front != null) front.SetActive(!option);
    }

    // 显示 / 隐藏召唤高亮
    public void ShowSummoning(bool option)
    {
        if (summonOutline != null) summonOutline.SetActive(option);
    }

    // 显示 / 隐藏可攻击高亮
    public void ShowAttacking(bool option)
    {
        if (atkOutline != null) atkOutline.SetActive(option);
    }

    // 显示 / 隐藏选中高亮
    public void ShowSpecial(bool option)
    {
        if (specialOutline != null) specialOutline.SetActive(option);
    }

    // 预留的效果表现，暂时没内容
    public void DoEffect()
    {

    }

    // 预留的增益表现，暂时没内容
    public void DoBuff()
    {

    }
    private Tween tween; // 缩放动画
    private float baseScale = 1f; // 卡牌的基础缩放，放大时按它算倍率
    // 设定卡牌的基础缩放，并停掉正在跑的缩放动画
    public void SetBaseScale(float scale)
    {
        baseScale = scale;
        tween.Kill();
        transform.localScale = Vector3.one * scale;
    }

    // 放大 / 还原卡牌并调整渲染层级
    public void ZoomCard(bool option)
    {
        //if (locationType != LocationType.HAND) return;
        if (option)
        {
            tween.Kill();
            tween = transform.DOScale(baseScale * 1.4f, 0.3f);
            originOrder = GetComponent<SortingGroup>().sortingOrder;
            GetComponent<SortingGroup>().sortingOrder += 10;
        }
        else
        {
            tween.Kill();
            tween = transform.DOScale(baseScale, 0.3f);
            GetComponent<SortingGroup>().sortingOrder = originOrder;

        }
    }
    // 鼠标移入时放大卡牌
    private void OnMouseEnter()
    {
        ZoomCard(true);
    }
    // 鼠标移出时还原卡牌
    private void OnMouseExit()
    {
        ZoomCard(false);
    }
}
