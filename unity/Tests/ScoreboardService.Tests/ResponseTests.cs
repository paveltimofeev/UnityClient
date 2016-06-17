using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using rest;
using UnityEngine;

namespace ScoreboardService.Tests
{
    [TestClass]
    public class ResponseTests
    {
        [TestMethod]
        [Microsoft.VisualStudio.TestTools.UnitTesting.Ignore]
        public void ResponseCodesTest()
        {
            Moq.MockRepository repo = new Moq.MockRepository(Moq.MockBehavior.Strict);
            
            var headers = new Dictionary<string, string>();
            headers.Add("STATUS", "HTTP/1.1 200 OK");
            
            var wwwMock = repo.Create<WWW>();
            wwwMock.Setup(w => w.isDone).Returns(true);
            wwwMock.Setup(w => w.responseHeaders).Returns(headers);

            Response resp = new Response(wwwMock.Object);

            Assert.AreEqual(200, resp.StatusCode);
        }
    }
}
