using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace UnityClient
{
    public class SignatureBuilder
    {
        private const string Algorithm = "AWS4-HMAC-SHA256";
        private readonly string APPID = string.Empty;
        private readonly string APIKEY = string.Empty;
        private readonly string APISECRET = string.Empty;

        private const string HeaderFormat = "{0} Credentials={1} SignedHeaders={2} Signature={3}";
        private const string CredentialsFormat = "{0}/{1}/{2}";
        
        private string svcname;
        private string method;
        private string uri;
        private string query;
        private Dictionary<string, string> headers = new Dictionary<string,string>();
        private string list_headers;
        
        private Encoding enc = Encoding.UTF8;
        
        public SignatureBuilder(string APPID, string APIKEY, string APISECRET)
        {
            ThrowIfNull(APPID , "APPID");
            ThrowIfNull(APIKEY , "APIKEY");
            ThrowIfNull(APISECRET, "APISECRET");

            this.APPID = APPID;
            this.APIKEY = APIKEY;
            this.APISECRET = APISECRET;
        }

        public void AddHeader(string name, string value)
        {
            ThrowIfNullOnly(name, "name");
            ThrowIfNullOnly(value , "value");
            
            // TODO: Case-sensitive, sorted?
            list_headers += string.Format("{0};", name);
            headers.Add(name, Regex.Replace(value.ToLowerInvariant().Trim(), @"\s+", " "));
        }

        public void AddService(string name)
        {
            ThrowIfNull(name, "name");
            svcname = name;
        }

        public void AddUrl(string method, string url)
        {
            ThrowIfNull(method, "method");

            if (url == null || url == string.Empty)
                url = "";
            
            if (url.Contains("://"))
                throw new ArgumentException("url argument should not contains protocol part", url);

            if (url.Contains("."))
                throw new ArgumentException("url argument should not contains domain part", url);

            this.method = method;
            var pieces = url.Split('?');

            if (pieces.Length > 2)
                throw new ArgumentException("url argument cannot have more than one query part", url);

            uri = pieces[0];
            query = pieces.Length > 1 ? pieces[1] : "";
        }

        public void AddBody(string body)
        {
            this.payloadHash = GetHash(body != null ? body : "");
        }

        public string canonicalUri
        {
            get
            {
                return uri != null ? uri.ToLowerInvariant().Trim() : "";
            }
        }

        public string canonicalQuery
        {
            get
            {
                if (query == null)
                    return "";
                
                // TODO: uri-encode spaces and so on
                string[] queryParam = query.Split('&');
                Array.Sort<string>(queryParam);
                return string.Join("&", queryParam);
            }
        }

        public string canonicalHeaders
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                headers.Any(x => sb.AppendFormat("{0}:{1}\n", x.Key, x.Value) == null);
                return sb.ToString()+'\n';
            }
        }

        public string signedHeaders
        {
            get
            {
                if (list_headers != null)
                    return list_headers.TrimEnd(new char[] { ';' });
                else
                    return ""; // TODO: really need?
            }
        }

        public string payloadHash { get; private set; }
        
        public string canonicalRequest
        {
            get
            {
                return string.Format("{0}\n{1}\n{2}\n{3}\n{4}\n{5}",
                    method, canonicalUri, canonicalQuery, canonicalHeaders, signedHeaders, payloadHash);
            }
        }

        public string HashedCanonicalRequest
        {
            get
            {
                return GetHash(canonicalRequest.ToLower());
            }
        }

        public string RequestDate
        {
            get
            {
                return "";
            }
        }

        public string CredentialScope
        {
            get
            {
                return string.Format("{0}/{1}", "20160619", svcname);
            }
        }

        public string stringToSign
        {
            get
            {
                return string.Format("{0}\n{1}\n{2}\n{3}", 
                    Algorithm, RequestDate, CredentialScope, HashedCanonicalRequest);
            }
        }

        public string Signature
        {
            get
            {
                ASCIIEncoding ascii = new System.Text.ASCIIEncoding();
                var keyByte = ascii.GetBytes(APISECRET);
                var messageBytes = ascii.GetBytes(stringToSign);
                using (var hmac = new HMACSHA256(keyByte))
                {
                    var hashBytes = hmac.ComputeHash(messageBytes);
                    return Convert.ToBase64String(hashBytes).TrimEnd('='); // TODO: trim is not needed
                }
            }
        }


        public string CreateSignature()
        {
            string Credentials = string.Format(CredentialsFormat, APIKEY, "20160619", svcname);
            return string.Format(HeaderFormat, Algorithm, Credentials, signedHeaders, Signature);
        }


        private string GetHash(string data)
        {
            StringBuilder sb = new StringBuilder();
            using (SHA256 hash = SHA256Managed.Create())
            {
                Byte[] result = hash.ComputeHash(enc.GetBytes(data));

                foreach (Byte b in result)
                {
                    sb.Append(b.ToString("x2"));
                }
            }

            return sb.ToString();
        }

        private void ThrowIfNull(string value, string argName)
        {
            if (value == null || value == string.Empty || value == "")
                throw new ArgumentNullException(argName);
        }

        private void ThrowIfNullOnly(object value, string argName)
        {
            if (value == null)
                throw new ArgumentNullException(argName);
        }
    }

    public static class Method
    {
        public const string GET = "GET";
        public const string POST = "POST";
    }
}
