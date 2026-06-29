using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using System.Collections;

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

[System.Serializable]
public class Unit
{
    public string unitName;
    public long revenuePerSec;
    public long baseCost;
    public long currentCost;
    public int count = 0;
    // [추가] 각 유닛의 아이콘을 담을 변수
    public Sprite iconSprite;

    public Unit(string name, int tier)
    {
        unitName = name;
        baseCost = (long)(10 * Mathf.Pow(10, tier));
        revenuePerSec = (long)(1 * Mathf.Pow(5, tier));
        currentCost = baseCost;
    }

    // [추가] 아이콘을 설정하는 메서드
    public void SetIcon(Sprite sprite)
    {
        this.iconSprite = sprite;
    }

    public void Buy()
    {
        count++;
        currentCost = (long)(currentCost * 1.5f);
    }

    // 저장된 개수 불러올 때 비용 재계산
    public void LoadCount(int savedCount)
    {
        count = savedCount;
        currentCost = (long)(baseCost * Mathf.Pow(1.5f, savedCount));
    }
}

public class GameManager : MonoBehaviour
{
    [Header("UI System")]
    public Transform shopContent;
    public GameObject shopButtonPrefab;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI myInfoText; // [신규] 정보창 텍스트 연결용

    [Header("Popup UI")]
    public GameObject rewardPopup;
    public TextMeshProUGUI rewardMessage;

    [Header("Server Upgrade")] // [신규] 서버 레벨업 관련 UI
    public TextMeshProUGUI upgradeText; // 업그레이드 버튼의 텍스트
    public Button upgradeButton;        // 업그레이드 버튼

    [Header("Game Data")]
    public long logs = 0;

    // [신규] 클릭 레벨 시스템
    public int serverLevel = 1;
    public long upgradeCost = 100; // 첫 업그레이드 비용

    public List<Unit> units = new List<Unit>();

    // [수정 1] 누락되었던 리스트 변수 선언 추가! (이게 없어서 오류남)
    private List<TextMeshProUGUI> generatedButtonTexts = new List<TextMeshProUGUI>();
    // [추가] 터미널 매니저를 연결할 변수
    public TerminalManager terminalManager;

    // [추가] 아까 Project 창에 넣은 아이콘 파일들을 여기에 연결할 겁니다.
    [Header("Resources")]
    public Sprite[] techIcons;

    [Header("Juice Effect")]
    public ObjectShaker buttonShaker; // [추가] 쉐이커 연결용

    string[] techNames = {
        "Shell Script", "Docker", "Kubernetes", "AWS EC2", "Serverless Lambda",
        "Kafka Cluster", "Elasticsearch", "Quantum Server", "AI DevOps Bot", "Dyson Sphere"
    };

    private float timer = 0f;

    [Header("Animation")]
    public Transform serverButtonTr;

    [Header("Effect")]
    public GameObject effectPrefab;
    public Transform effectParent;

    [Header("Skill System")]
    public Button coffeeBtn;        // 커피 버튼
    public Slider coffeeSlider;     // 쿨타임 게이지
    public TextMeshProUGUI boostText; // "x2 BOOST!" 텍스트 (없으면 생략 가능)

    private bool isCoffeeTime = false; // 현재 버프 중인가?
    private float skillDuration = 10f; // 지속 시간
    private float skillCooldown = 30f; // 쿨타임

    [Header("Overclock System")]
    public Slider heatSlider;       // 발열 게이지
    public Image backgroundPanel;   // 오버클럭 때 색깔 바꿀 배경 (Panel_Terminal의 Image 등)

    private bool isOverclock = false;
    private float heatValue = 0f;
    private float heatDecayRate = 10f; // 초당 10씩 식음 (빨리 식어야 긴장됨)
    private float heatGainPerClick = 5f; // 클릭당 5씩 오름 (20번 광클하면 발동)

    // [신규] 바운스 애니메이션을 기억할 변수
    private Coroutine bounceRoutine;

    [Header("Prestige System")]
    public GameObject migrationPanel;         // 아까 만든 팝업창
    public TextMeshProUGUI migrationInfoText; // 설명 텍스트 (Txt_Migrate_Info)
    public int goldenDisks = 0; // [핵심] 황금 디스크 개수 (초기화해도 안 사라짐)

    [Header("Bug Event")]
    public Button bugButton;      // 벌레 버튼 (Btn_Bug)
    public RectTransform canvasRect; // 화면 크기 알기 위해 필요 (Canvas)

