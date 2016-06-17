
/// © Pavel Timofeev 

using rest;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;
using System;
using UnityEngine.Experimental.Networking;
using UnityEngine.SocialPlatforms;
using UnityEngine;

public class RestBehaviour : MonoBehaviour
{
    public string _baseUri;
    public string _appId;
    public string _apiKey;
    public string _apiSecret;

    RestClient restClient = new RestClient();
    HttpRetryPolicy retryPolicy = new HttpRetryPolicy(3);

    private void RetryableGet<T>(string uri, Action<Exception, T> callback, int attempt = 0)
        where T : class
    {
        if (attempt > 0)
            Debug.LogWarning("Retry: " + attempt);

        StartCoroutine(
                restClient.Get(
                    _baseUri + uri,
                    (Response res) =>
                    {
                        if (retryPolicy.ShouldRetry(attempt, res.StatusCode))
                        {
                            RetryableGet<T>(uri, callback, attempt + 1);
                        }
                        else if (callback != null)
                        {
                            Debug.Log(res.www.text);
                            try
                            {
                                callback(null, JsonUtility.FromJson<T>(res.www.text));
                            }
                            catch (Exception ex)
                            {
                                if (retryPolicy.ShouldRetry(attempt))
                                    RetryableGet<T>(uri, callback, attempt + 1);
                                else
                                    callback(ex, null);
                            }
                        }
                    },
                    retryPolicy.GetRetryDelay(attempt))
                );
    }

    private void RetryablePost<T>(string uri, T data, Action<Exception, T> callback, int attempt = 0)
        where T : class
    {
        if (attempt > 0)
            Debug.LogWarning("Retry: " + attempt);

        StartCoroutine(
            restClient.Post(
                _baseUri + uri,
                JsonUtility.ToJson(data, false),
                (Response res) =>
                {
                    if (retryPolicy.ShouldRetry(attempt, res.StatusCode))
                    {
                        RetryableGet<T>(uri, callback, attempt + 1);
                    }
                    else if (callback != null)
                    {
                        try
                        {
                            callback(null, JsonUtility.FromJson<T>(res.www.text));
                        }
                        catch (Exception ex)
                        {
                            if (retryPolicy.ShouldRetry(attempt))
                                RetryableGet<T>(uri, callback, attempt + 1);
                            else
                                callback(ex, null);
                        }
                    }
                },
                retryPolicy.GetRetryDelay(attempt))
            );
    }

    public void Get<T>(string uri, Action<Exception, T> callback)
        where T : class
    {
        RetryableGet<T>(uri, callback, 0);
    }

    public void Post<T>(string uri, T data, Action<Exception, T> callback)
        where T : class
    {
        RetryablePost<T>(uri, data, callback, 0);
    }
}


namespace rest
{
    public class HttpRetryPolicy
    {
        System.Random random = new System.Random();
        const float delayRate = 0.25f; // first retry after ~0.75s, second after ~2.5s
        int maxRetries = 3;

        List<int> retryStatuses = new List<int>(new int[] 
        { 408, 409, 423, 449, 502, 503, 504 });

        public HttpRetryPolicy(int maxRetries)
        {
            this.maxRetries = maxRetries;
        }

        public float GetRetryDelay(int attemptNumber)
        {
            return attemptNumber 
                * (float)random.Next((int)Math.Pow(2, attemptNumber), (int)Math.Pow(2, attemptNumber + 1)) 
                * delayRate;
        }
        public bool ShouldRetry(int attemptNumber)
        {
            return attemptNumber < maxRetries - 1;
        }

        public bool ShouldRetry(int attemptNumber, int responseStatusCode)
        {
            return ShouldRetry(attemptNumber) && retryStatuses.Contains(responseStatusCode);
        }
    }

    public class Response
    {
        private const string STATUS_HEADERNAME = "STATUS";

        public WWW www { get; private set; }
        public int StatusCode { get; private set; }

        public Response(WWW www)
        {
            if (www == null)
                throw new ArgumentNullException("www");

            this.www = www;
            this.StatusCode = GetStatus(www);
        }

        private int GetStatus(WWW www)
        {
            int result = -1;
            string header = string.Empty;
            if (IsValidAndHasResponse(www) && 
                www.responseHeaders.TryGetValue(STATUS_HEADERNAME, out header))
            {
                string[] parts = header.Split(' '); /// HTTP/1.1 200 OK
                if (parts.Length > 1)
                    int.TryParse(parts[1], out result);
            }

            return result;
        }

