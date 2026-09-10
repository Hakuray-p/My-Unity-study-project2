public enum CardHoloTier
{
    None = 0,
    R = 1,
    SR = 2,
    UR = 3
}

public static class CardHoloTierMapper
{
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
