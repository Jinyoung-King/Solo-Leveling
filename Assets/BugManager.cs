using UnityEngine;
using System.Collections;

public class BugManager : MonoBehaviour
{
    private GameManager gameManager;

    private float bugSpawnTimer = 0f;
    private float nextBugTime = 10f; // 첫 버그는 10초 뒤 출현
    private Coroutine bugEscapeRoutine;

    public void Initialize(GameManager gm)
    {
        gameManager = gm;
        if (gameManager.bugButton != null)
        {
            gameManager.bugButton.gameObject.SetActive(false);
            gameManager.bugButton.onClick.RemoveAllListeners();
            gameManager.bugButton.onClick.AddListener(OnClickBug);
        }
    }

    void Update()
    {
        if (gameManager == null || gameManager.bugButton == null) return;

        // 오버클럭 중이 아니고, 버그가 안 떠있을 때만 시간 흐름
        if (!gameManager.bugButton.gameObject.activeSelf && !gameManager.IsOverclockActive)
        {
            bugSpawnTimer += Time.deltaTime;

            if (bugSpawnTimer >= nextBugTime)
            {
                SpawnBug(); // 버그 소환!
                bugSpawnTimer = 0f;
                // 다음 버그는 15초 ~ 40초 사이 랜덤한 시간에 등장
                nextBugTime = UnityEngine.Random.Range(15f, 40f);
            }
        }
    }

    void SpawnBug()
    {
        if (gameManager == null || gameManager.bugButton == null) return;

        // 1. 버튼 활성화
        gameManager.bugButton.gameObject.SetActive(true);

        // 2. 랜덤 위치 계산 (화면 안쪽 어딘가)
        float randX = UnityEngine.Random.Range(-400f, 400f);
        float randY = UnityEngine.Random.Range(-800f, 800f);

        // 3. 위치 이동
        gameManager.bugButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(randX, randY);

        string alertStr = LocalizationManager.Instance != null ? LocalizationManager.Instance.Get("beast_detected") : "[ALERT] Magical Beast detected! Purge it!";
        gameManager.terminalManager?.AddLog($"<color=red><b>{alertStr}</b></color>");

        // 5. 3초 뒤에 도망가는 로직 시작
        bugEscapeRoutine = StartCoroutine(BugEscapeRoutine());
    }

    public void OnClickBug()
    {
        if (gameManager == null || gameManager.bugButton == null) return;

        if (gameManager.bugButton.gameObject.activeSelf)
        {
            // 1. 보상 계산 (현재 초당 생산량의 10배 + 보너스)
            long reward = gameManager.GetTotalRevenue() * 10;
            if (reward == 0) reward = 1000; // 초반이라 생산량 0이면 기본값

            // 황금 디스크 보너스도 적용
            float prestigeMultiplier = 1f + (gameManager.goldenDisks * 0.1f);
            reward = (long)(reward * prestigeMultiplier);

            gameManager.logs += reward;
            gameManager.ReportBeastPurge();
            gameManager.UpdateUI();

            string fixedStr = LocalizationManager.Instance != null ? LocalizationManager.Instance.Get("beast_fixed") : "BEAST PURGED! Reward: +";
            gameManager.terminalManager?.AddLog($"<color=green><b>{fixedStr}{gameManager.FormatNumber(reward)}</b></color>");

            // 3. 버그 숨기기
            if (bugEscapeRoutine != null)
            {
                StopCoroutine(bugEscapeRoutine);
            }

            gameManager.bugButton.gameObject.SetActive(false);
        }
    }

    IEnumerator BugEscapeRoutine()
    {
        yield return new WaitForSeconds(3f); // 3초 대기

        if (gameManager != null && gameManager.bugButton != null && gameManager.bugButton.gameObject.activeSelf)
        {
            string escapeStr = LocalizationManager.Instance != null ? LocalizationManager.Instance.Get("beast_escaped") : "Beast escaped into the gate...";
            gameManager.terminalManager?.AddLog($"<color=grey>{escapeStr}</color>");
        }
    }
}
