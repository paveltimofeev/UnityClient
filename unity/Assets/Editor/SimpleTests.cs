using UnityEngine;
using UnityEditor;
using NUnit.Framework;
using System;
using Rest;
using SmallTests;
using NSubstitute;
using System.Collections.Generic;

namespace BigTests
{
    public class SimpleTests
    {
        private string URL_GET_SCORE_0 = "/v1/scoreboard/score/{0}";
        private string URL_POST_SCORE = "/v1/scoreboard/score";

        [Test]
        [Category("BigTests")]
        [MaxTime(10000)]
        public void StrigthforwardCallShouldReturn_AuthRequired()
        {
            WWW www = new WWW(DEV.BASEURI + string.Format(URL_GET_SCORE_0, 10));

            while (!www.isDone) { ;}

            Assert.IsTrue(www.text
                        .ToLowerInvariant()
                        .Contains("authorization header required"),
                        www.text);
        }

        [Test]
        [Category("BigTests")]
        [MaxTime(10000)]
        public void RestClient_GetShouldReturn_WrongSignature()
        {
            RestClient client = new RestClient(
                DEV.BASEURI, DEV.CLIENTID, DEV.APPID, DEV.APIKEY, DEV.APISECRET, DEV.SERVICE);

            Response response = null;

            var coroutine = client.Get(string.Format(URL_GET_SCORE_0, 10),
                (Response res) =>
                {
                    response = res;
                });

            while (coroutine.MoveNext()) { ;}

            Assert.AreEqual(401, response.StatusCode);
            Assert.IsTrue(response.www.error.ToLowerInvariant().Contains("401 unauthorized"), response.www.error);
            Assert.IsTrue(response.www.text.ToLowerInvariant().Contains("wrong signature"), response.www.text);
        }

        [Test]
        [Category("BigTests")]
        [MaxTime(10000)]
        public void RestClient_GetShouldReturn_200()
        {
            RestClient client = new RestClient(
                DEV.BASEURI, DEV.CLIENTID, DEV.APPID, DEV.APIKEY, DEV.APISECRET, DEV.SERVICE);

            Response response = null;

            var coroutine = client.Get(string.Format(URL_GET_SCORE_0, 10),
                (Response res) =>
                {
                    response = res;
                });

            while (coroutine.MoveNext()) { ;}

            Assert.AreEqual(200, response.StatusCode);
            Assert.AreEqual("", response.www.error);
        }

        [Test]
        [Category("BigTests")]
        [MaxTime(10000)]
        public void RestClient_PostShouldReturn_WrongSignature()
        {
            RestClient client = new RestClient(
                DEV.BASEURI, DEV.CLIENTID, DEV.APPID, DEV.APIKEY, DEV.APISECRET, DEV.SERVICE);

            Response response = null;

            var data = new { test = 0 };
            var body = JsonUtility.ToJson(data);
            var coroutine = client.Post( string.Format(URL_POST_SCORE), body, (Response res) => { response = res; });

            while (coroutine.MoveNext()) { ;}

            Assert.AreEqual(401, response.StatusCode);
            Assert.IsTrue(response.www.error.ToLowerInvariant().Contains("401 unauthorized"), response.www.error);
            Assert.IsTrue(response.www.text.ToLowerInvariant().Contains("wrong signature"), response.www.text);
        }
    }
}

namespace MiddleTests
{
    public class MockApiServer
    {
        private string URL_GET_SCORE_0 = "/v1/scoreboard/score/{0}";
        private string URL_POST_SCORE = "/v1/scoreboard/score";

        [Test]
        [Category("MiddleTests")]
        [MaxTime(10000)]
        public void RestClient_GetShouldReturn_200()
        {
            var client = Substitute.For<IRestClient>(
                DEV.BASEURI, DEV.CLIENTID, DEV.APPID, DEV.APIKEY, DEV.APISECRET, DEV.SERVICE);
            //RestClient client = new RestClient(
            //    DEV.BASEURI, DEV.CLIENTID, DEV.APPID, DEV.APIKEY, DEV.APISECRET, DEV.SERVICE);

            client.request("", null, Arg.Any<Dictionary<string, string>>()).Returns<WWW>(null, null);

            //client.request
            Response response = null;

            var coroutine = client.Get(string.Format(URL_GET_SCORE_0, 10),
                (Response res) =>
                {
                    response = res;
                });

            while (coroutine.MoveNext()) { ;}

            Assert.AreEqual(200, response.StatusCode);
            Assert.AreEqual("", response.www.error);
        }

        private object Dictionary<T1, T2>()
        {
            throw new NotImplementedException();
        }

    }
}
