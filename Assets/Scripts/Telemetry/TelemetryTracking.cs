using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;

public class TelemetryTracking : MonoBehaviour
{
    [Header("Telemetry Output")]
    [SerializeField] private bool persistAcrossScenes = true;
    [SerializeField] private float locationSampleIntervalSeconds = 1f;
    [SerializeField] private Key telemetryMenuToggleKey = Key.F10;

    [Header("Recording Metadata")]
    [SerializeField] private string testerName = string.Empty;
    [SerializeField] private string recordingPurpose = string.Empty;

    private static TelemetryTracking instance;

    private readonly HashSet<Key> pressedKeys = new();
    private bool leftMousePressed;
    private bool rightMousePressed;
    private bool middleMousePressed;
    private bool forwardMousePressed;
    private bool backMousePressed;

    private float nextLocationSampleTime;
    private string outputFilePath;
    private TelemetrySessionData telemetryData;
    private Transform cachedPlayerTransform;
    private bool sessionClosed;
    private bool isRecording;
    private bool showTelemetryGui;
    private Rect telemetryWindowRect = new Rect(20f, 20f, 430f, 240f);
    private PlayerMovement cachedPlayerMovement;
    private bool hadCanMoveBeforeMenu;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        if (persistAcrossScenes)
            DontDestroyOnLoad(gameObject);

    }

    private void OnEnable()
    {
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
    }

    private void OnDisable()
    {
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
    }

    private void Start()
    {
        SetTelemetryGuiVisible(true);
    }

    private void Update()
    {
        HandleGuiToggleInput();

        if (!isRecording)
            return;

        CaptureKeyboardInput();
        CaptureMouseInput();
        CaptureLocationSample();
    }

    private void OnApplicationQuit()
    {
        StopRecordingSession();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            StopRecordingSession();
            instance = null;
        }
    }

    public static void RecordWeakPointDestroyed(string weakPointName, WeakPointType weakPointType, string enemyName)
    {
        if (instance == null || !instance.isRecording)
            return;

        instance.RecordEvent(
            "WeakPointDestroyed",
            "weakPoint", weakPointName,
            "weakPointType", weakPointType.ToString(),
            "enemy", enemyName);
    }

    public static void RecordEnemyDestroyed(string enemyName)
    {
        if (instance == null || !instance.isRecording)
            return;

        instance.RecordEvent("EnemyDestroyed", "enemy", enemyName);
    }

    public static void RecordScan(string scannedObjectName, string entryName)
    {
        if (instance == null || !instance.isRecording)
            return;

        instance.RecordEvent(
            "GrimoireScan",
            "object", scannedObjectName,
            "entry", entryName);
    }

    public static void RecordInteraction(string interactionType, string objectName, string details)
    {
        if (instance == null || !instance.isRecording)
            return;

        instance.RecordEvent(
            "PlayerInteraction",
            "interactionType", interactionType,
            "object", objectName,
            "details", details);
    }

    private void InitializeTelemetryFile()
    {
        sessionClosed = false;
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        outputFilePath = Path.Combine(Application.persistentDataPath, $"telemetry_{timestamp}.json");

        telemetryData = new TelemetrySessionData
        {
            createdAtLocal = DateTime.Now.ToString("O", CultureInfo.InvariantCulture),
            createdAtUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            tester = string.IsNullOrWhiteSpace(testerName) ? "Unknown" : testerName.Trim(),
            purpose = string.IsNullOrWhiteSpace(recordingPurpose) ? "Unspecified" : recordingPurpose.Trim(),
            platform = Application.platform.ToString(),
            applicationVersion = Application.version,
            unityVersion = Application.unityVersion,
            outputFile = outputFilePath,
            systemInfo = CreateSystemInformationData(),
            entries = new List<TelemetryEventData>()
        };

        SaveDocument();
    }

    private SystemInfoData CreateSystemInformationData()
    {
        Resolution resolution = Screen.currentResolution;

        return new SystemInfoData
        {
            operatingSystem = SystemInfo.operatingSystem,
            deviceName = SystemInfo.deviceName,
            deviceModel = SystemInfo.deviceModel,
            deviceType = SystemInfo.deviceType.ToString(),
            processorType = SystemInfo.processorType,
            processorCount = SystemInfo.processorCount,
            systemMemoryMB = SystemInfo.systemMemorySize,
            graphicsDeviceName = SystemInfo.graphicsDeviceName,
            graphicsDeviceType = SystemInfo.graphicsDeviceType.ToString(),
            graphicsMemoryMB = SystemInfo.graphicsMemorySize,
            graphicsDriverVersion = SystemInfo.graphicsDeviceVersion,
            screenResolution = $"{resolution.width}x{resolution.height}@{resolution.refreshRateRatio.value:0.##}Hz",
            currentScreenSize = $"{Screen.width}x{Screen.height}",
            screenDpi = Screen.dpi,
            systemLanguage = Application.systemLanguage.ToString(),
            internetReachability = Application.internetReachability.ToString()
        };
    }

    private void HandleGuiToggleInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        KeyControl toggleKeyControl = keyboard[telemetryMenuToggleKey];
        if (toggleKeyControl != null && toggleKeyControl.wasPressedThisFrame)
            SetTelemetryGuiVisible(!showTelemetryGui);
    }

    private void OnGUI()
    {
        if (!showTelemetryGui)
        {
            if (GUI.Button(new Rect(20f, 20f, 120f, 32f), "Telemetry"))
                SetTelemetryGuiVisible(true);

            return;
        }

        telemetryWindowRect = GUI.Window(962134, telemetryWindowRect, DrawTelemetryWindow, "Telemetry Recorder");
    }

    private void DrawTelemetryWindow(int windowId)
    {
        GUI.Label(new Rect(12f, 28f, 120f, 20f), "Tester");
        testerName = GUI.TextField(new Rect(140f, 26f, 275f, 24f), testerName ?? string.Empty, 120);

        GUI.Label(new Rect(12f, 62f, 120f, 20f), "Purpose");
        recordingPurpose = GUI.TextField(new Rect(140f, 60f, 275f, 24f), recordingPurpose ?? string.Empty, 180);

        string statusText = isRecording
            ? "Recording: Active"
            : "Recording: Stopped";
        GUI.Label(new Rect(12f, 98f, 405f, 20f), statusText);

        string outputText = string.IsNullOrWhiteSpace(outputFilePath)
            ? "Output: Not started"
            : $"Output: {outputFilePath}";
        GUI.Label(new Rect(12f, 122f, 405f, 36f), outputText);

        if (!isRecording)
        {
            if (GUI.Button(new Rect(12f, 170f, 128f, 28f), "Start Record"))
                StartRecordingSession();
        }
        else
        {
            if (GUI.Button(new Rect(12f, 170f, 128f, 28f), "Stop Record"))
                StopRecordingSession();
        }

        if (GUI.Button(new Rect(150f, 170f, 128f, 28f), "Hide"))
            SetTelemetryGuiVisible(false);

        GUI.DragWindow(new Rect(0f, 0f, 430f, 22f));
    }

    private void SetTelemetryGuiVisible(bool isVisible)
    {
        showTelemetryGui = isVisible;
        cachedPlayerMovement ??= FindAnyObjectByType<PlayerMovement>();

        if (showTelemetryGui)
        {
            if (cachedPlayerMovement != null)
            {
                hadCanMoveBeforeMenu = cachedPlayerMovement.canMove;
                cachedPlayerMovement.canMove = false;
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        if (cachedPlayerMovement != null)
            cachedPlayerMovement.canMove = hadCanMoveBeforeMenu;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void StartRecordingSession()
    {
        if (isRecording)
            return;

        InitializeTelemetryFile();
        isRecording = true;
        nextLocationSampleTime = 0f;
        pressedKeys.Clear();
        leftMousePressed = false;
        rightMousePressed = false;
        middleMousePressed = false;
        forwardMousePressed = false;
        backMousePressed = false;

        RecordEvent("SessionStarted");
        RecordEvent("SystemInfoCaptured");
    }

    private void StopRecordingSession()
    {
        if (!isRecording)
            return;

        isRecording = false;
        CloseSession();
    }

    private void CaptureKeyboardInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        foreach (KeyControl keyControl in keyboard.allKeys)
        {
            Key key = keyControl.keyCode;
            bool isPressed = keyControl.isPressed;
            bool wasPressed = pressedKeys.Contains(key);

            if (isPressed && !wasPressed)
            {
                pressedKeys.Add(key);
                RecordEvent("KeyPressed", "key", key.ToString());
            }
            else if (!isPressed && wasPressed)
            {
                pressedKeys.Remove(key);
                RecordEvent("KeyReleased", "key", key.ToString());
            }
        }
    }

    private void CaptureMouseInput()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null)
            return;

        HandleMouseButton("Left", mouse.leftButton, ref leftMousePressed);
        HandleMouseButton("Right", mouse.rightButton, ref rightMousePressed);
        HandleMouseButton("Middle", mouse.middleButton, ref middleMousePressed);
        HandleMouseButton("Forward", mouse.forwardButton, ref forwardMousePressed);
        HandleMouseButton("Back", mouse.backButton, ref backMousePressed);
    }

    private void HandleMouseButton(string buttonName, ButtonControl buttonControl, ref bool cachedState)
    {
        bool isPressed = buttonControl.isPressed;

        if (isPressed && !cachedState)
            RecordEvent("MouseButtonPressed", "button", buttonName);
        else if (!isPressed && cachedState)
            RecordEvent("MouseButtonReleased", "button", buttonName);

        cachedState = isPressed;
    }

    private void CaptureLocationSample()
    {
        if (locationSampleIntervalSeconds <= 0f)
            return;

        if (Time.unscaledTime < nextLocationSampleTime)
            return;

        nextLocationSampleTime = Time.unscaledTime + locationSampleIntervalSeconds;
        RecordEvent("PlayerLocationSample");
    }

    private void OnActiveSceneChanged(Scene previousScene, Scene newScene)
    {
        if (!isRecording)
            return;

        RecordEvent(
            "SceneChanged",
            "previousScene", previousScene.name,
            "newScene", newScene.name);
    }

    private void CloseSession()
    {
        if (sessionClosed || telemetryData == null)
            return;

        sessionClosed = true;
        RecordEvent("SessionEnded");
    }

    private void RecordEvent(string eventType, params string[] details)
    {
        if (telemetryData == null)
            return;

        DateTime nowLocal = DateTime.Now;
        DateTime nowUtc = DateTime.UtcNow;
        Scene activeScene = SceneManager.GetActiveScene();
        string location = GetPlayerLocationString();

        TelemetryEventData eventData = new TelemetryEventData
        {
            type = eventType,
            timeLocal = nowLocal.ToString("O", CultureInfo.InvariantCulture),
            timeUtc = nowUtc.ToString("O", CultureInfo.InvariantCulture),
            scene = string.IsNullOrEmpty(activeScene.name) ? "Unknown" : activeScene.name,
            playerLocation = location,
            details = new List<TelemetryDetailData>()
        };

        if (details != null)
        {
            for (int i = 0; i + 1 < details.Length; i += 2)
            {
                eventData.details.Add(new TelemetryDetailData
                {
                    key = details[i],
                    value = details[i + 1] ?? string.Empty
                });
            }
        }

        telemetryData.entries.Add(eventData);
        SaveDocument();
    }

    private string GetPlayerLocationString()
    {
        Transform player = ResolvePlayerTransform();
        if (player == null)
            return "Unknown";

        Vector3 position = player.position;
        return string.Format(
            CultureInfo.InvariantCulture,
            "{0:0.###},{1:0.###},{2:0.###}",
            position.x,
            position.y,
            position.z);
    }

    private Transform ResolvePlayerTransform()
    {
        if (cachedPlayerTransform != null)
            return cachedPlayerTransform;

        GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
        if (taggedPlayer != null)
        {
            cachedPlayerTransform = taggedPlayer.transform;
            return cachedPlayerTransform;
        }

        PlayerMovement playerMovement = FindAnyObjectByType<PlayerMovement>();
        if (playerMovement != null)
            cachedPlayerTransform = playerMovement.transform;

        return cachedPlayerTransform;
    }

    private void SaveDocument()
    {
        if (telemetryData == null || string.IsNullOrEmpty(outputFilePath))
            return;

        string json = JsonUtility.ToJson(telemetryData, true);
        File.WriteAllText(outputFilePath, json);
    }

    [Serializable]
    private class TelemetrySessionData
    {
        public string createdAtLocal;
        public string createdAtUtc;
        public string tester;
        public string purpose;
        public string platform;
        public string applicationVersion;
        public string unityVersion;
        public string outputFile;
        public SystemInfoData systemInfo;
        public List<TelemetryEventData> entries;
    }

    [Serializable]
    private class SystemInfoData
    {
        public string operatingSystem;
        public string deviceName;
        public string deviceModel;
        public string deviceType;
        public string processorType;
        public int processorCount;
        public int systemMemoryMB;
        public string graphicsDeviceName;
        public string graphicsDeviceType;
        public int graphicsMemoryMB;
        public string graphicsDriverVersion;
        public string screenResolution;
        public string currentScreenSize;
        public float screenDpi;
        public string systemLanguage;
        public string internetReachability;
    }

    [Serializable]
    private class TelemetryEventData
    {
        public string type;
        public string timeLocal;
        public string timeUtc;
        public string scene;
        public string playerLocation;
        public List<TelemetryDetailData> details;
    }

    [Serializable]
    private class TelemetryDetailData
    {
        public string key;
        public string value;
    }
}
