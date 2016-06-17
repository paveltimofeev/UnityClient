using System;
using System.Text;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Networking;
using UnityEngine.SocialPlatforms;
using rest;

/// <summary>
/// Basic functionality for REST services
/// </summary>
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
