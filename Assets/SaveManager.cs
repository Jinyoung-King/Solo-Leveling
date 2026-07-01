using UnityEngine;
using System;

public class SaveManager : MonoBehaviour
{
    private GameManager gameManager;

    const string KEY_GOLDEN_DISK = "GoldenDisk";
    const string KEY_LOGS = "SavedLogs";
    const string KEY_TIME = "LastSaveTime";
    const string KEY_LEVEL = "ServerLevel";
    const string KEY_UPGRADE_COST = "UpgradeCost";
    const string KEY_GATE = "CurrentGate";

    public void Initialize(GameManager gm)
    {
        gameManager = gm;
    }

    public void SaveGame()
    {
        if (gameManager == null) return;

        PlayerPrefs.SetString(KEY_LOGS, gameManager.logs.ToString());
        PlayerPrefs.SetInt(KEY_LEVEL, gameManager.serverLevel);
        PlayerPrefs.SetString(KEY_UPGRADE_COST, gameManager.upgradeCost.ToString());
        PlayerPrefs.SetInt(KEY_GOLDEN_DISK, gameManager.goldenDisks);
        PlayerPrefs.SetInt(KEY_GATE, gameManager.currentGate);

        for (int i = 0; i < gameManager.units.Count; i++)
        {
            PlayerPrefs.SetInt("Unit_" + i, gameManager.units[i].count);
        }

        if (gameManager.equipments != null)
        {
            for (int i = 0; i < gameManager.equipments.Count; i++)
            {
                PlayerPrefs.SetInt("Equip_" + i, gameManager.equipments[i].level);
            }
        }
        PlayerPrefs.SetString("GachaCost", gameManager.gachaCost.ToString());

        PlayerPrefs.SetString(KEY_TIME, DateTime.Now.ToBinary().ToString());
        PlayerPrefs.Save();
    }

    public void LoadGame()
    {
        if (gameManager == null) return;
        if (!PlayerPrefs.HasKey(KEY_LOGS)) return;

        string logsStr = PlayerPrefs.GetString(KEY_LOGS, "0");
        long.TryParse(logsStr, out gameManager.logs);

        gameManager.serverLevel = PlayerPrefs.GetInt(KEY_LEVEL, 1);
        long.TryParse(PlayerPrefs.GetString(KEY_UPGRADE_COST, "100"), out gameManager.upgradeCost);

        gameManager.goldenDisks = PlayerPrefs.GetInt(KEY_GOLDEN_DISK, 0);
        gameManager.currentGate = PlayerPrefs.GetInt(KEY_GATE, 1);

        for (int i = 0; i < gameManager.units.Count; i++)
        {
            int savedCount = PlayerPrefs.GetInt("Unit_" + i, 0);
            gameManager.units[i].LoadCount(savedCount);
        }

        if (gameManager.equipments != null)
        {
            for (int i = 0; i < gameManager.equipments.Count; i++)
            {
                gameManager.equipments[i].level = PlayerPrefs.GetInt("Equip_" + i, 0);
            }
        }
        long.TryParse(PlayerPrefs.GetString("GachaCost", "50000"), out gameManager.gachaCost);

        if (PlayerPrefs.HasKey(KEY_TIME))
        {
            string timeStr = PlayerPrefs.GetString(KEY_TIME);
            long temp = 0;
            if (long.TryParse(timeStr, out temp))
            {
                DateTime lastTime = DateTime.FromBinary(temp);

                TimeSpan timeDiff = DateTime.Now - lastTime;
                double secondsPassed = timeDiff.TotalSeconds;

                if (secondsPassed > 0)
                {
                    long revenuePerSec = gameManager.GetTotalRevenue();
                    long offlineReward = (long)(secondsPassed * revenuePerSec);

                    if (offlineReward > 0)
                    {
                        gameManager.logs += offlineReward;
                        Debug.Log($"[Offline] {secondsPassed:F0}s elapsed, reward: {offlineReward}");
                        gameManager.ShowRewardPopup(secondsPassed, offlineReward);
                    }
                }
            }
        }
    }
}
