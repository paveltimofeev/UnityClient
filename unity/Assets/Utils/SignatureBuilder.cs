using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityClient.Utils;
using UnityEngine;

namespace UnityClient
{
    public class SignatureBuilder
    {
        private const string Algorithm = "AWS4-HMAC-SHA256";
        private readonly string CLIENTID = string.Empty;
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
        
        public SignatureBuilder(string CLIENTID, string APPID, string APIKEY, string APISECRET)
        {
            ThrowIf.Null(CLIENTID, "CLIENTID");
            ThrowIf.Null(APPID, "APPID");
            ThrowIf.Null(APIKEY , "APIKEY");
            ThrowIf.Null(APISECRET, "APISECRET");

            this.CLIENTID = CLIENTID;
            this.APPID = APPID;
            this.APIKEY = APIKEY;
            this.APISECRET = APISECRET;
        }

        public void AddHeader(string name, string value)
        {
            ThrowIf.NullOnly(name, "name");
            ThrowIf.NullOnly(value , "value");
            
            // TODO: Case-sensitive, sorted?
            list_headers += string.Format("{0};", name);
            headers.Add(name, Regex.Replace(value.ToLowerInvariant().Trim(), @"\s+", " "));
        }

        public void AddService(string name)
        {
            ThrowIf.Null(name, "name");
            svcname = name;
        }

        public void AddUrl(string method, string url)
        {
            ThrowIf.Null(method, "method");

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
                // TODO: CredentialScope
                //return string.Format("{0}/{1}/{2}/{3}/{4}", CLIENTID, APPID, APIKEY, APISECRET, svcname);
                return string.Format("{0}/{1}", "_", svcname);
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
                    // TODO: figure out
                    return Convert.ToBase64String(hashBytes).Replace('+', '-').Replace('/', '_');
                }
            }
        }

        public string Credentials
        {
            get
            {
                return string.Format(CredentialsFormat, APIKEY, "_", svcname);
            }
        }

        public string CreateSignature()
        {
            return string.Format(HeaderFormat, Algorithm, Credentials, signedHeaders, Signature);
        }

        public void DebugInfo()
        {
            StringBuilder sb = new StringBuilder();
            
            sb.AppendLine("--------------------------");
            sb.AppendFormat("  canonicalUri          = {0}\r\n" , canonicalUri);
            sb.AppendFormat("  canonicalQuery        = {0}\r\n" , canonicalQuery);
            sb.AppendFormat("  canonicalHeaders      = {0}\r\n" , canonicalHeaders);
            sb.AppendFormat("  payloadHash           = {0}\r\n" , payloadHash);
            sb.AppendFormat("  canonicalRequest      = {0}\r\n" , canonicalRequest);
            sb.AppendFormat("  HashedCanonicalRequest= {0}\r\n" , HashedCanonicalRequest);
            sb.AppendFormat("  Algorithm             = {0}\r\n" , Algorithm);
            sb.AppendFormat("  RequestDate           = {0}\r\n" , RequestDate);
            sb.AppendFormat("  CredentialScope       = {0}\r\n" , CredentialScope);
            sb.AppendFormat("  Credentials           = {0}\r\n" , Credentials);
            sb.AppendFormat("  signedHeaders         = {0}\r\n" , signedHeaders);
            sb.AppendFormat("  stringToSign          = {0}\r\n" , stringToSign);
            sb.AppendFormat("  Signature             = {0}\r\n" , Signature);
            sb.AppendLine("--------------------------");
            
            Debug.Log(sb.ToString());
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

    }
}
