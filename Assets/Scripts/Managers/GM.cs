using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GM : MonoSingleton<GM>
{
    public BattleManager BM;
    public DataManager DM;
    public AudioManager AM;
    public UIManager UM;

    
    
    public void Start()
    {
        CampaignSession.Instance.ToString();
        if (AM != null) AM.Init();
        if (BM != null) BM.Init();
    }
}
