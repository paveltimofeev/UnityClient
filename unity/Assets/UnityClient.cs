///
/// © Pavel Timofeev
/// Changed at 2016-06-30T20:20:48

using Rest;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Text;
using System;
using UnityClient.Utils;
using UnityClient;
using UnityEngine.Experimental.Networking;
using UnityEngine.SocialPlatforms;
using UnityEngine;
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
                    return Convert.ToBase64String(hashBytes);//.TrimEnd('='); // TODO: trim is not needed
                }
            }
        }
        public string Credentials
        {
            get
            {
                return string.Format(CredentialsFormat, APIKEY, "20160619", svcname);
            }
        }
        public string CreateSignature()
        {
            return string.Format(HeaderFormat, Algorithm, Credentials, signedHeaders, Signature);
        }
        public void DebugInfo()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("--------------------------");
            sb.AppendFormat("  canonicalUri          = {0}\r\n" , canonicalUri);
            sb.AppendFormat("  canonicalQuery        = {0}\r\n" , canonicalQuery);
            sb.AppendFormat("  canonicalHeaders      = {0}\r\n" , canonicalHeaders);
            sb.AppendFormat("  payloadHash           = {0}\r\n" , payloadHash);
            sb.AppendFormat("  canonicalRequest      = {0}\r\n" , canonicalRequest);
            sb.AppendFormat("  HashedCanonicalRequest= {0}\r\n" , HashedCanonicalRequest);
            sb.AppendFormat("  Algorithm             = {0}\r\n" , Algorithm);
            sb.AppendFormat("  RequestDate           = {0}\r\n" , RequestDate);
            sb.AppendFormat("  CredentialScope       = {0}\r\n" , CredentialScope);
            sb.AppendFormat("  Credentials           = {0}\r\n" , Credentials);
            sb.AppendFormat("  signedHeaders         = {0}\r\n" , signedHeaders);
            sb.AppendFormat("  stringToSign          = {0}\r\n" , stringToSign);
            sb.AppendFormat("  Signature             = {0}\r\n" , Signature);
            sb.AppendLine("--------------------------");
            Debug.Log(sb.ToString());
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
}
namespace UnityClient.Utils
{
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
namespace Rest
{
    public class RestClient : IRestClient
    {
        private readonly string _clientId;
        private readonly string _appId;
        private readonly string _apiKey;
        private readonly string _apiSecret;
        private readonly string host;
        private readonly string service;
        private readonly Encoding bodyEncoding = Encoding.Default;
        public RestClient(string host, string clientId, string appId, string apiKey, string apiSecret, string service)
        {
            ThrowIf.Null(host, "host");
            ThrowIf.Null(clientId, "clientId");
            ThrowIf.Null(appId, "appId");
            ThrowIf.Null(apiSecret, "apiSecret");
            ThrowIf.Null(service, "service");
            this._clientId = clientId;
            this._appId = appId;
            this._apiKey = apiKey;
            this._apiSecret = apiSecret;
            this.host = host;
            this.service = service;
        }
        
        public WWW request(string uri, byte[] body, Dictionary<string, string> headers)
        {
            Debug.Log("request: " + uri);
            return new WWW(uri, body, headers);
        }

        public IEnumerator Get(string path, Action<Response> callback, float delay = 0)
        {
            if (delay > 0)
            {
                Debug.LogWarning("Delay for " + delay + " sec");
                yield return new WaitForSeconds(delay);
            }
            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("Authorization", GetAuthorizationHeader(Method.GET, path, null));
            headers.Add("x-debug", "true");
            WWW www = request(host + path, null, headers);
            while (!www.isDone)
            {
                yield return new WaitForEndOfFrame();
            }
            if (callback != null)
                callback(new Response(www));
        }
        public IEnumerator Post(string path, string jsonBody, Action<Response> callback, float delay = 0)
        {
            if (delay > 0)
            {
                Debug.LogWarning("Delay for " + delay + " sec");
                yield return new WaitForSeconds(delay);
            }
            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("Authorization", GetAuthorizationHeader(Method.POST, path, jsonBody));
            headers.Add("Content-Type", "application/json");
            headers.Add("x-debug", "true");
            WWW www = request(host + path, bodyEncoding.GetBytes(jsonBody), headers);
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
            signatureBuilder.AddHeader("host", host.Replace("https://", "").Replace("http://", ""));
            signatureBuilder.DebugInfo();
            return signatureBuilder.CreateSignature();
        }
    }
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
    public static class Method
    {
        public const string GET = "GET";
        public const string POST = "POST";
    }
}