    private float bugSpawnTimer = 0f;
    private float nextBugTime = 10f; // 첫 버그는 10초 뒤 출현
    // [신규] 버그 도망가는 타이머를 기억할 변수
    private Coroutine bugEscapeRoutine;
    
    const string KEY_GOLDEN_DISK = "GoldenDisk"; // 저장용 키

    const string KEY_LOGS = "SavedLogs";
    const string KEY_TIME = "LastSaveTime";
    // [신규] 레벨 저장용 키
    const string KEY_LEVEL = "ServerLevel";
    const string KEY_UPGRADE_COST = "UpgradeCost";

    void Start()
    {
        // [추가] 모바일 플랫폼 프레임 레이트 상향 조정 (기본 30 FPS -> 60 FPS)
        // 화면 스크롤(슬라이드) 시 끊기는 렉 현상을 완전히 제거합니다.
        Application.targetFrameRate = 60;

        // [추가] 장비 시스템 데이터 생성
        InitializeEquipments();

        // [신규] 슬라이더 설정 강제 초기화 (이게 없어서 안 움직였을 수 있음!)
        if (coffeeSlider != null)
        {
            coffeeSlider.minValue = 0f;
            coffeeSlider.maxValue = 1f;
            coffeeSlider.value = 1f; // 처음엔 꽉 찬 상태
        }
        // 1. 데이터 생성
        GenerateTechStack();

        // [수정 2] 저장된 데이터 불러오기 (여기가 빠져 있었습니다!)
        LoadGame();

        // 3. UI 갱신
        UpdateUI();
    }

    void GenerateTechStack()
    {
        for (int i = 0; i < techNames.Length; i++)
        {
            Unit newUnit = new Unit(techNames[i], i);
            // [추가] 아이콘 연결 (배열 범위 체크 포함)
            if (i < techIcons.Length)
            {
                newUnit.SetIcon(techIcons[i]);
            }
            units.Add(newUnit);

            GameObject btnObj = Instantiate(shopButtonPrefab, shopContent);
            
            // 안전한 아이콘 이미지 컴포넌트 탐색
            Image iconImage = null;
            Image[] allImages = btnObj.GetComponentsInChildren<Image>(true);
            Button btnComponent = btnObj.GetComponent<Button>();
            foreach (var img in allImages)
            {
                if (btnComponent != null && img == btnComponent.targetGraphic)
                {
                    continue; // 버튼 배경용 이미지는 스킵
                }
                iconImage = img; // 실제 자식 아이콘 이미지 바인딩
                break;
            }

            // 아이콘 적용
            if (iconImage != null && newUnit.iconSprite != null)
            {
                iconImage.sprite = newUnit.iconSprite;
            }

            TextMeshProUGUI txt = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            generatedButtonTexts.Add(txt);

            int index = i;
            btnObj.GetComponent<Button>().onClick.AddListener(() => BuyUnit(index));
        }
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= 1f)
        {
            long totalRevenue = GetTotalRevenue();
            logs += totalRevenue;
            timer -= 1f;
            UpdateUI();
        }

        if (Input.GetKeyDown(KeyCode.Delete)) { DeleteAllData(); }

        // [신규] 발열 게이지 관리 (오버클럭 중이 아닐 때만)
        if (!isOverclock)
        {
            if (heatValue > 0)
            {
                // 가만히 있으면 게이지가 줄어듦
                heatValue -= heatDecayRate * Time.deltaTime;
                if (heatValue < 0) heatValue = 0;
            }

            // UI 갱신
            if (heatSlider != null) heatSlider.value = heatValue;
        }

        // [신규] 버그 출몰 타이머
        // 오버클럭 중이 아니고, 버그가 안 떠있을 때만 시간 흐름
        if (!bugButton.gameObject.activeSelf)
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

    // [신규] 버그를 화면 어딘가에 나타나게 하는 함수
    void SpawnBug()
    {
        // 1. 버튼 활성화
        bugButton.gameObject.SetActive(true);

        // 2. 랜덤 위치 계산 (화면 안쪽 어딘가)
        // Canvas의 크기를 기준으로 랜덤 좌표를 잡습니다.
        // (x: -400 ~ 400, y: -800 ~ 800 대충 이정도 범위)
        float randX = UnityEngine.Random.Range(-400f, 400f);
        float randY = UnityEngine.Random.Range(-800f, 800f); // 상하 범위는 UI 가리지 않게 조절

        // 3. 위치 이동
        bugButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(randX, randY);

        // 4. 로그 출력
        terminalManager?.AddLog("<color=red><b>[ALERT] Bug detected! Catch it!</b></color>");

        // 5. 3초 뒤에 도망가는 로직 시작
        // [수정] 코루틴 시작하면서 변수에 저장해두기
        bugEscapeRoutine = StartCoroutine(BugEscapeRoutine());
    }

