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
    public class RestClient
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
            
            // TODO: check whether GET with headers supported or not
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
            signatureBuilder.AddHeader("host", host);
            // TODO: should range be deleted?
            signatureBuilder.AddHeader("range", "");
            signatureBuilder.AddHeader("x-date", DateTime.UtcNow.ToString("o"));
            // Debug mode
            signatureBuilder.AddHeader("x-debug", "true");
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
 
    public static class Method
    {
        public const string GET = "GET";
        public const string POST = "POST";
    }
}
