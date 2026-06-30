using UnityEngine;

[System.Serializable]
public class Unit
{
    public string unitName;
    public long revenuePerSec;
    public long baseCost;
    public long currentCost;
    public int count = 0;
    public Sprite iconSprite;

    public Unit(string name, int tier)
    {
        unitName = name;
        baseCost = (long)(10 * Mathf.Pow(10, tier));
        revenuePerSec = (long)(1 * Mathf.Pow(5, tier));
        currentCost = baseCost;
    }

    public void SetIcon(Sprite sprite)
    {
        this.iconSprite = sprite;
    }

    public void Buy()
    {
        count++;
        currentCost = (long)(currentCost * 1.5f);
    }

    public void LoadCount(int savedCount)
    {
        count = savedCount;
        currentCost = (long)(baseCost * Mathf.Pow(1.5f, savedCount));
    }
}