    // [신규] 버그 잡기 (버튼 클릭 시 실행)
    public void OnClickBug()
    {
        if (bugButton.gameObject.activeSelf)
        {
            // 1. 보상 계산 (현재 초당 생산량의 10배 + 보너스)
            long reward = GetTotalRevenue() * 10;
            if (reward == 0) reward = 1000; // 초반이라 생산량 0이면 기본값

            // 황금 디스크 보너스도 적용
            float prestigeMultiplier = 1f + (goldenDisks * 0.1f);
            reward = (long)(reward * prestigeMultiplier);

            logs += reward;
            UpdateUI();

            // 2. 연출
            terminalManager?.AddLog($"<color=green><b>BUG FIXED! Reward: +{FormatNumber(reward)}</b></color>");
            
            // 3. 버그 숨기기
            // 오버클럭 타이머는 건드리지 않고, 버그 도망가는 것만 취소함
            if (bugEscapeRoutine != null) 
            {
                StopCoroutine(bugEscapeRoutine);
            }

            bugButton.gameObject.SetActive(false);
        }
    }

    // [신규] 3초 뒤에 도망가는 타이머
    IEnumerator BugEscapeRoutine()
    {
        yield return new WaitForSeconds(3f); // 3초 대기

        if (bugButton.gameObject.activeSelf)
        {
            bugButton.gameObject.SetActive(false); // 도망감
            terminalManager?.AddLog("<color=grey>Bug escaped into the server...</color>");
        }
    }

    void DeleteAllData()
    {
        PlayerPrefs.DeleteAll();
        logs = 0;
        serverLevel = 1;         // 초기화
        upgradeCost = 100;       // 초기화
        foreach (var u in units) { u.count = 0; u.LoadCount(0); }
        UpdateUI();
        terminalManager?.AddLog("[INFO] <color=red><b>SYSTEM RESET COMPLETED.</b></color>");
    }

    public long GetTotalRevenue()
    {
        long total = 0;
        foreach (Unit unit in units) total += (unit.count * unit.revenuePerSec);

        // [추가] 장비 LPS 보너스 합산
        total += GetEquipmentLPS();

        if (isCoffeeTime) total *= 2;

        // [추가] 오버클럭이면 5배! (커피랑 중첩되면 10배!)
        if (isOverclock) total *= 5;

        // 디스크 1개당 10% 추가 (1개 = 1.1배) & 장비 배율 보너스 중첩 적용
        float prestigeMultiplier = 1f + (goldenDisks * 0.1f);
        float equipMultiplier = GetEquipmentMultiplier();
        total = (long)(total * prestigeMultiplier * equipMultiplier);

        return total;
    }

    public void BuyUnit(int index)
    {
        Unit target = units[index];
        if (logs >= target.currentCost)
        {
            logs -= target.currentCost;
            target.Buy();
            UpdateUI();

            // [수정 3] 물건 살 때마다 자동 저장
            SaveGame();

            // [추가] 구매 로그 출력 (글자색 강조)
            // <b>는 Bold(굵게), <color>는 색상 변경
            terminalManager?.AddLog($"<color=yellow><b>Deployed: {target.unitName}</b></color>");
        }
    }

    // [신규] 서버 업그레이드 (CPU 증설)
    public void UpgradeServer()
    {
        if (logs >= upgradeCost)
        {
            logs -= upgradeCost;
            serverLevel++; // 레벨업

            // 비용 증가 (2.5배씩 비싸짐)
            upgradeCost = (long)(upgradeCost * 2.5f);

            UpdateUI();
            SaveGame();

            // 화려한 로그 출력
            terminalManager?.AddLog($"<color=blue><b>SERVER UPGRADE! Lv.{serverLevel}</b></color>");
        }
    }

