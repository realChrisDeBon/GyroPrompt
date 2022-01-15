using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace GyroPrompt
{
    class FTPServer
    {
        public string User { get; set; }
        public string Pass { get; set; }
        public string Host = "ftp://127.0.0.1:24/Index";
        
        public void Boot(string user, string pass)
        {
            User = user;
            Pass = pass;

                WebRequest webRequest = WebRequest.Create(Host);
                webRequest.Method = WebRequestMethods.Ftp.MakeDirectory;
                webRequest.Credentials = new NetworkCredential(user, pass);
                using (var resp = (FtpWebResponse)webRequest.GetResponse())
                {
                    Console.WriteLine();
                    Console.WriteLine("GyroPrompt > " + resp.StatusCode + ".");
                }                
        }
        
    }
}
