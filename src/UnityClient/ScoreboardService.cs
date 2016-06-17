using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SocialPlatforms;

/// <summary>
/// Scoreboard service
/// </summary>
public class ScoreboardService : RestBehaviour
{
    public string ApiEndpoint;

    /// <summary>
    /// Get Top Scores
    /// </summary>
    /// <param name="callback"></param>
    public void GetTop(Action<Exception, TopScores> callback)
    {
        Get<TopScores>("/score/top", callback);
    }

    /// <summary>
    /// Post User Score
    /// </summary>
    /// <param name="score"></param>
    /// <param name="callback"></param>
    public void PostScore(ScoreData score, Action<Exception, ScoreData> callback)
    {
        Post<ScoreData>("/score/", score, callback);
    }
}

// MODELS
[Serializable]
public class TopScores
{
    public string path = "";
    public ScoreData[] scores;
}

[Serializable]
public class ScoreData
{
    public string Leaderboard = "";
    public string Player = "";
    public long Value = 0;
    public string[] Values;
    public long Rank = 0;
    public string Clan = "";
    public string Location = "";
    public string Platform = "";
    public string GameSessionGUID = "";
}

public class Score : IScore
{
    ScoreboardService _rest;

    public ScoreData StoredData { get; private set; }

    public Score(ScoreboardService rest, string player, long value)
    {
        _rest = rest;
        StoredData = new ScoreData();
        StoredData.Player = player;
        StoredData.Value = value;
        StoredData.Platform = Application.platform.ToString().ToUpper();
        StoredData.GameSessionGUID = Guid.NewGuid().ToString().ToUpper();
    }

    public void ReportScore(Action<bool> callback)
    {
        _rest.PostScore(this.StoredData,
            (Exception ex, ScoreData score) =>
            {
                FromRawData(score);
                callback(ex == null);
            });
    }

    public void FromRawData(ScoreData data)
    {
        StoredData = data;
        //this.date = null;
        this.value = data.Value;
        this.rank = rank;
        this.userID = data.Player;
    }

    public string leaderboardID { get; set; }
    public long value { get; set; }
    public string formattedValue { get; private set; }
    public int rank { get; private set; }
    public string userID { get; private set; }
    public DateTime date { get; private set; }
}
