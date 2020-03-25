using EngineIOSharp.Common;
using EngineIOSharp.Common.Packet;
using EngineIOSharp.Common.Type;
using Newtonsoft.Json.Linq;
using SimpleThreadMonitor;
using System;
using System.Collections.Generic;
using System.Text;
using WebSocketSharp.Net;
using WebSocketSharp.Server;

namespace EngineIOSharp.Server
{
    partial class EngineIOServer
    {
        private List<string> SIDList = new List<string>();

        private void OnHttpRequest(object sender, HttpRequestEventArgs e)
        {
            SimpleMutex.Lock(ServerMutex, () =>
            {
                string SID = e.Request.QueryString["sid"] ?? EngineIOSocketID.Generate();

                if (!string.IsNullOrWhiteSpace(e.Request.QueryString["sid"]) && !SIDList.Contains(SID))
                {
                    SendErrorResponse(e.Response, EngineIOError.UNKNOWN_SID);
                    return;
                }

                string Transport = e.Request.QueryString["transport"];

                if (string.IsNullOrWhiteSpace(Transport) || !(Transport.Equals(EngineIOTransport.POLLING.Data) || Transport.Equals(EngineIOTransport.WEBSOCKET.Data)))
                {
                    SendErrorResponse(e.Response, EngineIOError.UNKNOWN_TRANSPORT);
                    return;
                }

                if (!string.IsNullOrWhiteSpace(Transport) && !Transport.Equals("websocket"))
                {
                    string Method = e.Request.HttpMethod.ToLower().Trim();
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

                        ResponseBody.Insert(0, string.Format("{0}:", Encoding.UTF8.GetByteCount(ResponseBody.ToString())));
                    }

                    SendOKResponse(e.Response, SID, ResponseBody.ToString());
                }
            });
        }

        private void SendOKResponse(HttpListenerResponse Response, string SID, string Content)
        {
            if ((Response?.OutputStream?.CanWrite ?? false) && !string.IsNullOrWhiteSpace(Content) && SIDList.Contains(SID))
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

        private void SendErrorResponse(HttpListenerResponse Response, EngineIOError Error)
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

                if ((Error ?? EngineIOError.FORBIDDEN) != EngineIOError.FORBIDDEN)
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
    }
}
