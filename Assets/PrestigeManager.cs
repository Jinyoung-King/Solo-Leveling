using UnityEngine;
using TMPro;

public class PrestigeManager : MonoBehaviour
{
    private GameManager gameManager;

    public void Initialize(GameManager gm)
    {
        gameManager = gm;
        if (gameManager.migrationPanel != null)
        {
            gameManager.migrationPanel.SetActive(false);
        }
    }

    public void OpenMigrationPopup()
    {
        if (gameManager == null) return;

        // 최소 100만 마나가 있어야 가능
        if (gameManager.logs < 1000000)
        {
            gameManager.terminalManager?.AddLog("<color=red><b>[ERROR] 최소 1.00M Mana가 필요합니다!</b></color>");
            return;
        }

        // 보상 계산 공식: 루트(현재 로그 / 100만)
        int potentialDisks = (int)Mathf.Sqrt(gameManager.logs / 1000000f);

        if (gameManager.migrationPanel != null)
        {
            gameManager.migrationPanel.SetActive(true);
            gameManager.migrationPanel.transform.SetAsLastSibling();

            if (gameManager.migrationInfoText != null)
            {
                gameManager.migrationInfoText.text =
                    $"그림자 영토를 다음 차원 게이트로 이전하겠습니까?\n\n" +
                    $"현재 그림자 군대 데이터는 <color=red>초기화</color>되지만\n" +
                    $"<color=yellow>어둠의 징표 {potentialDisks}개</color>를 획득합니다.\n\n" +
                    $"현재 보유: {gameManager.goldenDisks}개\n" +
                    $"총 마력 보너스: <color=green>+{(gameManager.goldenDisks + potentialDisks) * 10}%</color>";
            }
        }
    }

    public void CloseMigrationPopup()
    {
        if (gameManager == null) return;
        if (gameManager.migrationPanel != null) gameManager.migrationPanel.SetActive(false);
    }

    public void ConfirmMigration()
    {
        if (gameManager == null) return;

        // 받을 디스크 계산
        int potentialDisks = (int)Mathf.Sqrt(gameManager.logs / 1000000f);

        // 1. 디스크 지급
        gameManager.goldenDisks += potentialDisks;

        // 2. 데이터 초기화
        gameManager.logs = 0;
        gameManager.serverLevel = 1;
        gameManager.upgradeCost = 100;

        // 3. 유닛 초기화
        foreach (var u in gameManager.units)
        {
            u.count = 0;
            u.LoadCount(0); // 가격도 초기화
        }

        // 4. 저장 및 UI 갱신
        gameManager.SaveGame();
        gameManager.UpdateUI();
        CloseMigrationPopup();

        // 5. 멋진 로그 출력
        gameManager.terminalManager?.AddLog($"<color=#00FFFF><b>=== SHADOW MIGRATION COMPLETED ===</b></color>");
        gameManager.terminalManager?.AddLog($"<color=yellow>New Bonus: +{gameManager.goldenDisks * 10}% Mana Power!</color>");
    }
}
