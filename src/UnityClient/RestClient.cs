using System;
using System.Text;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Networking;
using UnityEngine.SocialPlatforms;
using rest;
using UnityClient;

/// <summary>
/// Basic functionality for REST services
/// </summary>
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


// REST
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

        /// <summary>
        /// Parse status header and return status code
        /// </summary>
        /// <param name="www">WWW object</param>
        /// <returns>Status Code of response</returns>
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

            // TODO: check whether GET with headers supported or not
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
            // TODO: should range be deleted?
            signatureBuilder.AddHeader("range", "");
            signatureBuilder.AddHeader("x-date", DateTime.UtcNow.ToString("o"));

            return signatureBuilder.CreateSignature();
        }
    }
}
