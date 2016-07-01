using UnityEngine;
using UnityEditor;
using NUnit.Framework;
using System;
using Rest;
using SmallTests;
using System.Collections.Generic;
// using NSubstitute; // Install-Package NSubstitute

namespace MiddleTests
{
    public class MockApiServer
    {
        private string URL_GET_SCORE_0 = "/v1/scoreboard/score/{0}";
        private string URL_POST_SCORE = "/v1/scoreboard/score";

        [Test]
        [Category("MiddleTests")]
        [MaxTime(10000)]
        [Ignore]
        public void RestClient_GetShouldReturn_200()
        {
            //var client = Substitute.For<IRestClient>(DEV.BASEURI, DEV.CLIENTID, DEV.APPID, DEV.APIKEY, DEV.APISECRET, DEV.SERVICE);
            var client = new RestClient(DEV.BASEURI, DEV.CLIENTID, DEV.APPID, DEV.APIKEY, DEV.APISECRET, DEV.SERVICE);
            
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
    }
}
