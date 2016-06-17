using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UnityClient;

namespace Tests
{
    [TestClass]
    public class SignatureBuilderTest
    {
        string APPID = "TEST-APPID";
        string APIKEY = "TEST-APIKEY";
        string APISECRET = "TEST-APISECRET";
            
        [TestMethod]
        public void CreateSignature()
        {
            SignatureBuilder builder = new SignatureBuilder(APPID, APIKEY, APISECRET);
            string sign = builder.CreateSignature();

            Assert.IsNotNull(sign);
        }

        [TestMethod]
        public void NullOrEmptyCredsAreNotAllowed()
        {
            ExpectedException<ArgumentNullException>(() => {
                SignatureBuilder builder = new SignatureBuilder(null, APIKEY, APISECRET);});

            ExpectedException<ArgumentNullException>(() =>{
                SignatureBuilder builder = new SignatureBuilder(APPID, null, APISECRET);});

            ExpectedException<ArgumentNullException>(() =>{
                SignatureBuilder builder = new SignatureBuilder(APPID, APIKEY, null);});

            ExpectedException<ArgumentNullException>(() =>{
                SignatureBuilder builder = new SignatureBuilder("", APIKEY, APISECRET);});

            ExpectedException<ArgumentNullException>(() =>{
                SignatureBuilder builder = new SignatureBuilder(APPID, "", APISECRET);});

            ExpectedException<ArgumentNullException>(() =>{
                SignatureBuilder builder = new SignatureBuilder(APPID, APIKEY, "");});
        }

        [TestMethod]
        public void SignatureShouldContainsAddedHeaders()
        {
            string HEADER = "X-DATA";
            string HEADERVALUE = "VALUE";

            SignatureBuilder builder = new SignatureBuilder(APPID, APIKEY, APISECRET);
            builder.AddHeader(HEADER, HEADERVALUE);
            string sign = builder.CreateSignature();

            Assert.IsTrue(sign.Contains(HEADER));
        }

        [TestMethod]
        public void NullOrEmptyHeadersAreNotAllowed()
        {
            string HEADER = "X-DATA";
            string HEADERVALUE = "VALUE";

            ExpectedException<ArgumentNullException>(() =>
            {
                SignatureBuilder builder = new SignatureBuilder(APPID, APIKEY, APISECRET);
                builder.AddHeader(null, HEADERVALUE);
            });

            ExpectedException<ArgumentNullException>(() =>
            {
                SignatureBuilder builder = new SignatureBuilder(APPID, APIKEY, APISECRET);
                builder.AddHeader(HEADER, null);
            });

            ExpectedException<ArgumentNullException>(() =>
            {
                SignatureBuilder builder = new SignatureBuilder(APPID, APIKEY, APISECRET);
                builder.AddHeader(null, null);
            });
        }


        [TestMethod]
        public void SignatureShouldContainsAddedServiceWithSlashPrefix()
        {
            string SERVICENAME = "SERVICE";

            SignatureBuilder builder = new SignatureBuilder(APPID, APIKEY, APISECRET);
            builder.AddService(SERVICENAME);
            string sign = builder.CreateSignature();

            Assert.IsTrue(sign.Contains("/"+SERVICENAME));
        }

        [TestMethod]
        public void SignatureShouldContainsApikeyWithSlashPostfix()
        {
            string SERVICENAME = "SERVICE";

            SignatureBuilder builder = new SignatureBuilder(APPID, APIKEY, APISECRET);
            builder.AddService(SERVICENAME);
            string sign = builder.CreateSignature();

            Assert.IsTrue(sign.Contains(APIKEY + "/"));
        }

        [TestMethod]
        public void NullOrEmptyServiceIsNotAllowed()
        {
            ExpectedException<ArgumentNullException>(() =>
            {
                SignatureBuilder builder = new SignatureBuilder(APPID, APIKEY, APISECRET);
                builder.AddService(null);
            });
        }


        void ExpectedException<T>(Action action)
            where T:Exception
        {
            try
            {
                action.Invoke();
            }
            catch(T expectedException)
            {
                Assert.AreEqual(typeof(T), expectedException.GetType());
                return;
            }
            catch(Exception ex)
            {
                Assert.AreEqual(typeof(T), ex.GetType());
                return;
            }

            Assert.Fail("Expected exception of type " + typeof(T));
            
        }
    }
}