    public void OnClickServer()
    {
        long clickProfit = serverLevel;

        // [추가] 장비 클릭 보너스 합산
        clickProfit += GetEquipmentClick();

        // 기존 코드 밑에 추가:
        float prestigeMultiplier = 1f + (goldenDisks * 0.1f);
        float equipMultiplier = GetEquipmentMultiplier();
        clickProfit = (long)(clickProfit * prestigeMultiplier * equipMultiplier);

        // [추가] 오버클럭이면 클릭 효율도 5배
        if (isOverclock) clickProfit *= 5;

        bool isCritical = (UnityEngine.Random.Range(0, 100) < 10);
        if (isCritical)
        {
            // --- [핵심 수정] 복잡한 크리티컬 계산식 ---

            // 1. 배율 보너스: 기본 50배 + (서버 레벨 * 2)
            // 예: Lv.1 -> 52배, Lv.10 -> 70배
            long multiplier = 50 + (serverLevel * 2);

            // 2. 지식 보너스: 내가 가진 유닛 총 개수만큼 추가 데미지
            long knowledgeBonus = 0;
            foreach (var u in units) knowledgeBonus += u.count;
            knowledgeBonus *= 10; // 유닛 하나당 +10 데미지

            // 3. 최종 계산
            clickProfit = (clickProfit * multiplier) + knowledgeBonus;

            // 4. 카오스(랜덤) 변수: 80% ~ 120% 사이로 숫자가 튐
            float chaosFactor = UnityEngine.Random.Range(0.8f, 1.2f);
            clickProfit = (long)(clickProfit * chaosFactor);

            // ----------------------------------------
            if (buttonShaker != null) buttonShaker.Shake();
            terminalManager?.AddLog("<color=red>[FATAL] CRITICAL PACKET!</color>");
        }
        else
        {
            // 일반 클릭 로그는 너무 많이 쌓이면 지저분하니 10% 확률로만 출력하거나 생략 가능
            if (UnityEngine.Random.Range(0, 10) < 2) terminalManager?.AddLog("<color=yellow>Packet received.</color>");
        }

        // [추가] 클릭할 때마다 열기 축적 (오버클럭 중 아닐 때만)
        if (!isOverclock)
        {
            heatValue += heatGainPerClick;

            // 게이지 꽉 참 -> 오버클럭 발동!
            if (heatValue >= 100f)
            {
                StartCoroutine(OverclockRoutine());
            }
        }

        logs += clickProfit;
        UpdateUI();
        // 1. 만약 이전에 돌고 있던 바운스 애니메이션이 있다면, 그것만 멈춤
        if (bounceRoutine != null)
        {
            StopCoroutine(bounceRoutine);
        }

        // 2. 새로운 애니메이션을 시작하고, 변수에 저장해둠 (다음에 끄기 위해)
        bounceRoutine = StartCoroutine(BounceAnimation());
        ShowClickEffect(clickProfit, isCritical);
    }

    // [신규] 오버클럭 모드 코루틴
    IEnumerator OverclockRoutine()
    {
        isOverclock = true;
        heatValue = 100f;
        if (heatSlider != null) heatSlider.value = 100f;

        // 1. 시각 효과 (배경을 빨갛게!)
        Color originalColor = Color.black; // 혹은 원래 배경색
        if (backgroundPanel != null)
        {
            originalColor = backgroundPanel.color;
            backgroundPanel.color = new Color(0.5f, 0f, 0f, 0.5f); // 붉은 반투명
        }

        terminalManager?.AddLog("<color=red><b> WARNING: SERVER OVERHEAT! (x5 Boost) </b></color>");

        // 2. 지속 시간 (10초 동안 게이지가 타들어감)
        float duration = 10f;
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            // 게이지가 100 -> 0 으로 줄어드는 연출
            if (heatSlider != null) heatSlider.value = 100f - ((timer / duration) * 100f);
            yield return null;
        }

        // 3. 종료
        isOverclock = false;
        heatValue = 0f;

        // 색깔 원상복구
        if (backgroundPanel != null) backgroundPanel.color = originalColor;

