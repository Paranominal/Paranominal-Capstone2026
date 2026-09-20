using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

public class LeaderboardManager : MonoBehaviour
{
    public static LeaderboardManager Instance { get; private set; }

    [Header("Config")]
    [SerializeField] private int maxEntries = 10;
    [SerializeField] private string playerPrefsKey = "Leaderboard_Entries";

    [Header("Debug")]
    [SerializeField] private bool debugMode = true;

    private LeaderboardData data = new LeaderboardData();

    //used by addEntry to highlight the latest run
    public LeaderboardEntry LastAddedEntry { get; private set; }

    private void Awake()
    {
        //destroy duped instances
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        //load local records
        Load();
    }

    //retrieves read only list of entries in desc order
    public IReadOnlyList<LeaderboardEntry> GetTopEntries()
    {
        return data.entries.OrderByDescending(e => e.score).ToList();
    }

    //adds new entry
    public LeaderboardEntry AddEntry(string playerName, int score, string rank)
    {
        if (string.IsNullOrWhiteSpace(playerName))
        {
            playerName = "Player";
        }

        LeaderboardEntry entry = new LeaderboardEntry(playerName.Trim(), score, rank);
        //insert at the start to keep the latest run at the top
        data.entries.Insert(0, entry); 

        //keep only the highest entries in descending order
        data.entries = data.entries.OrderByDescending(e => e.score).Take(maxEntries).ToList();

        LastAddedEntry = entry;

        if (debugMode) Debug.Log($"Leaderboard: added {entry.playerName}, {entry.score}, ({entry.rank})");

        Save();
        return entry;
    }

    //save to local json
    private void Save()
    {
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(playerPrefsKey, json);
        PlayerPrefs.Save();
    }

    //loads json into memory
    private void Load()
    {
        if (!PlayerPrefs.HasKey(playerPrefsKey))
        {
            data = new LeaderboardData();
            return;
        }

        string json = PlayerPrefs.GetString(playerPrefsKey);
        data = JsonUtility.FromJson<LeaderboardData>(json) ?? new LeaderboardData();
    }

    //sets the data to a new state
    public void ClearLeaderboard()
    {
        data = new LeaderboardData();
        Save();
    }
}

