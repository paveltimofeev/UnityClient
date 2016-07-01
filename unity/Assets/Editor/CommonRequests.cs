﻿using UnityEngine;
using UnityEditor;
using NUnit.Framework;
using System;
using Rest;
using SmallTests;
using System.Collections.Generic;


namespace BigTests
{
    public class CommonRequests
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
        public void RestClient_IfKeyIsNotCorrect_GetShouldReturn_Forbidden()
        {
            RestClient client = new RestClient(DEV.BASEURI, "-", "-", "-", "-", DEV.SERVICE);

            Response response = null;

            var coroutine = client.Get(string.Format(URL_GET_SCORE_0, 10),
                (Response res) =>
                {
                    response = res;
                });

            while (coroutine.MoveNext()) { ;}

            Assert.AreEqual(403, response.StatusCode);
            Assert.AreEqual("403 Forbidden", response.www.error.Trim( new char[]{'\r','\n'} ));
        }

        [Test]
        [Category("BigTests")]
        [MaxTime(10000)]
        [Ignore]
        public void RestClient_PostShouldReturn_WrongSignature()
        {
            RestClient client = new RestClient(
                DEV.BASEURI, DEV.CLIENTID, DEV.APPID, DEV.APIKEY, DEV.APISECRET, DEV.SERVICE);

            Response response = null;

            var data = new { test = 0 };
            var body = JsonUtility.ToJson(data);
            var coroutine = client.Post(string.Format(URL_POST_SCORE), body, (Response res) => { response = res; });

            while (coroutine.MoveNext()) { ;}

            Assert.AreEqual(400, response.StatusCode);
            Assert.IsTrue(response.www.error.ToLowerInvariant().Contains("401 unauthorized"), response.www.error);
            Assert.IsTrue(response.www.text.ToLowerInvariant().Contains("wrong signature"), response.www.text);
        }


        Response RequestCommonGet()
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

            return response;
        }

        [Test]
        [Category("BigTests")]
        [MaxTime(10000)]
        public void RestClient_GetShouldReturn_200()
        {
            var response = RequestCommonGet();
            Assert.AreEqual(200, response.StatusCode);
        }


        [Test]
        [Category("BigTests")]
        [MaxTime(10000)]
        public void RestClient_GetShouldNotReturn_Error()
        {
            var response = RequestCommonGet();
            Assert.AreEqual(null, response.www.error);
        }

        [Test]
        [Category("BigTests")]
        [MaxTime(10000)]
        public void RestClient_GetShouldReturn_ValidData()
        {
            var response = RequestCommonGet();
            Assert.AreEqual("[]", response.www.text);
        }

    }
}