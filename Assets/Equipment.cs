[System.Serializable]
public class Equipment
{
    public string equipName;
    public string rarity; // "Common", "Rare", "Epic", "Legendary"
    public int level = 0; // 중복 획득 시 레벨업
    public float statBonus; // 추가 수치 (LPS 가산 또는 배율)
    public string bonusType; // "LPS", "Multiplier", "Click"
    public string description;

    public Equipment(string name, string rarity, float bonus, string type, string desc)
    {
        equipName = name;
        this.rarity = rarity;
        statBonus = bonus;
        bonusType = type;
        description = desc;
        level = 0;
    }
}
