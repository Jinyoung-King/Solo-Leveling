using UnityEngine;
using System.Collections;

public class OverclockManager : MonoBehaviour
{
    private GameManager gameManager;

    public bool IsOverclock { get; private set; } = false;
    public float HeatValue { get; private set; } = 0f;

    private float heatDecayRate = 10f; // 초당 10씩 식음
    private float heatGainPerClick = 5f; // 클릭당 5씩 오름

    public void Initialize(GameManager gm)
    {
        gameManager = gm;
        if (gameManager.heatSlider != null)
        {
            gameManager.heatSlider.minValue = 0f;
            gameManager.heatSlider.maxValue = 100f;
            gameManager.heatSlider.value = 0f;
        }
    }

    void Update()
    {
        if (gameManager == null) return;

        // 발열 게이지 관리 (오버클럭 중이 아닐 때만)
        if (!IsOverclock)
        {
            if (HeatValue > 0)
            {
                // 가만히 있으면 게이지가 줄어듦
                HeatValue -= heatDecayRate * Time.deltaTime;
                if (HeatValue < 0) HeatValue = 0f;
            }

            // UI 갱신
            if (gameManager.heatSlider != null) gameManager.heatSlider.value = HeatValue;
        }
    }

    public void AddHeat()
    {
        if (IsOverclock) return;

        HeatValue += heatGainPerClick;
        if (gameManager.heatSlider != null) gameManager.heatSlider.value = HeatValue;

        // 게이지 꽉 참 -> 오버클럭 발동!
        if (HeatValue >= 100f)
        {
            StartCoroutine(OverclockRoutine());
        }
    }

    IEnumerator OverclockRoutine()
    {
        if (gameManager == null) yield break;

        IsOverclock = true;
        HeatValue = 100f;
        if (gameManager.heatSlider != null) gameManager.heatSlider.value = 100f;

        // 1. 시각 효과 (배경을 빨갛게!)
        Color originalColor = Color.black;
        if (gameManager.backgroundPanel != null)
        {
            originalColor = gameManager.backgroundPanel.color;
            gameManager.backgroundPanel.color = new Color(0.5f, 0f, 0f, 0.5f); // 붉은 반투명
        }

        string warnStr = LocalizationManager.Instance != null ? LocalizationManager.Instance.Get("mana_overload") : " WARNING: MANA OVERLOAD! (x5 Boost) ";
        gameManager.terminalManager?.AddLog($"<color=red><b>{warnStr}</b></color>");

        // 2. 지속 시간 (10초 동안 게이지가 타들어감)
        float duration = 10f;
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            // 게이지가 100 -> 0 으로 줄어드는 연출
            if (gameManager.heatSlider != null)
            {
                gameManager.heatSlider.value = 100f - ((timer / duration) * 100f);
            }
            yield return null;
        }

        // 3. 종료
        IsOverclock = false;
        HeatValue = 0f;

        // 색깔 원상복구
        if (gameManager.backgroundPanel != null) gameManager.backgroundPanel.color = originalColor;

        string stableStr = LocalizationManager.Instance != null ? LocalizationManager.Instance.Get("mana_stabilized") : "Mana stabilized. Cooling down...";
        gameManager.terminalManager?.AddLog($"<color=green>{stableStr}</color>");
        gameManager.UpdateUI();
    }
}
