using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using UnityEngine.UI;
using System.IO;

public class AutoBuilder
{
    [MenuItem("Tools/1. Setup InputField & Send Button Only")]
    public static void SetupOnly()
    {
        Debug.Log("[AutoBuilder] Starting UI setup with Mobile Send Button...");
        string scenePath = "Assets/Scenes/SampleScene.unity";
        var scene = EditorSceneManager.OpenScene(scenePath);

        TerminalManager terminalManager = Object.FindAnyObjectByType<TerminalManager>();
        if (terminalManager == null)
        {
            Debug.LogError("[AutoBuilder] TerminalManager not found in scene!");
            EditorUtility.DisplayDialog("Error", "TerminalManager not found in the scene!", "OK");
            return;
        }

        // 1. InputField 생성 및 할당
        if (terminalManager.inputField == null)
        {
            TMP_InputField existingInputField = terminalManager.GetComponentInChildren<TMP_InputField>();
            if (existingInputField == null)
            {
                Canvas canvas = terminalManager.GetComponentInParent<Canvas>();
                if (canvas == null)
                {
                    Debug.LogError("[AutoBuilder] Canvas not found in parent hierarchy!");
                    return;
                }

                // TMP_InputField 컨테이너 생성
                GameObject inputFieldGo = new GameObject("TerminalInputField", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_InputField));
                inputFieldGo.transform.SetParent(terminalManager.transform, false);

                // RectTransform 조정 (오른쪽 110만큼 띄워서 전송 버튼 공간 확보)
                RectTransform rect = inputFieldGo.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0, 0);
                rect.anchorMax = new Vector2(1, 0);
                rect.pivot = new Vector2(0, 0);
                rect.anchoredPosition = new Vector2(10, 10);
                rect.sizeDelta = new Vector2(-120, 40); // 오른쪽 110px 여백 + 왼쪽 10px 여백

                Image bgImage = inputFieldGo.GetComponent<Image>();
                bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

                GameObject textAreaGo = new GameObject("TextArea", typeof(RectTransform), typeof(RectMask2D));
                textAreaGo.transform.SetParent(inputFieldGo.transform, false);
                RectTransform textAreaRect = textAreaGo.GetComponent<RectTransform>();
                textAreaRect.anchorMin = Vector2.zero;
                textAreaRect.anchorMax = Vector2.one;
                textAreaRect.sizeDelta = new Vector2(-10, -10);

                GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                textGo.transform.SetParent(textAreaGo.transform, false);
                RectTransform textRect = textGo.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.sizeDelta = Vector2.zero;

                TextMeshProUGUI textMesh = textGo.GetComponent<TextMeshProUGUI>();
                textMesh.fontSize = 18;
                textMesh.color = Color.green;
                textMesh.extraPadding = true;

                GameObject placeholderGo = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
                placeholderGo.transform.SetParent(textAreaGo.transform, false);
                RectTransform placeholderRect = placeholderGo.GetComponent<RectTransform>();
                placeholderRect.anchorMin = Vector2.zero;
                placeholderRect.anchorMax = Vector2.one;
                placeholderRect.sizeDelta = Vector2.zero;

                TextMeshProUGUI placeholderMesh = placeholderGo.GetComponent<TextMeshProUGUI>();
                placeholderMesh.fontSize = 18;
                placeholderMesh.color = new Color(0, 1, 0, 0.4f);
                placeholderMesh.text = "Enter command here...";
                placeholderMesh.fontStyle = FontStyles.Italic;

                TMP_InputField inputField = inputFieldGo.GetComponent<TMP_InputField>();
                inputField.textViewport = textAreaRect;
                inputField.textComponent = textMesh;
                inputField.placeholder = placeholderMesh;

                if (terminalManager.terminalText != null)
                {
                    textMesh.font = terminalManager.terminalText.font;
                    placeholderMesh.font = terminalManager.terminalText.font;
                }

                terminalManager.inputField = inputField;
                Debug.Log("[AutoBuilder] TMP_InputField successfully created.");
            }
            else
            {
                terminalManager.inputField = existingInputField;
                Debug.Log("[AutoBuilder] Found existing TMP_InputField. Reference assigned.");
            }
        }

        // 2. 모바일 전송(Send) 버튼 생성 및 연결
        Button existingSendBtn = terminalManager.GetComponentInChildren<Button>(true);
        if (existingSendBtn == null)
        {
            Debug.Log("[AutoBuilder] Creating new Mobile Send Button...");
            GameObject sendBtnGo = new GameObject("TerminalSendButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            sendBtnGo.transform.SetParent(terminalManager.transform, false);

            // 전송 버튼 위치 조정 (우측 하단 고정)
            RectTransform sendRect = sendBtnGo.GetComponent<RectTransform>();
            sendRect.anchorMin = new Vector2(1, 0);
            sendRect.anchorMax = new Vector2(1, 0);
            sendRect.pivot = new Vector2(1, 0);
            sendRect.anchoredPosition = new Vector2(-10, 10);
            sendRect.sizeDelta = new Vector2(90, 40);

            // 버튼 색상 세팅
            Image btnImage = sendBtnGo.GetComponent<Image>();
            btnImage.color = new Color(0.12f, 0.45f, 0.22f, 1f); // 어두운 초록색 (터미널 테마)

            // 버튼 텍스트 추가
            GameObject btnTextGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            btnTextGo.transform.SetParent(sendBtnGo.transform, false);
            RectTransform btnTextRect = btnTextGo.GetComponent<RectTransform>();
            btnTextRect.anchorMin = Vector2.zero;
            btnTextRect.anchorMax = Vector2.one;
            btnTextRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI btnText = btnTextGo.GetComponent<TextMeshProUGUI>();
            btnText.fontSize = 16;
            btnText.color = Color.white;
            btnText.alignment = TextAlignmentOptions.Center;
            btnText.text = "Send";
            btnText.fontStyle = FontStyles.Bold;
            if (terminalManager.terminalText != null)
            {
                btnText.font = terminalManager.terminalText.font;
            }

            Debug.Log("[AutoBuilder] TerminalSendButton successfully created.");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[AutoBuilder] Scene modified and saved.");
        EditorUtility.DisplayDialog("Success", "Terminal InputField & Send Button setup has been completed!", "OK");
    }

    [MenuItem("Tools/2. Setup and Build PC (Windows EXE)")]
    public static void PerformBuild()
    {
        SetupOnly();

        Debug.Log("[AutoBuilder] Starting standalone PC build...");
        string scenePath = "Assets/Scenes/SampleScene.unity";
        string buildFolder = Path.Combine(Directory.GetCurrentDirectory(), "Build");
        if (!Directory.Exists(buildFolder))
        {
            Directory.CreateDirectory(buildFolder);
        }

        string buildPath = Path.Combine(buildFolder, "SoloLeveling.exe");
        
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = new[] { scenePath };
        buildPlayerOptions.locationPathName = buildPath;
        buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
        buildPlayerOptions.options = BuildOptions.None;

        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        var summary = report.summary;

        if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log($"[AutoBuilder] PC Build succeeded! Path: {buildPath}");
            EditorUtility.DisplayDialog("Success", $"PC Build succeeded!\nPath: {buildPath}", "OK");
        }
        else
        {
            Debug.LogError($"[AutoBuilder] PC Build failed with result: {summary.result}");
            EditorUtility.DisplayDialog("Build Failed", $"PC Build failed with result: {summary.result}", "OK");
        }
    }

    [MenuItem("Tools/3. Setup and Build Android APK")]
    public static void PerformAndroidBuild()
    {
        SetupOnly();

        Debug.Log("[AutoBuilder] Starting standalone Android build...");
        string scenePath = "Assets/Scenes/SampleScene.unity";
        string buildFolder = Path.Combine(Directory.GetCurrentDirectory(), "Build");
        if (!Directory.Exists(buildFolder))
        {
            Directory.CreateDirectory(buildFolder);
        }

        string buildPath = Path.Combine(buildFolder, "SoloLeveling.apk");
        
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = new[] { scenePath };
        buildPlayerOptions.locationPathName = buildPath;
        buildPlayerOptions.target = BuildTarget.Android;
        buildPlayerOptions.options = BuildOptions.None;

        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        var summary = report.summary;

        if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log($"[AutoBuilder] Android Build succeeded! Path: {buildPath}");
            EditorUtility.DisplayDialog("Success", $"Android Build succeeded!\nPath: {buildPath}", "OK");
        }
        else
        {
            Debug.LogError($"[AutoBuilder] Android Build failed with result: {summary.result}");
            EditorUtility.DisplayDialog("Build Failed", $"Android Build failed with result: {summary.result}\n(Android SDK/NDK 설정이 완료되어 있어야 빌드가 성공합니다.)", "OK");
        }
    }
}
