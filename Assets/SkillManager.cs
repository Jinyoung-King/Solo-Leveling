using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SkillManager : MonoBehaviour
{
    private GameManager gameManager;

    public bool IsCoffeeTime { get; private set; } = false;

    private float skillDuration = 10f; // 지속 시간
    private float skillCooldown = 30f; // 쿨타임

    public void Initialize(GameManager gm)
    {
        gameManager = gm;
        if (gameManager.coffeeSlider != null)
        {
            gameManager.coffeeSlider.minValue = 0f;
            gameManager.coffeeSlider.maxValue = 1f;
            gameManager.coffeeSlider.value = 1f;
        }
    }

    public void ActivateCoffee()
    {
        if (gameManager == null) return;
        if (!IsCoffeeTime && gameManager.coffeeBtn != null && gameManager.coffeeBtn.interactable)
        {
            StartCoroutine(CoffeeRoutine());
        }
    }

    IEnumerator CoffeeRoutine()
    {
        if (gameManager == null) yield break;

        // --- 1. 버프 시작 ---
        IsCoffeeTime = true;
        if (gameManager.coffeeBtn != null) gameManager.coffeeBtn.interactable = false; // 버튼 잠금
        gameManager.terminalManager?.AddLog("<color=yellow><b>[BOOST] COFFEE TIME! Speed x2</b></color>");

        // 지속 시간 동안 게이지 줄이기 (1 -> 0)
        float timer = 0f;
        while (timer < skillDuration)
        {
            timer += Time.deltaTime;
            if (gameManager.coffeeSlider != null)
            {
                gameManager.coffeeSlider.value = 1f - (timer / skillDuration);
            }
            yield return null;
        }

        // --- 2. 버프 종료 ---
        IsCoffeeTime = false;
        gameManager.terminalManager?.AddLog("[INFO] Caffeine worn off...");
        gameManager.UpdateUI(); // 색깔 원상복구

        // --- 3. 쿨타임 시작 ---
        // 게이지가 다시 차오름 (0 -> 1)
        timer = 0f;
        while (timer < skillCooldown)
        {
            timer += Time.deltaTime;
            if (gameManager.coffeeSlider != null)
            {
                gameManager.coffeeSlider.value = timer / skillCooldown;
            }
            yield return null;
        }

        // --- 4. 쿨타임 끝 (재사용 가능) ---
        if (gameManager.coffeeBtn != null) gameManager.coffeeBtn.interactable = true;
        if (gameManager.coffeeSlider != null) gameManager.coffeeSlider.value = 1f; // 꽉 참
        gameManager.terminalManager?.AddLog("<color=white>Coffee is ready!</color>");
    }
}
