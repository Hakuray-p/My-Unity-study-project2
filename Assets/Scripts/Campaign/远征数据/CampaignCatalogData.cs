using System.Collections.Generic;
using UnityEngine;

// 在 Inspector 中维护城市与赛事配置，不把静态内容写入存档。
[CreateAssetMenu(menuName = "ArkCard/城市赛事总表", fileName = "CityCampaign")]
public class CampaignCatalogData : ScriptableObject
{
    public List<CityData> cities = new List<CityData>(); // 可用的城市配置
    public List<MatchData> matches = new List<MatchData>(); // 可用的赛事配置
}
