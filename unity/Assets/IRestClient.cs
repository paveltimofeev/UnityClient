using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Rest
{
    public interface IRestClient
    {
        IEnumerator Get(string path, Action<Response> callback, float delay = 0);
        IEnumerator Post(string path, string jsonBody, Action<Response> callback, float delay = 0);
        WWW request(string uri, byte[] body, Dictionary<string, string> headers);
    }


    public interface IWWWWrapper
    {
        void request(string uri, byte[] body, Dictionary<string, string> headers, Action<string> callback);
    }

    public class WWWWrapper : MonoBehaviour, IWWWWrapper
    {
        string uri;
        byte[] body;
        Dictionary<string, string> headers;
        Action<string> callback;

        IEnumerator _request()
        {
            WWW www = new WWW(uri, body, headers);

            while (!www.isDone)
            {
                yield return new WaitForEndOfFrame();
            }

            callback(www.text);
        }

        public void request(string uri, byte[] body, Dictionary<string, string> headers, Action<string> callback)
        {
            this.uri = uri;
            this.body = body;
            this.headers = headers;
            this.callback = callback;

            StartCoroutine(this._request());
        }
    }
}
