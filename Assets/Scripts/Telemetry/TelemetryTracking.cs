using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class TelemetryTracking : MonoBehaviour
{
    public static TelemetryTracking Instance { get; private set; }

    [Header("Output")]
    [SerializeField] private string outputFolderName = "TelemetryLogs";
    [SerializeField] private bool prettyJson = true;
    [SerializeField] private bool autosaveOnQuit = true;

    [Header("Player Tracking")]
    [SerializeField] private Transform playerTransform;

    [Header("Input Tracking")]
    [SerializeField] private bool trackInputSystemActions = true;

    private TelemetrySession session;
    private TelemetryUploader telemetryUploader;
    private bool warnedMissingPlayerTransform;
    private string outputPath;
    private bool saved;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (playerTransform == null)
        {
            var playerMovement = FindAnyObjectByType<PlayerMovement>();
            if (playerMovement != null)
                playerTransform = playerMovement.transform;
        }

        CreateSession();
        telemetryUploader = GetComponent<TelemetryUploader>();
        SceneManager.activeSceneChanged += OnSceneChanged;
        LogEvent("Session", "SessionStarted", "Telemetry session started", null, GetCurrentPlayerPosition());
    }

    private void Update()
    {
        TryResolvePlayerTransform();
        TrackInput();
    }

    private void OnApplicationQuit()
    {
        if (autosaveOnQuit)
            Save();
    }

    private void OnDestroy()
    {
        SceneManager.activeSceneChanged -= OnSceneChanged;

        if (Instance == this)
            Instance = null;

        if (autosaveOnQuit && !saved)
            Save();
    }

    public static void RecordWeakPointDestroyed(string weakPointName, WeakPointType weakPointType, string enemyName)
    {
        if (Instance == null)
            return;

        var details = new List<TelemetryKeyValue>
        {
            new TelemetryKeyValue("weakPointName", weakPointName),
            new TelemetryKeyValue("weakPointType", weakPointType.ToString()),
            new TelemetryKeyValue("enemyName", enemyName)
        };

        Vector3? pos = Instance.GetCurrentPlayerPosition();
        string description = "Shot " + weakPointType + " weakpoint '" + weakPointName + "' on enemy '" + enemyName + "'";
        Instance.LogEvent("Combat", "WeakPointDestroyed", description, details, pos);
    }

    public static void RecordEnemyDestroyed(string enemyName)
    {
        if (Instance == null)
            return;

        var details = new List<TelemetryKeyValue>
        {
            new TelemetryKeyValue("enemyName", enemyName)
        };

        Vector3? pos = Instance.GetCurrentPlayerPosition();
        Instance.LogEvent("Combat", "EnemyDestroyed", "Destroyed enemy '" + enemyName + "'", details, pos);
    }

    public static void RecordInteraction(string interactionType, string targetName, string note)
    {
        if (Instance == null)
            return;

        var details = new List<TelemetryKeyValue>
        {
            new TelemetryKeyValue("interactionType", interactionType),
            new TelemetryKeyValue("targetName", targetName),
            new TelemetryKeyValue("note", note)
        };

        Vector3? pos = Instance.GetCurrentPlayerPosition();
        string description = interactionType + " with '" + targetName + "'";
        if (!string.IsNullOrWhiteSpace(note))
            description += " - " + note;

        Instance.LogEvent("Interaction", "WorldInteraction", description, details, pos);
    }

    public static void RecordScan(string targetName, string entryName)
    {
        if (Instance == null)
            return;

        var details = new List<TelemetryKeyValue>
        {
            new TelemetryKeyValue("targetName", targetName),
            new TelemetryKeyValue("entryName", entryName)
        };

        Vector3? pos = Instance.GetCurrentPlayerPosition();
        Instance.LogEvent("Interaction", "ObjectScanned", "Scanned '" + targetName + "' with entry '" + entryName + "'", details, pos);
    }

    public void Save()
    {
        if (session == null)
            return;

        session.endUtc = DateTime.UtcNow.ToString("o");
        session.endLocal = DateTime.Now.ToString("o");
        session.durationSeconds = (float)(DateTime.UtcNow - session.StartUtcAsDateTime).TotalSeconds;

        try
        {
            string json = JsonUtility.ToJson(session, prettyJson);
            File.WriteAllText(outputPath, json);
            saved = true;
            Debug.Log("Telemetry saved to " + outputPath);
        }
        catch (Exception ex)
        {
            Debug.LogError("Telemetry save failed: " + ex.Message);
        }
    }

    private void CreateSession()
    {
        DateTime nowUtc = DateTime.UtcNow;
        string directory = Path.Combine(Application.persistentDataPath, outputFolderName);
        Directory.CreateDirectory(directory);

        outputPath = Path.Combine(directory, "telemetry_" + nowUtc.ToString("yyyyMMdd_HHmmss") + ".json");

        session = new TelemetrySession
        {
            sessionId = Guid.NewGuid().ToString(),
            startUtc = nowUtc.ToString("o"),
            startLocal = DateTime.Now.ToString("o"),
            initialScene = SceneManager.GetActiveScene().name,
            outputPath = outputPath,
            systemInfo = new TelemetrySystemInfo
            {
                unityVersion = Application.unityVersion,
                platform = Application.platform.ToString(),
                isEditor = Application.isEditor,
                targetFrameRate = Application.targetFrameRate,
                operatingSystem = SystemInfo.operatingSystem,
                deviceName = SystemInfo.deviceName,
                deviceModel = SystemInfo.deviceModel,
                deviceType = SystemInfo.deviceType.ToString(),
                processorType = SystemInfo.processorType,
                processorCount = SystemInfo.processorCount,
                systemMemorySizeMb = SystemInfo.systemMemorySize,
                graphicsDeviceName = SystemInfo.graphicsDeviceName,
                graphicsDeviceType = SystemInfo.graphicsDeviceType.ToString(),
                graphicsMemorySizeMb = SystemInfo.graphicsMemorySize
            }
        };
    }

    private void OnSceneChanged(Scene previousScene, Scene currentScene)
    {
        var details = new List<TelemetryKeyValue>
        {
            new TelemetryKeyValue("previousScene", previousScene.name),
            new TelemetryKeyValue("currentScene", currentScene.name)
        };

        LogEvent("Scene", "SceneChanged", "Scene changed from '" + previousScene.name + "' to '" + currentScene.name + "'", details, GetCurrentPlayerPosition());
    }

    private void TrackInput()
    {
        if (!trackInputSystemActions || InputSystem.actions == null)
            return;

        foreach (InputAction action in InputSystem.actions)
        {
            if (action == null || !action.enabled)
                continue;

            string actionName = action.name;
            if (ShouldSkipInputActionName(actionName))
                continue;

            Vector3? pos = GetCurrentPlayerPosition();

            if (action.WasPressedThisFrame())
            {
                var details = new List<TelemetryKeyValue>
                {
                    new TelemetryKeyValue("action", actionName),
                    new TelemetryKeyValue("state", "Pressed")
                };

                LogEvent("Input", "InputPressed", "Pressed input action '" + actionName + "'", details, pos);
            }

            if (action.WasReleasedThisFrame())
            {
                var details = new List<TelemetryKeyValue>
                {
                    new TelemetryKeyValue("action", actionName),
                    new TelemetryKeyValue("state", "Released")
                };

                LogEvent("Input", "InputReleased", "Released input action '" + actionName + "'", details, pos);
            }
        }
    }

    private bool ShouldSkipInputActionName(string actionName)
    {
        return actionName != null && actionName.IndexOf("look", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private Vector3? GetCurrentPlayerPosition()
    {
        TryResolvePlayerTransform();

        if (playerTransform == null)
            return null;

        return playerTransform.position;
    }

    private void TryResolvePlayerTransform()
    {
        if (playerTransform != null)
            return;

        var playerMovement = FindAnyObjectByType<PlayerMovement>();
        if (playerMovement != null)
        {
            playerTransform = playerMovement.transform;
            warnedMissingPlayerTransform = false;
        }
        else if (!warnedMissingPlayerTransform)
        {
            Debug.LogWarning("TelemetryTracking: playerTransform is null, position will be omitted until player is found.");
            warnedMissingPlayerTransform = true;
        }
    }

    private void LogEvent(string category, string eventName, string description, List<TelemetryKeyValue> details, Vector3? worldPosition)
    {
        if (session == null)
            return;

        TelemetryEntry entry = new TelemetryEntry
        {
            utcTimestamp = DateTime.UtcNow.ToString("o"),
            localTimestamp = DateTime.Now.ToString("o"),
            category = category,
            eventName = eventName,
            description = description,
            scene = SceneManager.GetActiveScene().name,
            frame = Time.frameCount,
            realtimeSinceStartup = Time.realtimeSinceStartup,
            details = details ?? new List<TelemetryKeyValue>()
        };

        if (worldPosition.HasValue)
        {
            entry.hasPosition = true;
            entry.position = new TelemetryVector3(worldPosition.Value);
        }

        session.entries.Add(entry);

        if (telemetryUploader != null)
        {
            telemetryUploader.Enqueue(session.sessionId, entry);
        }
    }
}

[Serializable]
public class TelemetrySession
{
    public string sessionId;
    public string startUtc;
    public string startLocal;
    public string endUtc;
    public string endLocal;
    public string initialScene;
    public string outputPath;
    public float durationSeconds;
    public TelemetrySystemInfo systemInfo;
    public List<TelemetryEntry> entries = new List<TelemetryEntry>();

    public DateTime StartUtcAsDateTime
    {
        get
        {
            if (DateTime.TryParse(startUtc, out DateTime parsed))
                return parsed;

            return DateTime.UtcNow;
        }
    }
}

[Serializable]
public class TelemetryEntry
{
    public string utcTimestamp;
    public string localTimestamp;
    public string category;
    public string eventName;
    public string description;
    public string scene;
    public int frame;
    public float realtimeSinceStartup;
    public bool hasPosition;
    public TelemetryVector3 position;
    public List<TelemetryKeyValue> details = new List<TelemetryKeyValue>();
}

[Serializable]
public class TelemetryKeyValue
{
    public string key;
    public string value;

    public TelemetryKeyValue(string key, string value)
    {
        this.key = key;
        this.value = value;
    }
}

[Serializable]
public class TelemetryVector3
{
    public float x;
    public float y;
    public float z;

    public TelemetryVector3(Vector3 value)
    {
        x = value.x;
        y = value.y;
        z = value.z;
    }
}

[Serializable]
public class TelemetrySystemInfo
{
    public string unityVersion;
    public string platform;
    public bool isEditor;
    public int targetFrameRate;
    public string operatingSystem;
    public string deviceName;
    public string deviceModel;
    public string deviceType;
    public string processorType;
    public int processorCount;
    public int systemMemorySizeMb;
    public string graphicsDeviceName;
    public string graphicsDeviceType;
    public int graphicsMemorySizeMb;
}
