using System;
using System.Diagnostics;
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
            builder.AddUrl(Method.GET, "");
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
        public void AddedHeaderNamesShouldBeJoinedToSignedHeadersWithoutTailingSemicolon()
        {
            string HEADER1 = "X-DATA";
            string HEADER2 = "X-DEBUG";
            string HEADERVALUE = "VALUE";

            SignatureBuilder builder = new SignatureBuilder(APPID, APIKEY, APISECRET);
            builder.AddHeader(HEADER1, HEADERVALUE);
            builder.AddHeader(HEADER2, HEADERVALUE);

            Assert.AreEqual(builder.signedHeaders, HEADER1 + ";" + HEADER2);
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

            ExpectedException<ArgumentNullException>(() =>
            {
                SignatureBuilder builder = new SignatureBuilder(APPID, APIKEY, APISECRET);
                builder.AddService("");
            });
        }


        [TestMethod]
        public void UrlsWithProtocolOrDomainIsNotAllowed()
        {
            ExpectedException<ArgumentException>(() =>
            {
                SignatureBuilder builder = new SignatureBuilder(APPID, APIKEY, APISECRET);
                builder.AddUrl(Method.GET, "http://url");
            });

            ExpectedException<ArgumentException>(() =>
            {
                SignatureBuilder builder = new SignatureBuilder(APPID, APIKEY, APISECRET);
                builder.AddUrl(Method.GET, "url.com");
            });

        }

        [TestMethod]
        public void UrlsCannotContainsMoreThenOneQuery()
        {
            ExpectedException<ArgumentException>(() =>
            {
                SignatureBuilder builder = new SignatureBuilder(APPID, APIKEY, APISECRET);
                builder.AddUrl(Method.GET, "/v1/path?query?query");
            });
        }

        [TestMethod]
        public void AddUrlShouldCreateCanonicalUri()
        {
            string URL = " /V1/Path  ?query=val&param2=val2";
            string CanonicalURI = "/v1/path";

            SignatureBuilder builder = new SignatureBuilder(APPID, APIKEY, APISECRET);
            builder.AddUrl(Method.GET, URL);

            Assert.AreEqual(builder.canonicalUri, CanonicalURI);
        }

        
        [TestMethod]
        public void AddUrlShouldCreateCanonicalQuery()
        {
            string URL = " /V1/Path  ?b=val&a=val2";
            string CanonicalQuery = "a=val2&b=val";

            SignatureBuilder builder = new SignatureBuilder(APPID, APIKEY, APISECRET);
            builder.AddUrl(Method.GET, URL);

            Assert.AreEqual(builder.canonicalQuery, CanonicalQuery);
        }

        [TestMethod]
        public void AddHeaderShouldCreateCanonicalHeaders()
        {
            SignatureBuilder builder = new SignatureBuilder(APPID, APIKEY, APISECRET);
            builder.AddHeader("HOST", "1");
            builder.AddHeader("range", "2");
            builder.AddHeader("X-Date", "3");

            Assert.AreEqual(builder.canonicalHeaders, "HOST:1\nrange:2\nX-Date:3\n\n");
        }


        [TestMethod]
        [TestCategory("DataDriven")]
        public void SignatureCreationSteps()
        {
            SignatureBuilder builder = new SignatureBuilder(APPID, "<APIKEY>", "<APISECRET>");
            builder.AddHeader("host", "https://localhost");
            builder.AddHeader("range", "");
            builder.AddHeader("x-date", "20160501");
            builder.AddService("scoreboard");
            builder.AddUrl(Method.GET, "/v1/serviceName/action/parameter?arg1=1&arg3=3&arg2=2");
            builder.AddBody("");

            Assert.AreEqual("20160619/scoreboard", builder.CredentialScope, "Mistake in CredentialScope step");
            Assert.AreEqual("host:https://localhost\nrange:\nx-date:20160501\n\n", builder.canonicalHeaders, "Mistake in canonicalHeaders step");
            Assert.AreEqual(@"arg1=1&arg2=2&arg3=3", builder.canonicalQuery, "Mistake in canonicalQuery step");
            Assert.AreEqual(@"/v1/servicename/action/parameter", builder.canonicalUri, "Mistake in canonicalUri step");
            Assert.AreEqual(@"host;range;x-date", builder.signedHeaders, "Mistake in signedHeaders step");

            var expPayloadHash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";
            var actPayloadHash = builder.payloadHash;
            Assert.AreEqual(expPayloadHash, actPayloadHash, "Mistake in payloadHash step");

            var expCanonicalRequest = "GET\n/v1/servicename/action/parameter\narg1=1&arg2=2&arg3=3\nhost:https://localhost\nrange:\nx-date:20160501\n\n\nhost;range;x-date\ne3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";
            var actCanonicalRequest = builder.canonicalRequest;            
            Assert.AreEqual(expCanonicalRequest, actCanonicalRequest, "Mistake in canonicalRequest step");

            Assert.AreEqual("1a64e2178c77dc7c94d2786462fe276d60a527edc1a751654ca6577ba885152e", builder.HashedCanonicalRequest, "Mistake in HashedCanonicalRequest step");

            var expStringToSign = "AWS4-HMAC-SHA256\n\n20160619/scoreboard\n1a64e2178c77dc7c94d2786462fe276d60a527edc1a751654ca6577ba885152e";
            var actStringToSign = builder.stringToSign;
            Assert.AreEqual(expStringToSign, actStringToSign, "Mistake in stringToSign step");

            Assert.AreEqual("UVv7IXUJW9JXucGkO1SNCSpHzIRxvljisAFLGLIa6Rg", builder.Signature, "Mistake in Header step");

            var expSignatureHeader = "AWS4-HMAC-SHA256 Credentials=<APIKEY>/20160619/scoreboard SignedHeaders=host;range;x-date Signature=UVv7IXUJW9JXucGkO1SNCSpHzIRxvljisAFLGLIa6Rg";
            var actSignatureHeader = builder.CreateSignature();
            Assert.AreEqual(expSignatureHeader, actSignatureHeader, "Mistake in Header step");

/* 
stringToSign= PGS1

YYYYMMDD/testService
§]
JùÈY=¢w§Ë¯k;IT¯­
canonicalRequest= 
/v1/path
p1=a&p2=b
host: 1
range: 2
x-date: 3

host;range;x-date
d-
,>|º83-b:iq )þ²¡çù&,
HashedCanonicalRequest= §]
JùÈY=¢w§Ë¯k;IT¯­
Signature= ?*@°x
»ø´¤£;tç
LãÄ
Header= PGS1 Credentials=TEST-APIKEY/YYYYMMDD/testService SignedHeaders=HOST;range;X-Date Signature=?*@°x
»ø´¤£;tç
LãÄ

             
             
*/

            Debug.Print("------------------------------------------------");

            Debug.Print("CredentialScope= {0}", builder.CredentialScope);
            Debug.Print("canonicalHeaders= {0}", builder.canonicalHeaders);
            Debug.Print("canonicalQuery= {0}", builder.canonicalQuery);
            Debug.Print("canonicalUri= {0}", builder.canonicalUri);
            Debug.Print("signedHeaders= {0}", builder.signedHeaders);
            Debug.Print("payloadHash= {0}", builder.payloadHash);
            Debug.Print("stringToSign= {0}", builder.stringToSign);
            Debug.Print("canonicalRequest= {0}", builder.canonicalRequest);
            Debug.Print("HashedCanonicalRequest= {0}", builder.HashedCanonicalRequest);
            Debug.Print("Signature= {0}", builder.Signature);
            
            Debug.Print("Header= {0}", builder.CreateSignature());

            Debug.Print("------------------------------------------------");

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
