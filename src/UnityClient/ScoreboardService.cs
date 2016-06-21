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
    void OnStart()
    {
        base.serviceName = "scoreboard";
        base.Init();
    }

    /// <summary>
    /// Get Top Scores
    /// </summary>
    /// <param name="callback">Callback method with Exception and TopScores args</param>
    public void GetTop(Action<Exception, TopScores> callback)
    {
        Get<TopScores>("/score/top", callback);
    }

    /// <summary>
    /// Post User Score
    /// </summary>
    /// <param name="score">Score data</param>
    /// <param name="callback">Callback method with Exception and ScoreData args</param>
    public void PostScore(ScoreData score, Action<Exception, ScoreData> callback)
    {
        Post<ScoreData>("/score/", score, callback);
    }
}
