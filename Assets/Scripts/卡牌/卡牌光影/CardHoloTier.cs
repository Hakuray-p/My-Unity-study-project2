// 卡牌光影的三个档位
public enum CardHoloTier
{
    None = 0, // 未指定
    R = 1, // R 卡
    SR = 2, // SR 卡
    UR = 3 // UR 卡
}

// 把卡牌稀有度换成对应的光影档位
public static class CardHoloTierMapper
{
    // 把卡牌稀有度映射成光影档位
    public static CardHoloTier FromRarity(CardRarity rarity)
    {
        return rarity switch
        {
            CardRarity.Common => CardHoloTier.R,
            CardRarity.Rare => CardHoloTier.SR,
            CardRarity.Limited => CardHoloTier.UR,
            _ => CardHoloTier.None
        };
    }
}
