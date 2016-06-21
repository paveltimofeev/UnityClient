///
/// © Pavel Timofeev
/// Changed at 2016-06-21T22:16:54

using rest;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Text;
using System;
using UnityClient;
using UnityEngine.Experimental.Networking;
using UnityEngine.SocialPlatforms;
using UnityEngine;
public class RestBehaviour : MonoBehaviour
{
    public string _baseUri;
    public string _clientId;
    public string _appId;
    public string _apiKey;
    public string _apiSecret;
    protected string serviceName = "baseService";
    protected RestClient restClient;
    protected HttpRetryPolicy retryPolicy = new HttpRetryPolicy(3);
    protected void Init()
    {
        RestClient restClient = new RestClient(_baseUri, _clientId, _appId, _apiKey, _apiSecret, serviceName);
    }
    void OnStart()
    {
        Init();
    }
    private void RetryableGet<T>(string uri, Action<Exception, T> callback, int attempt = 0)
        where T : class
    {
        if (attempt > 0)
            Debug.LogWarning("Retry: " + attempt);
        StartCoroutine(
                restClient.Get(
                    uri,
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
                uri,
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
        private readonly string _baseUri;
        private readonly string _clientId;
        private readonly string _appId;
        private readonly string _apiKey;
        private readonly string _apiSecret;
        private string host;
        private string service;
        private readonly Encoding bodyEncoding = Encoding.Default;
        public RestClient(string host, string clientId, string appId, string apiKey, string apiSecret, string service)
        {
            ThrowIf.Null(host, "host");
            ThrowIf.Null(clientId, "clientId");
            ThrowIf.Null(appId, "appId");
            ThrowIf.Null(apiSecret, "apiSecret");
            ThrowIf.Null(service, "service");
            this._baseUri = host;
            this._clientId = clientId;
            this._appId = appId;
            this._apiKey = apiKey;
            this._apiSecret = apiSecret;
            this.host = host;
            this.service = service;
        }
        public IEnumerator Get(string url, Action<Response> callback, float delay = 0)
        {
            if (delay > 0)
            {
                Debug.LogWarning("Delay for " + delay + " sec");
                yield return new WaitForSeconds(delay);
            }
            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("Authorization", GetAuthorizationHeader(Method.GET, url, null));
            WWW www = new WWW(host + url, bodyEncoding.GetBytes(""), headers);
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
            headers.Add("Authorization", GetAuthorizationHeader(Method.POST, url, jsonBody));
            headers.Add("Content-Type", "application/json");
            WWW www = new WWW(host + url, bodyEncoding.GetBytes(jsonBody), headers);
            while (!www.isDone)
            {
                yield return new WaitForEndOfFrame();
            }
            if (callback != null)
                callback(new Response(www));
        }
        private string GetAuthorizationHeader(string method, string url, string body)
        {
            var signatureBuilder = new SignatureBuilder(_clientId, _appId, _apiKey, _apiSecret);
            signatureBuilder.AddUrl(method, url);
            signatureBuilder.AddBody(body);
            signatureBuilder.AddService(service);
            signatureBuilder.AddHeader("host", host);
            signatureBuilder.AddHeader("range", "");
            signatureBuilder.AddHeader("x-date", DateTime.UtcNow.ToString("o"));
            return signatureBuilder.CreateSignature();
        }
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
        private const string Algorithm = "AWS4-HMAC-SHA256";
        private readonly string CLIENTID = string.Empty;
        private readonly string APPID = string.Empty;
        private readonly string APIKEY = string.Empty;
        private readonly string APISECRET = string.Empty;
        private const string HeaderFormat = "{0} Credentials={1} SignedHeaders={2} Signature={3}";
        private const string CredentialsFormat = "{0}/{1}/{2}";
        private string svcname;
        private string method;
        private string uri;
        private string query;
        private Dictionary<string, string> headers = new Dictionary<string,string>();
        private string list_headers;
        private Encoding enc = Encoding.UTF8;
        public SignatureBuilder(string CLIENTID, string APPID, string APIKEY, string APISECRET)
        {
            ThrowIf.Null(CLIENTID, "CLIENTID");
            ThrowIf.Null(APPID, "APPID");
            ThrowIf.Null(APIKEY , "APIKEY");
            ThrowIf.Null(APISECRET, "APISECRET");
            this.CLIENTID = CLIENTID;
            this.APPID = APPID;
            this.APIKEY = APIKEY;
            this.APISECRET = APISECRET;
        }
        public void AddHeader(string name, string value)
        {
            ThrowIf.NullOnly(name, "name");
            ThrowIf.NullOnly(value , "value");
            list_headers += string.Format("{0};", name);
            headers.Add(name, Regex.Replace(value.ToLowerInvariant().Trim(), @"\s+", " "));
        }
        public void AddService(string name)
        {
            ThrowIf.Null(name, "name");
            svcname = name;
        }
        public void AddUrl(string method, string url)
        {
            ThrowIf.Null(method, "method");
            if (url == null || url == string.Empty)
                url = "";
            if (url.Contains("://"))
                throw new ArgumentException("url argument should not contains protocol part", url);
            if (url.Contains("."))
                throw new ArgumentException("url argument should not contains domain part", url);
            this.method = method;
            var pieces = url.Split('?');
            if (pieces.Length > 2)
                throw new ArgumentException("url argument cannot have more than one query part", url);
            uri = pieces[0];
            query = pieces.Length > 1 ? pieces[1] : "";
        }
        public void AddBody(string body)
        {
            this.payloadHash = GetHash(body != null ? body : "");
        }
        public string canonicalUri
        {
            get
            {
                return uri != null ? uri.ToLowerInvariant().Trim() : "";
            }
        }
        public string canonicalQuery
        {
            get
            {
                if (query == null)
                    return "";
                string[] queryParam = query.Split('&');
                Array.Sort<string>(queryParam);
                return string.Join("&", queryParam);
            }
        }
        public string canonicalHeaders
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                headers.Any(x => sb.AppendFormat("{0}:{1}\n", x.Key, x.Value) == null);
                return sb.ToString()+'\n';
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
        public string payloadHash { get; private set; }
        public string canonicalRequest
        {
            get
            {
                return string.Format("{0}\n{1}\n{2}\n{3}\n{4}\n{5}",
                    method, canonicalUri, canonicalQuery, canonicalHeaders, signedHeaders, payloadHash);
            }
        }
        public string HashedCanonicalRequest
        {
            get
            {
                return GetHash(canonicalRequest.ToLower());
            }
        }
        public string RequestDate
        {
            get
            {
                return "";
            }
        }
        public string CredentialScope
        {
            get
            {
                return string.Format("{0}/{1}", "20160619", svcname);
            }
        }
        public string stringToSign
        {
            get
            {
                return string.Format("{0}\n{1}\n{2}\n{3}", 
                    Algorithm, RequestDate, CredentialScope, HashedCanonicalRequest);
            }
        }
        public string Signature
        {
            get
            {
                ASCIIEncoding ascii = new System.Text.ASCIIEncoding();
                var keyByte = ascii.GetBytes(APISECRET);
                var messageBytes = ascii.GetBytes(stringToSign);
                using (var hmac = new HMACSHA256(keyByte))
                {
                    var hashBytes = hmac.ComputeHash(messageBytes);
                    return Convert.ToBase64String(hashBytes).TrimEnd('='); // TODO: trim is not needed
                }
            }
        }
        public string CreateSignature()
        {
            string Credentials = string.Format(CredentialsFormat, APIKEY, "20160619", svcname);
            return string.Format(HeaderFormat, Algorithm, Credentials, signedHeaders, Signature);
        }
        private string GetHash(string data)
        {
            StringBuilder sb = new StringBuilder();
            using (SHA256 hash = SHA256Managed.Create())
            {
                Byte[] result = hash.ComputeHash(enc.GetBytes(data));
                foreach (Byte b in result)
                {
                    sb.Append(b.ToString("x2"));
                }
            }
            return sb.ToString();
        }
    }
    public static class Method
    {
        public const string GET = "GET";
        public const string POST = "POST";
    }
    public class ThrowIf
    {
        public static void Null(string value, string argName)
        {
            if (value == null || value == string.Empty || value == "")
                throw new ArgumentNullException(argName);
        }
        public static void NullOnly(object value, string argName)
        {
            if (value == null)
                throw new ArgumentNullException(argName);
        }
    }
}
