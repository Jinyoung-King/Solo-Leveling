using UnityEngine;
using TMPro;
using UnityEngine.UI; // [추가] Button 컴포넌트 사용을 위해 필요
using System.Collections.Generic;
using System;

public class TerminalManager : MonoBehaviour
{
    public TextMeshProUGUI terminalText;
    public TMP_InputField inputField; // [추가] 명령어 입력창
    public GameManager gameManager;   // [추가] 게임 매니저 참조

    private List<string> logLines = new List<string>();
    private int maxLines = 4; // 작은 메시지 박스: 최근 4줄만 표시

    // 작은 메시지 박스(토스트) 표현용
    private CanvasGroup messageBoxGroup;
    private float lastMessageTime;
    private const float MessageDisplayDuration = 5f; // 표시 후 서서히 사라지기까지의 시간(초)

    void Start()
    {
        if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>();
        }

        if (inputField == null)
        {
            inputField = GetComponentInChildren<TMP_InputField>();
        }

        // 콘솔 창을 작은 메시지 박스로 전환
        SetupMessageBox();

        AddLog("<color=#33FF88>게이트 접속 완료. 어서 오세요, 그림자 군주여!</color>");
    }

    // 기존의 큰 콘솔(입력창 + 로그 창)을 화면 하단의 작은 메시지 박스로 바꿉니다.
    private void SetupMessageBox()
    {
        // 1. 콘솔 입력창 / 전송 버튼 숨기기 (명령 입력 UI 제거)
        if (inputField != null) inputField.gameObject.SetActive(false);

        Button[] childButtons = GetComponentsInChildren<Button>(true);
        foreach (var btn in childButtons)
        {
            string n = btn.gameObject.name.ToLower();
            if (n.Contains("send") || n.Contains("submit"))
            {
                btn.gameObject.SetActive(false);
            }
        }

        // 2. 패널을 하단(탭 바 위)의 작은 박스로 재배치
        RectTransform rect = GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 140f); // 하단 탭 바(120px) 바로 위
            rect.sizeDelta = new Vector2(780f, 160f);
        }

        // 3. 배경: 반투명 박스 + 터치 통과(게임 조작 방해 방지)
        Image panelImg = GetComponent<Image>();
        if (panelImg != null)
        {
            panelImg.color = new Color(0.05f, 0.05f, 0.08f, 0.82f);
            panelImg.raycastTarget = false;
        }

        // 4. 텍스트: 최근 메시지만 하단 정렬로 표시, 터치 통과
        if (terminalText != null)
        {
            terminalText.raycastTarget = false;
            terminalText.fontSize = 22;
            terminalText.alignment = TextAlignmentOptions.BottomLeft;
            terminalText.overflowMode = TextOverflowModes.Truncate;

            RectTransform tr = terminalText.GetComponent<RectTransform>();
            if (tr != null)
            {
                tr.anchorMin = Vector2.zero;
                tr.anchorMax = Vector2.one;
                tr.offsetMin = new Vector2(20f, 12f);
                tr.offsetMax = new Vector2(-20f, -12f);
            }
        }

        // 5. 페이드아웃용 CanvasGroup
        messageBoxGroup = GetComponent<CanvasGroup>();
        if (messageBoxGroup == null) messageBoxGroup = gameObject.AddComponent<CanvasGroup>();
        messageBoxGroup.alpha = 0f;
        messageBoxGroup.interactable = false;
        messageBoxGroup.blocksRaycasts = false;
    }

    void Update()
    {
        // 마지막 메시지 후 일정 시간이 지나면 메시지 박스를 서서히 숨김
        if (messageBoxGroup != null && messageBoxGroup.alpha > 0f)
        {
            if (Time.time - lastMessageTime > MessageDisplayDuration)
            {
                messageBoxGroup.alpha = Mathf.MoveTowards(messageBoxGroup.alpha, 0f, Time.deltaTime * 1.5f);
            }
        }
    }

    public void AddLog(string message)
    {
        logLines.Add(message);

        // 너무 많으면 옛날 로그 삭제 (최근 몇 줄만 유지)
        if (logLines.Count > maxLines)
        {
            logLines.RemoveAt(0);
        }

        // 화면 갱신
        if (terminalText != null)
        {
            terminalText.text = string.Join("\n", logLines);
        }

        // 메시지 박스를 즉시 표시하고 페이드 타이머 리셋
        if (messageBoxGroup != null) messageBoxGroup.alpha = 1f;
        lastMessageTime = Time.time;
    }

    // [신규] 입력된 명령어를 처리하는 함수
    private void OnSubmitInput(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            // 빈 입력 처리
            if (inputField != null)
            {
                inputField.text = "";
                inputField.ActivateInputField();
            }
            return;
        }

        // 1. 에코 출력 (내가 친 명령어 터미널에 표시)
        AddLog($"<color=#ffffff>> {input}</color>");

        // 2. 명령어 파싱 (대소문자 무시, 공백 트림)
        string cleanedInput = input.Trim().ToLower();
        
        // 3. 인자값 분리 (ex: buy docker -> cmd: buy, arg: docker)
        string[] tokens = cleanedInput.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        string cmd = tokens[0];
        string arg = tokens.Length > 1 ? tokens[1] : "";

        ProcessCommand(cmd, arg, cleanedInput);

    // [4. 입력창 초기화 및 다시 포커스] 부분 수정 (모바일 대응)
        if (inputField != null)
        {
            inputField.text = "";
            
            // 모바일 플랫폼이 아닐 때(PC)만 엔터 후 입력창 포커스를 강제 유지
            // 모바일에서는 가상 키보드가 화면을 계속 가리지 않게 포커스를 풀어줍니다.
            if (!Application.isMobilePlatform)
            {
                inputField.ActivateInputField();
            }
            else
            {
                inputField.DeactivateInputField();
            }
        }
    }

    // [신규] UI Send(전송) 버튼을 클릭했을 때 호출할 수 있는 모바일용 함수
    public void OnClickSendButton()
    {
        if (inputField != null)
        {
            OnSubmitInput(inputField.text);
        }
    }

    private void ProcessCommand(string cmd, string arg, string fullInput)
    {
        if (gameManager == null) gameManager = FindObjectOfType<GameManager>();

        switch (cmd)
        {
            case "help":
                AddLog("<color=yellow>=== AVAILABLE COMMANDS ===</color>");
                AddLog("<color=cyan>help</color> - Show this help menu");
                AddLog("<color=cyan>clear</color> - Clear terminal screen");
                AddLog("<color=cyan>top</color> / <color=cyan>status</color> - Display monarch status & mana stats");
                AddLog("<color=cyan>ps</color> / <color=cyan>ls</color> - List active shadow army (units)");
                AddLog("<color=cyan>ping</color> - Probe gate connection latency");
                AddLog("<color=cyan>upgrade</color> - Upgrade Monarch Authority (same as Monarch Upgrade)");
                AddLog("<color=cyan>coffee</color> - Drink caffeine potion booster (if available)");
                AddLog("<color=cyan>bugfix</color> / <color=cyan>kill -9 bug</color> - Purge active magical beast event");
                AddLog("<color=cyan>buy [name]</color> - Summon a shadow soldier (e.g. 'buy knight', 'buy infantry')");
                AddLog("<color=cyan>gacha</color> / <color=cyan>draw</color> - Draw random hunter equipment (Costs 50.0K+ Mana)");
                break;

            case "clear":
                logLines.Clear();
                terminalText.text = "";
                break;

            case "top":
            case "status":
                if (gameManager != null)
                {
                    long currentLPS = gameManager.GetTotalRevenue();
                    
                    AddLog("<color=#00FF00>=== MONARCH STATUS ===</color>");
                    AddLog($"Monarch Level: <color=cyan>Lv.{gameManager.serverLevel}</color>");
                    AddLog($"Current Mana: <color=yellow>{gameManager.logs}</color>");
                    AddLog($"MPS (Mana/sec): <color=green>{currentLPS}/sec</color>");
                    AddLog($"Golden Disks: <color=yellow>{gameManager.goldenDisks}</color>");
                    AddLog("Type <color=cyan>'ps'</color> to check shadow army details.");
                }
                break;

            case "ps":
            case "ls":
                if (gameManager != null && gameManager.units != null)
                {
                    AddLog("<color=yellow>=== ACTIVE SHADOW ARMY ===</color>");
                    int activeCount = 0;
                    for (int i = 0; i < gameManager.units.Count; i++)
                    {
                        var u = gameManager.units[i];
                        if (u.count > 0)
                        {
                            AddLog($"[{i}] <color=cyan>{u.unitName}</color> (Lv.{u.count}) - Yield: {u.revenuePerSec * u.count}/sec");
                            activeCount++;
                        }
                    }
                    if (activeCount == 0)
                    {
                        AddLog("<color=red>No active shadow soldiers. Summon shadows to start harvesting mana.</color>");
                    }
                }
                break;

            case "ping":
                int latency = UnityEngine.Random.Range(5, 120);
                AddLog($"64 bytes from gate_portal: icmp_seq=1 power={latency} mana");
                if (gameManager != null && UnityEngine.Random.Range(0, 10) < 2)
                {
                    long bonus = (long)(gameManager.serverLevel * 5);
                    gameManager.logs += bonus;
                    AddLog($"<color=green>[BONUS] Mana rift harvested: +{bonus} Mana</color>");
                    gameManager.UpdateUI();
                }
                break;

            case "upgrade":
                if (gameManager != null)
                {
                    if (gameManager.logs >= gameManager.upgradeCost)
                    {
                        gameManager.UpgradeServer();
                    }
                    else
                    {
                        AddLog("<color=red>[ERROR] Insufficient Mana to upgrade Monarch Authority.</color>");
                    }
                }
                break;

            case "coffee":
                if (gameManager != null)
                {
                    if (gameManager.coffeeBtn != null && gameManager.coffeeBtn.interactable)
                    {
                        gameManager.ActivateCoffee();
                    }
                    else
                    {
                        AddLog("<color=red>[ERROR] Potion is still brewing (on cooldown).</color>");
                    }
                }
                break;

            case "bugfix":
                FixBugCommand();
                break;

            case "gacha":
            case "draw":
                if (gameManager != null)
                {
                    gameManager.DrawEquipmentGacha();
                }
                break;

            case "kill":
                if (arg == "-9" && fullInput.Contains("bug"))
                {
                    FixBugCommand();
                }
                else
                {
                    AddLog("Usage: kill -9 bug");
                }
                break;

            case "buy":
                if (string.IsNullOrEmpty(arg))
                {
                    AddLog("Usage: buy [shadow_name] (e.g. 'buy knight', 'buy infantry')");
                    break;
                }
                BuyTechByName(arg);
                break;

            default:
                // kill -9 bug가 cmd가 "kill"이 아닐 때 fullInput 매칭을 위해 예외 처리
                if (fullInput.Replace(" ", "") == "kill-9bug")
                {
                    FixBugCommand();
                }
                else
                {
                    AddLog($"<color=red>Command not found: '{cmd}'</color>. Type 'help' for info.");
                }
                break;
        }
    }

    private void FixBugCommand()
    {
        if (gameManager != null && gameManager.bugButton != null)
        {
            if (gameManager.bugButton.gameObject.activeSelf)
            {
                gameManager.OnClickBug();
            }
            else
            {
                AddLog("<color=grey>No active magical beasts detected in the gate.</color>");
            }
        }
    }

    private void BuyTechByName(string namePart)
    {
        if (gameManager == null || gameManager.units == null) return;

        int foundIndex = -1;
        for (int i = 0; i < gameManager.units.Count; i++)
        {
            // 기술 이름에 검색어 포함 여부 확인
            if (gameManager.units[i].unitName.ToLower().Replace(" ", "").Contains(namePart))
            {
                foundIndex = i;
                break;
            }
        }

        if (foundIndex != -1)
        {
            var u = gameManager.units[foundIndex];
            if (gameManager.logs >= u.currentCost)
            {
                gameManager.BuyUnit(foundIndex);
            }
            else
            {
                AddLog($"<color=red>[ERROR] Insufficient mana to summon {u.unitName}. Cost: {u.currentCost}</color>");
            }
        }
        else
        {
            AddLog($"<color=red>[ERROR] Shadow type matching '{namePart}' not found.</color>");
        }
    }
}