        private bool IsValidAndHasResponse(WWW www)
        {
            return www != null && www.isDone && www.responseHeaders != null;
        }
    }

    public class RestClient
    {
        public IEnumerator Get(string url, Action<Response> callback, float delay = 0)
        {
            if (delay > 0)
            {
                Debug.LogWarning("Delay for " + delay + " sec");
                yield return new WaitForSeconds(delay);
            }

            WWW www = new WWW(url);
            
            while (!www.isDone)
            {
                yield return new WaitForEndOfFrame();
            }

            if (callback != null)
                callback(new Response(www));
        }

        public IEnumerator Post(string url, string jsonBody, Action<Response> callback, float delay = 0)
        {
            if (delay > 0)
            {
                Debug.LogWarning("Delay for " + delay + " sec");
                yield return new WaitForSeconds(delay);
            }

            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("Content-Type", "application/json");

            WWW www = new WWW(url, Encoding.Default.GetBytes(jsonBody), headers);

            while (!www.isDone)
            {
                yield return new WaitForEndOfFrame();
            }

            if (callback != null)
                callback(new Response(www));
        }
    }
}

public class ScoreboardService : RestBehaviour
{
    public string ApiEndpoint;

    public void GetTop(Action<Exception, TopScores> callback)
    {
        Get<TopScores>("/score/top", callback);
    }

    public void PostScore(ScoreData score, Action<Exception, ScoreData> callback)
    {
        Post<ScoreData>("/score/", score, callback);
    }
}

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

namespace UnityClient
{

    public class SignatureBuilder
    {
        private const string Algorithm = "PGS1";
        private readonly string APPID = string.Empty;
        private readonly string APIKEY = string.Empty;
        private readonly string APISECRET = string.Empty;

        private const string HeaderFormat = "{0} Credentials={1} SignedHeaders={2} Signature={3}";
        private const string CredentialsFormat = "{0}/{1}/{2}";

        private string Signature;
        
        private string svcname;
        private string uri;
        private string query;
        private string headers;
        private string list_headers;
        private string body;
        private string bodyHash;
        private string hash;

        
        public SignatureBuilder(string APPID, string APIKEY, string APISECRET)
        {
            ThrowIfNull(APPID , "APPID");
            ThrowIfNull(APIKEY , "APIKEY");
            ThrowIfNull(APISECRET, "APISECRET");

            this.APPID = APPID;
            this.APIKEY = APIKEY;
            this.APISECRET = APISECRET;
        }

        public void AddHeader(string name, string value)
        {
            ThrowIfNull(name , "name");
            ThrowIfNull(value , "value");

            list_headers += string.Format("{0};", name);
            headers += Regex.Replace(value.ToLowerInvariant().Trim(), @"\s+", " ");
        }

        public void AddService(string name)
        {
            ThrowIfNull(name, "name");
            svcname = name;
        }

        public void AddUrl(string url)
        {
            ThrowIfNull(url, "url");

            if (url.Contains("://"))
                throw new ArgumentException("url argument should not contains protocol part", url);

            if (url.Contains("."))
                throw new ArgumentException("url argument should not contains domain part", url);

            var pieces = url.Split('?');

            if (pieces.Length > 2)
                throw new ArgumentException("url argument cannot have more than one query part", url);

            uri = pieces[0];
            query = pieces.Length > 0 ? pieces[1] : "";
        }

        public string canonicalUri
        {
            get
            {
                ThrowIfNull(uri, "uri"); // TODO: really need?
                return uri.ToLowerInvariant().Trim();
            }
        }

        public string canonicalQuery
        {
            get
            {
                if (query == null) // TODO: really need?
                    throw new ArgumentNullException("query");

                string[] queryParam = query.Split('&');
                Array.Sort<string>(queryParam);
                return string.Join("&", queryParam);
            }
        }

        public string signedHeaders
        {
            get
            {
                if (list_headers != null)
                    return list_headers.TrimEnd(new char[] { ';' });
                else
                    return ""; // TODO: really need?
            }
        }


        public string CreateSignature()
        {
            string Credentials = string.Format(CredentialsFormat, APIKEY, "YYYYMMDD", svcname);
            return string.Format(HeaderFormat, Algorithm, Credentials, signedHeaders, Signature);
        }


        private void ThrowIfNull(string value, string argName)
        {
            if (value == null || value == string.Empty || value == "")
                throw new ArgumentNullException(argName);
        }
    }
}
