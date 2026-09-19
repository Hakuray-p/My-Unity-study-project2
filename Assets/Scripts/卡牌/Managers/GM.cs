using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 战斗场景的总管理器，持有几个子管理器并在开局初始化
public class GM : MonoSingleton<GM>
{
    public BattleManager BM; // 战斗管理器
    public DataManager DM; // 数据管理器
    public AudioManager AM; // 音频管理器
    public UIManager UM; // 界面管理器



    // 开局先初始化音频和战斗
    public void Start()
    {
        CampaignSession.Instance.ToString();
        if (DM != null) CampaignCatalog.SetCardDatabase(DM.cardListSO);
        if (AM != null) AM.Init();
        if (BM != null) BM.Init();
    }
}
