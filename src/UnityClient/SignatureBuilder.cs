﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnityClient
{
    // sign*(
    //   Algorithm name
    //   Date of request
    //   YYYYMMDD/svcname
    //   hash(
    //         (
    //           uri
    //           query
    //           headers
    //           list_headers
    //           hash( body )
    //         )
    //       )
    // )
    // 
    // * with apisecret
    //
    // Algorithm Credentials=APIKEY/YYYYMMDD/svcname SignedHeaders=list_headers Signature=sign

    public class SignatureBuilder
    {
        private const string Algorithm = "PGS1";
        private readonly string APPID = string.Empty;
        private readonly string APIKEY = string.Empty;
        private readonly string APISECRET = string.Empty;

        private const string HeaderFormat = "{0} Credentials={1} SignedHeaders={2} Signature={3}";
        private const string CredentialsFormat = "{0}/{1}/{2}";

        private string SignedHeaders;
        private string Signature;
        
        private string svcname;
        private string uri;
        private string query;
        private string headers;
        private string body;
        private string bodyHash;
        private string hash;

        
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
            ThrowIfNull(name , "name");
            ThrowIfNull(value , "value");
            
            SignedHeaders += string.Format("{0};", name);
            // TODO: value
        }

        public void AddService(string name)
        {
            ThrowIfNull(name, "name");
            svcname = name;
        }

        public void AddUrl(string url)
        {
            ThrowIfNull(url, "url");

            if (url.Contains("://"))
                throw new ArgumentException("url argument should not contains protocol part", url);

            if (url.Contains("."))
                throw new ArgumentException("url argument should not contains domain part", url);

            var pieces = url.Split('?');

            if (pieces.Length > 2)
                throw new ArgumentException("url argument cannot have more than one query part", url);

            uri = pieces[0];
            query = pieces.Length > 0 ? pieces[1] : "";
        }

        public string canonicalUri
        {
            get
            {
                ThrowIfNull(uri, "uri");
                return uri.ToLowerInvariant().Trim();
            }
        }


        public string CreateSignature()
        {
            string Credentials = string.Format(CredentialsFormat, APIKEY, "YYYYMMDD", svcname);
            return string.Format(HeaderFormat, Algorithm, Credentials, SignedHeaders, Signature);
        }


        private void ThrowIfNull(string value, string argName)
        {
            if (value == null || value == string.Empty || value == "")
                throw new ArgumentNullException(argName);
        }
    }
}