        terminalManager?.AddLog("<color=green>System stabilized. Cooling down...</color>");
        UpdateUI();
    }

    // ---------------------------------------------------------
    // 📊 숫자 포맷팅 (K, M, G, T)
    // ---------------------------------------------------------
    string FormatNumber(long num)
    {
        if (num >= 1000000000000000) return (num / 1000000000000000f).ToString("F2") + "P"; // Peta
        if (num >= 1000000000000) return (num / 1000000000000f).ToString("F2") + "T";    // Tera
        if (num >= 1000000000) return (num / 1000000000f).ToString("F2") + "G";       // Giga
        if (num >= 1000000) return (num / 1000000f).ToString("F2") + "M";          // Mega
        if (num >= 1000) return (num / 1000f).ToString("F2") + "K";             // Kilo

        return num.ToString("N0"); // 1,000 미만은 그냥 콤마 찍어서 출력
    }

    // ---------------------------------------------------------
    // 💾 저장 및 불러오기 시스템 (Save & Load)
    // ---------------------------------------------------------

    private void OnApplicationQuit() { SaveGame(); }
    private void OnApplicationPause(bool pause) { if (pause) SaveGame(); }

    void SaveGame()
    {
        PlayerPrefs.SetString(KEY_LOGS, logs.ToString());
        PlayerPrefs.SetInt(KEY_LEVEL, serverLevel);              // [신규]
        PlayerPrefs.SetString(KEY_UPGRADE_COST, upgradeCost.ToString()); // [신규]
        PlayerPrefs.SetInt(KEY_GOLDEN_DISK, goldenDisks); // 추가
        for (int i = 0; i < units.Count; i++) // Length -> Count로 수정
        {
            PlayerPrefs.SetInt("Unit_" + i, units[i].count);
        }
        // 장비 레벨 저장
        if (equipments != null)
        {
            for (int i = 0; i < equipments.Count; i++)
            {
                PlayerPrefs.SetInt("Equip_" + i, equipments[i].level);
            }
        }
        PlayerPrefs.SetString("GachaCost", gachaCost.ToString());

        PlayerPrefs.SetString(KEY_TIME, DateTime.Now.ToBinary().ToString());
        PlayerPrefs.Save();
    }

    void LoadGame()
    {
        if (!PlayerPrefs.HasKey(KEY_LOGS)) return;

        string logsStr = PlayerPrefs.GetString(KEY_LOGS, "0");
        long.TryParse(logsStr, out logs); // Parse보다 TryParse가 안전함

        // [신규] 레벨 불러오기
        serverLevel = PlayerPrefs.GetInt(KEY_LEVEL, 1);
        long.TryParse(PlayerPrefs.GetString(KEY_UPGRADE_COST, "100"), out upgradeCost);

        goldenDisks = PlayerPrefs.GetInt(KEY_GOLDEN_DISK, 0); // 추가

        // 유닛 개수 복구
        for (int i = 0; i < units.Count; i++)
        {
            int savedCount = PlayerPrefs.GetInt("Unit_" + i, 0);
            units[i].LoadCount(savedCount);
        }

        // 장비 레벨 복구
        if (equipments != null)
        {
            for (int i = 0; i < equipments.Count; i++)
            {
                equipments[i].level = PlayerPrefs.GetInt("Equip_" + i, 0);
            }
        }
        long.TryParse(PlayerPrefs.GetString("GachaCost", "50000"), out gachaCost);

        // 부재중 보상 계산 (Convert.ToInt64 예외 및 데이터 꼬임 방어 가드)
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
                    long revenuePerSec = GetTotalRevenue();
                    long offlineReward = (long)(secondsPassed * revenuePerSec);

                    if (offlineReward > 0)
                    {
                        logs += offlineReward;
                        Debug.Log($"[배치] {secondsPassed:F0}초 경과, {offlineReward} 획득");
                        ShowRewardPopup(secondsPassed, offlineReward);
                    }
                }
            }
        }
    }

    void ShowRewardPopup(double seconds, long reward)
    {
        if (rewardPopup != null && rewardMessage != null)
        {
            // [수정] 포맷팅 적용
            rewardMessage.text = $"[배치 작업 리포트]\n\n지난 {seconds:F0}초 동안\n서버가 열심히 돌아서\n\n<color=yellow>+{FormatNumber(reward)} Logs</color>\n를 수집했습니다!";
            rewardPopup.SetActive(true);
            rewardPopup.transform.SetAsLastSibling();
        }
    }

    public void CloseRewardPopup()
    {
        if (rewardPopup != null) rewardPopup.SetActive(false);
    }

    // [수정] 파라미터 추가됨
    void ShowClickEffect(long profit, bool isCritical)
    {
        GameObject instance = Instantiate(effectPrefab, Input.mousePosition, Quaternion.identity, effectParent);
        FloatingText floatingText = instance.GetComponent<FloatingText>();

        if (floatingText != null)
        {
            // [수정] 포맷팅 적용
            if (isCritical) floatingText.SetCritical($"CRITICAL!\n+{FormatNumber(profit)}");
            else floatingText.SetText($"+{FormatNumber(profit)}");
        }
    }

    IEnumerator BounceAnimation()
    {
        Vector3 startScale = Vector3.one;
        Vector3 targetScale = Vector3.one * 0.9f;

        float time = 0f;
        while (time < 0.1f)
        {
            time += Time.deltaTime;
            serverButtonTr.localScale = Vector3.Lerp(startScale, targetScale, time / 0.1f);
            yield return null;
        }

        time = 0f;
        while (time < 0.1f)
        {
            time += Time.deltaTime;
            serverButtonTr.localScale = Vector3.Lerp(targetScale, startScale, time / 0.1f);
            yield return null;
        }
        serverButtonTr.localScale = startScale;
    }

    public void UpdateUI()
    {
        // 1. 왼쪽: 현재 보유 로그 (널 가드 장착)
        if (scoreText != null)
        {
            scoreText.text = $"{FormatNumber(logs)} <size=70%>Logs</size>";
            if (isCoffeeTime) scoreText.color = Color.yellow;
            else scoreText.color = Color.green;
        }

        // 2. 오른쪽: 상세 정보 (My Info)
        if (myInfoText != null)
        {
            long currentLPS = GetTotalRevenue();
            
            float totalMultiplier = 1f;
            if (isCoffeeTime) totalMultiplier *= 2f;
            if (isOverclock) totalMultiplier *= 5f;
            totalMultiplier *= (1f + (goldenDisks * 0.1f)); // 환생 보너스

            // 장비 곱연산 배율 누적
            float equipMultiplier = GetEquipmentMultiplier();
            totalMultiplier *= equipMultiplier;

            myInfoText.text = 
                $"<color=#00FF00>⚡ {FormatNumber(currentLPS)} /sec</color>\n" +
                $"<color=#FFD700>💾 Disk: {goldenDisks}</color> | <color=#00FFFF>💻 Lv.{serverLevel}</color>\n" +
                $"<size=80%>(Current Boost: <color=orange>x{totalMultiplier:F1}</color>)</size>";
        }

        // 3. [신규] 업그레이드 버튼 텍스트 갱신
        if (upgradeText != null)
        {
            upgradeText.text = $"<b>CPU Upgrade (Lv.{serverLevel})</b>\nCost: {FormatNumber(upgradeCost)}";
            upgradeText.color = (logs >= upgradeCost) ? Color.cyan : Color.red;
        }

        // 4. 상점 목록 갱신 (리스트 및 인덱스 널 가드 장착)
        if (generatedButtonTexts != null && units != null)
        {
            for (int i = 0; i < units.Count; i++)
            {
                if (i < generatedButtonTexts.Count && generatedButtonTexts[i] != null)
                {
                    Unit u = units[i];
                    string colorHex = (logs >= u.currentCost) ? "#00FF00" : "#FF0000";

                    generatedButtonTexts[i].text =
                        $"<b>{u.unitName}</b> <color=orange>Lv.{u.count}</color>\n" +
                        $"<size=80%>{FormatNumber(u.revenuePerSec)} /sec</size>\n" +
                        $"<color={colorHex}>Cost: {FormatNumber(u.currentCost)}</color>";
                }
            }
        }
    }

    // [신규] 커피 버튼 누르면 실행될 함수
    public void ActivateCoffee()
    {
        if (!isCoffeeTime && coffeeBtn.interactable)
        {
            StartCoroutine(CoffeeRoutine());
        }
    }

    // [신규] 버프 및 쿨타임 관리 코루틴
    IEnumerator CoffeeRoutine()
    {
        // --- 1. 버프 시작 ---
        isCoffeeTime = true;
        coffeeBtn.interactable = false; // 버튼 잠금
        terminalManager?.AddLog("<color=yellow><b>[BOOST] COFFEE TIME! Speed x2</b></color>");

        // 지속 시간 동안 게이지 줄이기 (1 -> 0)
        float timer = 0f;
        while (timer < skillDuration)
        {
            timer += Time.deltaTime;
            // 남은 시간 비율 (100% -> 0%)
            coffeeSlider.value = 1f - (timer / skillDuration);
            yield return null;
        }

        // --- 2. 버프 종료 ---
        isCoffeeTime = false;
        terminalManager?.AddLog("[INFO] Caffeine worn off...");
        UpdateUI(); // 색깔 원상복구

        // --- 3. 쿨타임 시작 ---
        // 게이지가 다시 차오름 (0 -> 1)
        timer = 0f;
        while (timer < skillCooldown)
        {
            timer += Time.deltaTime;
            coffeeSlider.value = timer / skillCooldown;
            yield return null;
        }

        // --- 4. 쿨타임 끝 (재사용 가능) ---
        coffeeBtn.interactable = true;
        coffeeSlider.value = 1f; // 꽉 참
        terminalManager?.AddLog("<color=white>Coffee is ready!</color>");
    }

    // --- 마이그레이션(환생) 시스템 ---

    // 1. 팝업창 열기 (버튼 누르면 실행)
    public void OpenMigrationPopup()
    {
        // 최소 100만 로그는 있어야 가능 (너무 빠르면 재미없음)
        if (logs < 1000000)
        {
            terminalManager?.AddLog("<color=red><b>[ERROR] 최소 1.00M Logs가 필요합니다!</b></color>");
            return;
        }

        // 보상 계산 공식: 루트(현재 로그 / 100만)
        // 예: 100만 -> 1개, 400만 -> 2개, 1억 -> 10개
        int potentialDisks = (int)Mathf.Sqrt(logs / 1000000f);

        if (migrationPanel != null)
        {
            migrationPanel.SetActive(true); // 창 켜기
            migrationPanel.transform.SetAsLastSibling(); // 맨 위로 올리기

            // 설명 텍스트 갱신
            if (migrationInfoText != null)
            {
                migrationInfoText.text =
                    $"서버를 클라우드로 이전하시겠습니까?\n\n" +
                    $"현재 데이터는 <color=red>초기화</color>되지만\n" +
                    $"<color=yellow>황금 디스크 {potentialDisks}개</color>를 얻습니다.\n\n" +
                    $"현재 보유: {goldenDisks}개\n" +
                    $"총 수익 보너스: <color=green>+{(goldenDisks + potentialDisks) * 10}%</color>";
            }
        }
    }

    // 2. 팝업창 닫기 (취소 버튼)
    public void CloseMigrationPopup()
    {
        if (migrationPanel != null) migrationPanel.SetActive(false);
    }

    // 3. 진짜 초기화 실행 (확인 버튼)
    public void ConfirmMigration()
    {
        // 받을 디스크 계산
        int potentialDisks = (int)Mathf.Sqrt(logs / 1000000f);

        // 1. 디스크 지급 (영구 보존)
        goldenDisks += potentialDisks;

        // 2. 데이터 초기화 (로그, 레벨, 업그레이드 비용)
        logs = 0;
        serverLevel = 1;
        upgradeCost = 100;

        // 3. 유닛 초기화
        foreach (var u in units)
        {
            u.count = 0;
            u.LoadCount(0); // 가격도 초기화
        }

        // 4. 저장 및 UI 갱신
        SaveGame();
        UpdateUI();
        CloseMigrationPopup();

        // 5. 멋진 로그 출력
        terminalManager?.AddLog($"<color=#00FFFF><b>=== MIGRATION COMPLETED ===</b></color>");
        terminalManager?.AddLog($"<color=yellow>New Bonus: +{goldenDisks * 10}% Revenue!</color>");
    }

    // ---------------------------------------------------------
    // 🎰 가차 (뽑기) 장비 시스템 추가 구현부
    // ---------------------------------------------------------
    [Header("Gacha UI System")]
    public GameObject gachaPanel;
    public TextMeshProUGUI gachaResultText;
    public TextMeshProUGUI equipmentListText;
    public TextMeshProUGUI gachaCostText;

    [HideInInspector]
    public List<Equipment> equipments = new List<Equipment>();
    [HideInInspector]
    public long gachaCost = 50000; // 초기 비용 5만 로그

    void InitializeEquipments()
    {
        if (equipments == null) equipments = new List<Equipment>();
        equipments.Clear();
        // Common (일반)
        equipments.Add(new Equipment("다이소 마우스", "Common", 2f, "Click", "클릭당 로그 획득 +2"));
        equipments.Add(new Equipment("멤브레인 키보드", "Common", 15f, "LPS", "초당 로그 생산 +15"));
        equipments.Add(new Equipment("15인치 구형 CRT 모니터", "Common", 40f, "LPS", "초당 로그 생산 +40"));

        // Rare (희귀)
        equipments.Add(new Equipment("버티컬 마우스", "Rare", 10f, "Click", "클릭당 로그 획득 +10"));
        equipments.Add(new Equipment("한성 무접점 키보드", "Rare", 200f, "LPS", "초당 로그 생산 +200"));
        equipments.Add(new Equipment("FHD 듀얼 모니터", "Rare", 500f, "LPS", "초당 로그 생산 +500"));

        // Epic (에픽)
        equipments.Add(new Equipment("로지텍 지프로 무선", "Epic", 50f, "Click", "클릭당 로그 획득 +50"));
        equipments.Add(new Equipment("리얼포스 키보드", "Epic", 2000f, "LPS", "초당 로그 생산 +2,000"));
        equipments.Add(new Equipment("4K 울트라와이드 모니터", "Epic", 6000f, "LPS", "초당 로그 생산 +6,000"));

        // Legendary (전설)
        equipments.Add(new Equipment("RTX 5090 Ti 그래픽카드", "Legendary", 0.5f, "Multiplier", "서버 오버클럭/기본 배율 +50% (+0.5)"));
        equipments.Add(new Equipment("허먼밀러 에어론 의자", "Legendary", 1.0f, "Multiplier", "서버 전체 로그 수입 +100% (+1.0)"));
    }

    public long GetEquipmentLPS()
    {
        long lps = 0;
        if (equipments == null) return 0;
        foreach (var eq in equipments)
        {
            if (eq.bonusType == "LPS") lps += (long)(eq.statBonus * eq.level);
        }
        return lps;
    }

    public long GetEquipmentClick()
    {
        long click = 0;
        if (equipments == null) return 0;
        foreach (var eq in equipments)
        {
            if (eq.bonusType == "Click") click += (long)(eq.statBonus * eq.level);
        }
        return click;
    }

    public float GetEquipmentMultiplier()
    {
        float mult = 1f;
        if (equipments == null) return 1f;
        foreach (var eq in equipments)
        {
            if (eq.bonusType == "Multiplier") mult += (eq.statBonus * eq.level);
        }
        return mult;
    }

    public void OpenGachaPanel()
    {
        if (gachaPanel != null)
        {
            gachaPanel.SetActive(true);
            gachaPanel.transform.SetAsLastSibling();
            UpdateGachaUI();
        }
    }

    public void CloseGachaPanel()
    {
        if (gachaPanel != null) gachaPanel.SetActive(false);
    }

    public void DrawEquipmentGacha()
    {
        if (equipments == null || equipments.Count == 0) InitializeEquipments();

        if (logs < gachaCost)
        {
            terminalManager?.AddLog("<color=red>[ERROR] 가차 비용이 부족합니다!</color>");
            return;
        }

        logs -= gachaCost;
        long lastCost = gachaCost;
        gachaCost = (long)(gachaCost * 1.25f); // 비용 점진적 25% 상승

        // 가차 뽑기 (확률: Common 60%, Rare 30%, Epic 8.5%, Legendary 1.5%)
        float dice = UnityEngine.Random.Range(0f, 100f);
        string targetRarity = "Common";

        if (dice < 1.5f) targetRarity = "Legendary";
        else if (dice < 10.0f) targetRarity = "Epic";
        else if (dice < 40.0f) targetRarity = "Rare";
        else targetRarity = "Common";

        List<Equipment> candidates = equipments.FindAll(e => e.rarity == targetRarity);
        if (candidates.Count == 0) candidates = equipments;

        Equipment drawn = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        drawn.level++;

        SaveGame();
        UpdateUI();
        UpdateGachaUI();

        string rarityColor = "#FFFFFF";
        if (drawn.rarity == "Rare") rarityColor = "#32CD32";
        else if (drawn.rarity == "Epic") rarityColor = "#1E90FF";
        else if (drawn.rarity == "Legendary") rarityColor = "#FFD700";

        // 터미널 피드백
        terminalManager?.AddLog($"🎰 <color=yellow><b>[GACHA]</b></color> <color={rarityColor}><b>[{drawn.rarity}] {drawn.equipName}</b></color> 획득! (현재 Lv.{drawn.level})");
        terminalManager?.AddLog($"   └ 효과: {drawn.description}");

        if (gachaResultText != null)
        {
            gachaResultText.text = $"🎰 <color={rarityColor}><b>[{drawn.rarity}]</b></color> 획득!\n<size=120%><b>{drawn.equipName}</b></size>\n\n<size=80%>{drawn.description}</size>";
        }

        if (buttonShaker != null && drawn.rarity == "Legendary")
        {
            buttonShaker.Shake();
        }
    }

    public void UpdateGachaUI()
    {
        if (gachaCostText != null)
        {
            gachaCostText.text = $"🎰 <b>1회 뽑기</b>\nCost: {FormatNumber(gachaCost)} Logs";
            gachaCostText.color = (logs >= gachaCost) ? Color.yellow : Color.red;
        }

        if (equipmentListText != null && equipments != null)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("<color=yellow>=== 보유 장비 목록 (도감) ===</color>");
            foreach (var eq in equipments)
            {
                string rarityColor = "#FFFFFF";
                if (eq.rarity == "Rare") rarityColor = "#32CD32";
                else if (eq.rarity == "Epic") rarityColor = "#1E90FF";
                else if (eq.rarity == "Legendary") rarityColor = "#FFD700";

                sb.AppendLine($"<color={rarityColor}>[{eq.rarity}]</color> {eq.equipName} <color=orange>Lv.{eq.level}</color>");
                sb.AppendLine($"<size=85%>{eq.description} (합계: +{eq.statBonus * eq.level:F1})</size>");
            }
            equipmentListText.text = sb.ToString();
        }
    }
}