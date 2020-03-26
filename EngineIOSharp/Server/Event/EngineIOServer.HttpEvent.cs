using EngineIOSharp.Common;
using EngineIOSharp.Common.Packet;
using EngineIOSharp.Common.Type;
using Newtonsoft.Json.Linq;
using SimpleThreadMonitor;
using System.Collections.Generic;
using System.Text;
using WebSocketSharp.Net;
using WebSocketSharp.Server;

namespace EngineIOSharp.Server
{
    partial class EngineIOServer
    {
        private readonly List<string> SIDList = new List<string>();

        private void OnHttpRequest(object sender, HttpRequestEventArgs e)
        {
            string SID = e.Request.QueryString["sid"] ?? EngineIOSocketID.Generate();

            SimpleMutex.Lock(ServerMutex, () =>
            {
                if (string.IsNullOrWhiteSpace(e.Request.QueryString["sid"]) || SIDList.Contains(SID))
                {
                    string Transport = e.Request.QueryString["transport"];

                    if (!string.IsNullOrWhiteSpace(Transport) && Transport.Equals("polling"))
                    {
                        if (HttpManager.IsValidHeader(e.Request.Headers["origin"] ?? e.Request.Headers["Origin"]))
                        {
                            string Method = e.Request.HttpMethod.ToLower().Trim();

                            if (Method.Equals("get") || SIDList.Contains(SID))
                            {
                                StringBuilder ResponseBody = new StringBuilder();

                                if (Method.Equals("get"))
                                {
                                    if (!SIDList.Contains(SID))
                                    {
                                        SIDList.Add(SID);

                                        ResponseBody.Append('0');
                                        ResponseBody.Append(new JObject()
                                        {
                                            ["sid"] = SID,
                                            ["upgrades"] = new JArray() { "websocket" },
                                            ["pingInterval"] = PingInterval,
                                            ["pingTimeout"] = PingTimeout,
                                        }.ToString().Replace("\r", "").Replace("\n", "").Replace("\t", "").Replace(" ", "").Trim());
                                    }
                                    else
                                    {
                                        ResponseBody.Append((int)EngineIOPacketType.NOOP);
                                    }
                                }

                                ResponseBody.Insert(0, string.Format("{0}:", Encoding.UTF8.GetByteCount(ResponseBody.ToString())));
                                HttpManager.SendOKResponse(e.Response, SID, ResponseBody.ToString());
                            }
                            else
                            {
                                HttpManager.SendErrorResponse(e.Response, EngineIOErrorType.BAD_HANDSHAKE_METHOD);
                            }
                        }
                        else
                        {
                            HttpManager.SendErrorResponse(e.Response, EngineIOErrorType.BAD_REQUEST);
                        }
                    }
                    else
                    {
                        HttpManager.SendErrorResponse(e.Response, EngineIOErrorType.UNKNOWN_TRANSPORT);
                    }
                } 
                else
                {
                    HttpManager.SendErrorResponse(e.Response, EngineIOErrorType.UNKNOWN_SID);
                }
            }, (Exception) => HttpManager.SendErrorResponse(e.Response, EngineIOErrorType.BAD_REQUEST));
        }

        internal static class HttpManager
        {
            public readonly static int[] ValidHeader = new int[]
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
            public static void SendOKResponse(HttpListenerResponse Response, string SID, string Content)
            {
                if ((Response?.OutputStream?.CanWrite ?? false) && !string.IsNullOrWhiteSpace(Content))
                {
                    byte[] Buffer = Encoding.UTF8.GetBytes(Content);

                    Response.Headers["Access-Control-Allow-Origin"] = "*";
                    Response.Headers["Set-Cookie"] = string.Format("io={0}; Path=/; HttpOnly", SID);
                    Response.KeepAlive = false;

                    Response.ContentType = "text/plain; charset=UTF-8";
                    Response.ContentEncoding = Encoding.UTF8;
                    Response.ContentLength64 = Buffer.LongLength;

                    Response.OutputStream.Write(Buffer, 0, Buffer.Length);
                    Response.OutputStream.Close();
                    Response.Close();
                }
            }

            public static void SendErrorResponse(HttpListenerResponse Response, EngineIOErrorType Error)
            {
                if ((Response?.OutputStream?.CanWrite ?? false))
                {
                    JObject Content = new JObject()
                    {
                        ["code"] = Error.Data,
                        ["message"] = Error.ToString()
                    };

                    byte[] Buffer = Encoding.UTF8.GetBytes(Content.ToString());

                    Response.ContentType = "application/json";
                    Response.ContentEncoding = Encoding.UTF8;
                    Response.ContentLength64 = Buffer.LongLength;

                    Response.KeepAlive = false;

                    if ((Error ?? EngineIOErrorType.FORBIDDEN) != EngineIOErrorType.FORBIDDEN)
                    {
                        Response.Headers["Access-Control-Allow-Origin"] = "*";
                        Response.StatusCode = 400;
                    }
                    else
                    {
                        Response.StatusCode = 403;
                    }

                    Response.OutputStream.Write(Buffer, 0, Buffer.Length);
                    Response.OutputStream.Close();
                    Response.Close();
                }
            }

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
        }
    }
}
