using System;

[Serializable]
public class LeaderboardEntry
{
    public string playerName;
    public int score;
    public string rank;

    //entries contain the name, score, and rank of the session
    public LeaderboardEntry(string playerName, int score, string rank)
    {
        this.playerName = playerName;
        this.score = score;
        this.rank = rank;
    }
}