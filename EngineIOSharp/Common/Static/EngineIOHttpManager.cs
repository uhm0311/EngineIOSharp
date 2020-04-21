using EngineIOSharp.Common.Enum.Internal;
using EngineIOSharp.Server;
using EngineIOSharp.Server.Client.Transport;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using WebSocketSharp.Net;

namespace EngineIOSharp.Common.Static
{
    internal static class EngineIOHttpManager
    {
        private readonly static int[] ValidHeader = new int[]
        {
                0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, // 0 - 15
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 16 - 31
                1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // 32 - 47
                1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // 48 - 63
                1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // 64 - 79
                1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // 80 - 95
                1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // 96 - 111
                1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, // 112 - 127
                1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // 128 ...
                1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
                1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
                1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
                1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
                1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
                1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
                1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1  // ... 255
        };

        public static bool IsValidHeader(string Header)
        {
            if (string.IsNullOrWhiteSpace(Header))
            {
                Header = "";
            }

            for (int i = 0; i < Header.Length; i++)
            {
                if (!(Header[i] >= 0 && Header[i] <= 0xff && ValidHeader[Header[i]] == 1))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool IsPolling(HttpListenerRequest Request)
        {
            return GetTransport(Request.QueryString).Equals(EngineIOPolling.Name);
        }

        public static bool IsWebSocket(HttpListenerRequest Request)
        {
            return GetTransport(Request.QueryString).Equals(EngineIOWebSocket.Name);
        }

        public static string GetTransport(NameValueCollection Query)
        {
            return Query["transport"]?.Trim()?.ToLower() ?? string.Empty;
        }

        public static string GetSID(NameValueCollection Query)
        {
            return Query["sid"]?.Trim() ?? string.Empty;
        }

        public static EngineIOHttpMethod ParseMethod(string Method)
        {
            return (EngineIOHttpMethod)System.Enum.Parse(typeof(EngineIOHttpMethod), Method.Trim().ToUpper());
        }

        public static void SendErrorMessage(HttpListenerRequest Request, HttpListenerResponse Response, EngineIOException Exception)
        {
            using (Response)
            {
                int Code = EngineIOServer.Exceptions.IndexOf(Exception);
                string Message = Exception.Message;

                if (EngineIOServer.Exceptions.Contains(Exception))
                {
                    string Origin = Request.Headers["Origin"]?.Trim() ?? string.Empty;

                    if (!string.IsNullOrWhiteSpace(Origin))
                    {
                        Response.Headers["Access-Control-Allow-Credentials"] = "true";
                        Response.Headers["Access-Control-Allow-Origin"] = Origin;
                    }
                    else
                    {
                        Response.Headers["Access-Control-Allow-Origin"] = "*";
                    }

                    Response.StatusCode = 400;
                }
                else
                {
                    Response.StatusCode = 403;
                    Message = "Forbidden.";
                }

                byte[] RawData = Encoding.UTF8.GetBytes(new JObject()
                {
                    ["code"] = Code,
                    ["message"] = Message
                }.ToString());

                using (Response.OutputStream)
                {
                    Response.OutputStream.Write(RawData, 0, RawData.Length);
                }
            }
        }
    }
}
