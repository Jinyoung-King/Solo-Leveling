using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System;

public class TerminalManager : MonoBehaviour
{
    public TextMeshProUGUI terminalText;
    public TMP_InputField inputField; // [추가] 명령어 입력창
    public GameManager gameManager;   // [추가] 게임 매니저 참조

    private List<string> logLines = new List<string>();
    private int maxLines = 28; // 최대 28줄까지만 표시

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

        if (inputField != null)
        {
            // 엔터키를 눌렀을 때 실행될 리스너 추가
            inputField.onSubmit.AddListener(OnSubmitInput);
            
            // 시작할 때 입력창 활성화
            inputField.ActivateInputField();
        }

        // [추가] Send 버튼 자동 검색 및 이벤트 바인딩
        Button[] childButtons = GetComponentsInChildren<Button>(true);
        foreach (var btn in childButtons)
        {
            if (btn.gameObject.name.Contains("Send") || btn.gameObject.name.Contains("Submit"))
            {
                btn.onClick.AddListener(OnClickSendButton);
                break;
            }
        }

        AddLog("<color=green>System initialized. Welcome to Solo Leveling Terminal!</color>");
        AddLog("Type <color=yellow>'help'</color> to see available commands.");
    }

    void Update()
    {
        // 마우스 클릭 등으로 입력창 포커스가 풀렸을 때, 
        // 키보드 입력을 계속 터미널에 할 수 있도록 백그라운드 클릭 시 복구하는 처리
        // (필요 시 포커스 강제 유지 또는 `~`나 `Enter` 등으로 포커스 주는 연출 가능)
        if (inputField != null && !inputField.isFocused && Input.GetKeyDown(KeyCode.Return))
        {
            inputField.ActivateInputField();
        }
    }

    public void AddLog(string message)
    {
        // 현재 시간 구하기
        string time = System.DateTime.Now.ToString("HH:mm:ss");
        string formattedLog = $"<color=#888888>[{time}]</color> {message}";

        logLines.Add(formattedLog);

        // 너무 많으면 옛날 로그 삭제 (메모리 관리)
        if (logLines.Count > maxLines)
        {
            logLines.RemoveAt(0);
        }

        // 화면 갱신
        terminalText.text = string.Join("\n", logLines);
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
                AddLog("<color=cyan>top</color> / <color=cyan>status</color> - Display server load & performance stats");
                AddLog("<color=cyan>ps</color> / <color=cyan>ls</color> - List active tech stacks (units)");
                AddLog("<color=cyan>ping</color> - Send a packet to test connection latency");
                AddLog("<color=cyan>upgrade</color> - Upgrade server CPU (same as clicking CPU Upgrade)");
                AddLog("<color=cyan>coffee</color> - Activate coffee booster (if available)");
                AddLog("<color=cyan>bugfix</color> / <color=cyan>kill -9 bug</color> - Debug and fix active bug event");
                AddLog("<color=cyan>buy [name]</color> - Buy a tech stack (e.g. 'buy docker', 'buy aws')");
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
                    float totalMultiplier = 1f;
                    
                    AddLog("<color=#00FF00>=== SYSTEM STATUS ===</color>");
                    AddLog($"Server Level: <color=cyan>Lv.{gameManager.serverLevel}</color>");
                    AddLog($"Current Logs: <color=yellow>{gameManager.logs}</color>");
                    AddLog($"LPS (Logs/sec): <color=green>{currentLPS}/sec</color>");
                    AddLog($"Golden Disks: <color=yellow>{gameManager.goldenDisks}</color>");
                    AddLog("Type <color=cyan>'ps'</color> to check tech stack details.");
                }
                break;

            case "ps":
            case "ls":
                if (gameManager != null && gameManager.units != null)
                {
                    AddLog("<color=yellow>=== ACTIVE PROCESSES (TECH STACK) ===</color>");
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
                        AddLog("<color=red>No active processes. Purchase tech stacks to start logging.</color>");
                    }
                }
                break;

            case "ping":
                // 핑 명령어로 깨알 보너스 획득 기믹
                int latency = UnityEngine.Random.Range(5, 120);
                AddLog($"64 bytes from 127.0.0.1: icmp_seq=1 ttl=64 time={latency}ms");
                if (gameManager != null && UnityEngine.Random.Range(0, 10) < 2) // 20% 확률 보너스
                {
                    long bonus = (long)(gameManager.serverLevel * 5);
                    gameManager.logs += bonus;
                    AddLog($"<color=green>[BONUS] Packet payload hijacked: +{bonus} Logs</color>");
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
                        AddLog("<color=red>[ERROR] Insufficient Logs to upgrade CPU.</color>");
                    }
                }
                break;

            case "coffee":
                if (gameManager != null)
                {
                    // GameManager.cs 의 coffeeBtn.interactable 이 참인지 확인
                    if (gameManager.coffeeBtn != null && gameManager.coffeeBtn.interactable)
                    {
                        gameManager.ActivateCoffee();
                    }
                    else
                    {
                        AddLog("<color=red>[ERROR] Coffee is still brewing (on cooldown).</color>");
                    }
                }
                break;

            case "bugfix":
                FixBugCommand();
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
                    AddLog("Usage: buy [tech_name_part] (e.g. 'buy docker', 'buy aws')");
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
                AddLog("<color=grey>No active bugs detected in the pipeline.</color>");
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
                AddLog($"<color=red>[ERROR] Insufficient logs to deploy {u.unitName}. Cost: {u.currentCost}</color>");
            }
        }
        else
        {
            AddLog($"<color=red>[ERROR] Tech stack matching '{namePart}' not found.</color>");
        }
    }
}