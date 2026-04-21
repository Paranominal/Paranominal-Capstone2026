using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class TelemetryUploader : MonoBehaviour
{
    [Header("Supabase")]
    [SerializeField] private string supabaseUrl = "https://YOUR_PROJECT_REF.supabase.co";
    [SerializeField] private string supabaseAnonKey = "YOUR_ANON_KEY";
    [SerializeField] private string tableName = "telemetry_events";

    [Header("Upload")]
    [SerializeField] private float flushIntervalSeconds = 1.0f;
    [SerializeField] private int maxBatchSize = 25;

    private readonly List<TelemetryUploadRow> queue = new List<TelemetryUploadRow>();
    private string endpoint;
    private Coroutine flushLoop;

    private void Awake()
    {
        endpoint = supabaseUrl.TrimEnd('/') + "/rest/v1/" + tableName;
        Debug.Log("TelemetryUploader endpoint: " + endpoint);
    }

    private void OnEnable()
    {
        flushLoop = StartCoroutine(FlushLoop());
    }

    private void OnDisable()
    {
        if (flushLoop != null) StopCoroutine(flushLoop);
    }

    public void Enqueue(string sessionId, TelemetryEntry entry)
    {
        if (entry == null || string.IsNullOrWhiteSpace(sessionId))
            return;

        var row = new TelemetryUploadRow
        {
            session_id = sessionId,
            utc_timestamp = entry.utcTimestamp,
            local_timestamp = entry.localTimestamp,
            category = entry.category,
            event_name = entry.eventName,
            description = entry.description,
            scene = entry.scene,
            frame = entry.frame,
            realtime_since_startup = entry.realtimeSinceStartup,
            has_position = entry.hasPosition,
            position_x = entry.hasPosition && entry.position != null ? entry.position.x : 0f,
            position_y = entry.hasPosition && entry.position != null ? entry.position.y : 0f,
            position_z = entry.hasPosition && entry.position != null ? entry.position.z : 0f,
            details_json = JsonUtility.ToJson(new TelemetryDetailsWrapper { details = entry.details ?? new List<TelemetryKeyValue>() })
        };

        queue.Add(row);
        Debug.Log("TelemetryUploader queued row. Queue size: " + queue.Count);
    }

    private IEnumerator FlushLoop()
    {
        var wait = new WaitForSeconds(flushIntervalSeconds);

        while (true)
        {
            if (queue.Count > 0)
            {
                int take = Mathf.Min(maxBatchSize, queue.Count);
                var batch = queue.GetRange(0, take);
                yield return StartCoroutine(PostBatch(batch));
            }

            yield return wait;
        }
    }

    private IEnumerator PostBatch(List<TelemetryUploadRow> batch)
    {
        var payload = new TelemetryUploadRowArray { rows = batch };
        string body = JsonHelper.ToJson(payload.rows);

        using (var req = new UnityWebRequest(endpoint, UnityWebRequest.kHttpVerbPOST))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(body);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();

            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("apikey", supabaseAnonKey);
            req.SetRequestHeader("Authorization", "Bearer " + supabaseAnonKey);
            req.SetRequestHeader("Prefer", "return=representation");

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Telemetry upload success: " + req.responseCode + " " + req.downloadHandler.text);
                queue.RemoveRange(0, batch.Count);
            }
            else
            {
                Debug.LogWarning("Telemetry upload failed: " + req.responseCode + " " + req.error + " " + req.downloadHandler.text);
            }
        }
    }

    [Serializable]
    private class TelemetryDetailsWrapper
    {
        public List<TelemetryKeyValue> details;
    }

    [Serializable]
    private class TelemetryUploadRow
    {
        public string session_id;
        public string utc_timestamp;
        public string local_timestamp;
        public string category;
        public string event_name;
        public string description;
        public string scene;
        public int frame;
        public float realtime_since_startup;
        public bool has_position;
        public float position_x;
        public float position_y;
        public float position_z;
        public string details_json;
    }

    [Serializable]
    private class TelemetryUploadRowArray
    {
        public List<TelemetryUploadRow> rows;
    }

    private static class JsonHelper
    {
        [Serializable]
        private class Wrapper<T> { public List<T> Items; }

        public static string ToJson<T>(List<T> array)
        {
            var wrapper = new Wrapper<T> { Items = array };
            string wrapped = JsonUtility.ToJson(wrapper);
            return wrapped.Replace("{\"Items\":", "").TrimEnd('}');
        }
    }
}