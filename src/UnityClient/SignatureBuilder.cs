using System;
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
            if (APPID == null || APPID == "") throw new ArgumentNullException("APPID");
            if (APIKEY == null || APIKEY == "") throw new ArgumentNullException("APIKEY");
            if (APISECRET == null || APISECRET == "") throw new ArgumentNullException("APISECRET");

            this.APPID = APPID;
            this.APIKEY = APIKEY;
            this.APISECRET = APISECRET;
        }

        public void AddHeader(string name, string value)
        {
            if (name == null || name == "") throw new ArgumentNullException("name");
            if (value == null || value == "") throw new ArgumentNullException("APPID");
            
            SignedHeaders += string.Format("{0};", name);
            // TODO: value
        }

        public void AddService(string name)
        {
            if (name == null || name == "") throw new ArgumentNullException("name");
            svcname = name;
        }

        public string CreateSignature()
        {
            string Credentials = string.Format(CredentialsFormat, APIKEY, "YYYYMMDD", svcname);
            return string.Format(HeaderFormat, Algorithm, Credentials, SignedHeaders, Signature);
        }
    }
}
