using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UnityEngine;
using Rest;

namespace Tests
{
    /// <summary>
    /// Summary description for RestBehaviourTests
    /// </summary>
    [TestClass]
    public class RestBehaviourTests
    {
        public RestBehaviourTests()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        [TestMethod]
        public void TestMethod1()
        {
            /*
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);

            RestBehaviour sut = go.AddComponent<RestBehaviour>();

            sut._apiKey = "";
            sut._apiKey = "";
            sut._apiKey = "";
            sut._apiKey = "";

            sut.Get<string>("http://google.com", (Exception ex, string result) => {

                Assert.IsTrue(true);
            });
            */


            /*
             
            Moq.MockRepository repo = new Moq.MockRepository(Moq.MockBehavior.Strict);
            
            var headers = new Dictionary<string, string>();
            headers.Add("STATUS", "HTTP/1.1 200 OK");
            
            var wwwMock = repo.Create<WWW>();
            wwwMock.Setup(w => w.isDone).Returns(true);
            wwwMock.Setup(w => w.responseHeaders).Returns(headers);

            Response resp = new Response(wwwMock.Object);

            Assert.AreEqual(200, resp.StatusCode);
             
            */

        }
    }
}
