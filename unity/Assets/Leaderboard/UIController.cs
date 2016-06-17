using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;
using UnityEngine.EventSystems;

[RequireComponent(typeof(ScoreboardService))]
public class UIController : MonoBehaviour {

    ScoreboardService scoreSvc;

	void Start () 
    {
        scoreSvc = GetComponent<ScoreboardService>();
	}
	
    /// Buttons handlers
    
    public void Post()
    {
        Debug.Log("Post");   

        var playerName = getInputField("EnterPlayerName", "Unknown player");
        var score = getInputField("EnterScore", "0");
        var playerGUID = getInputField("PlayerGUID", Guid.Empty.ToString());
        bool autoUpdate = getToggle("AutoUpdateAfterPost", false);
        
        /// TODO:
        addLogs("Posting score of '{0}' (GUID {2}) equals '{1}'", playerName, score, playerGUID);
        
        Score gameScore = new Score(scoreSvc, playerName, long.Parse(score));

        gameScore.ReportScore((bool result) => 
        {
            if (result && autoUpdate)
                UpdateLeaderboards();        
        });
    }
    
    public void Random()
    {
        Debug.Log("Random");
        setInputField("EnterPlayerName", "Player " + (new System.Random()).Next(0, 10));
        setInputField("EnterScore", (new System.Random()).Next(0, 100).ToString());
    }
    
    public void New()
    {
        Debug.Log("New");
        setInputField("PlayerGUID", Guid.NewGuid().ToString().ToUpper());        
    }
    
    public void Save()
    {
        Debug.Log("Save");   
    }

    public void UpdateLeaderboards()
    {
        Debug.Log("UpdateLeaderboards");
        addLogs("Updating leaderboards info");

        scoreSvc.GetTop((Exception ex, TopScores scores) =>
        {
            setText("LeaderboardTitle", scores.path);
            var row = Resources.Load("LeaderRow");

            findGo("LeaderboardContent", (GameObject go) => {
        
                for (int i = 0; i < scores.scores.Length; i++)
                {
                    addLogs("Loaded '{0}'", scores.scores[i].Player);

                    var newRow = Instantiate(row) as GameObject;
                    newRow.name = "raw" + i.ToString();
                    newRow.transform.SetParent(go.transform);
                }
            });
        });
    }
    
    public void Clear()
    {
        Debug.Log("Clear");
        setText("Output", "");
    }


    void addLogs(string format, params object[] args)
    {
        addLogs(string.Format(format, args));
    }
    void addLogs(string record)
    {
        find<Text>("Output", (Text comp) =>
        {

            comp.text = DateTime.Now.ToLongTimeString() + " " + record + "\r\n" + comp.text;
        });
    }    

    /// Utils
    
    void findGo(string go, Action<GameObject> action)
    {
        var foundGo = GameObject.Find(go);
        if(foundGo != null)
        {
            action.Invoke(foundGo);
        }
        else
        {
            Debug.Log(go + " gameobject not found");
        }
    }

    void findComp<T>(GameObject go, Action<T> action)
        where T : UIBehaviour
    {
        var comp = go.GetComponent<T>();

        if (comp != null)
            action.Invoke(comp);
        else
            Debug.Log(go + " has not Text");
    }

    void find<T>(string go, Action<T> action)
        where T : UIBehaviour
    {
        findGo(go, (GameObject found) =>
        {
            findComp<T>(found, (T comp) =>
            {
                action(comp);
            });
        });
    }

    bool getToggle(string go, bool def)
    {
        find<Toggle>(go, (Toggle toggle) =>
        {
            def = toggle.isOn;
        });

        return def;
    }

    string getInputField(string go, string def)
    {
        find<InputField>(go, (InputField input) =>
        {
            def = string.IsNullOrEmpty(input.text) ? def : input.text;
        });

        return def;
    }
    
    void setInputField(string go, string text)
    {
        find<InputField>(go, (InputField input) =>
        {
            input.text = text;
        });
    }

    void setText(string go, string text)
    {
        find<Text>(go, (Text c) =>
        {
            c.text = text;
        });
    }
}
