using System;
using System.Text;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Networking;
using UnityEngine.SocialPlatforms;
using Rest;
using UnityClient;
using UnityClient.Utils;

namespace Rest
{
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

        public virtual void Start()
        {
            Init();
        }

        protected void Init()
        {
            this.restClient = new RestClient(_baseUri, _clientId, _appId, _apiKey, _apiSecret, serviceName);
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
                                try
                                {
                                    Debug.Log(">>>res.www.text=" + res.www.text);
                                    if (!string.IsNullOrEmpty(res.www.text))
                                    {
                                        T response = JsonUtility.FromJson<T>(res.www.text);
                                        callback(null, response);
                                    }
                                    else
                                    {
                                        callback(new Exception("Response is empty"), null);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Debug.Log(">>>ex=" + ex.Message);
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
}
