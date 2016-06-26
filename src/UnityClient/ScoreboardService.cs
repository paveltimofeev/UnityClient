using System;
using System.Collections.Generic;
using System.Text;
using Rest;
using UnityEngine;
using UnityEngine.SocialPlatforms;

/// <summary>
/// Scoreboard service
/// </summary>
public class ScoreboardService : RestBehaviour
{
    public override void Start()
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
        Get<TopScores>("/v1/scoreboard/score/10", callback);
    }

    /// <summary>
    /// Post User Score
    /// </summary>
    /// <param name="score">Score data</param>
    /// <param name="callback">Callback method with Exception and ScoreData args</param>
    public void PostScore(ScoreData score, Action<Exception, ScoreData> callback)
    {
        Post<ScoreData>("/v1/scoreboard/score", score, callback);
    }
}
