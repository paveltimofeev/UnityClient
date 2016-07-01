using UnityEngine;
using UnityEditor;
using NUnit.Framework;
using System;
using Rest;

namespace SmallTests
{
    public class ScoreboardServiceTests
    {
        [Test]
        [Category("Small")]
        [MaxTime(10000)]
        [Ignore]
        public void GetLeaderboard()
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ScoreboardService service = go.AddComponent<ScoreboardService>();
            service._baseUri = DEV.BASEURI;
            service._clientId = DEV.CLIENTID;
            service._appId = DEV.APPID;
            service._apiKey = DEV.APIKEY;
            service._apiSecret = DEV.APISECRET;

            var done = false;
            service.GetTop((Exception ex, TopScores scores) => {

                if (ex != null)
                    Assert.Fail(ex.Message);

                done = true;
            });

            Assert.That(done, Is.True.After(3000));
        }

        [Test]
        [Category("Small")]
        [MaxTime(10000)]
        [Ignore]
        public void GetMyBestScore()
        {
            Assert.Fail();
        }

        [Test]
        [Category("Small")]
        [MaxTime(10000)]
        [Ignore]
        public void GetAppLeaderboards()
        {
            Assert.Fail();
        }

        [Test]
        [Category("Small")]
        [MaxTime(10000)]
        [Ignore]
        public void GetScopes()
        {
            Assert.Fail();
        }

        [Test]
        [Category("Small")]
        [MaxTime(10000)]
        [Ignore]
        public void PostScore()
        {
            Assert.Fail();
        }
    }
}
