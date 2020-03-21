using EngineIOSharp.Common;
using EngineIOSharp.Common.Packet;
using Newtonsoft.Json.Linq;

namespace EngineIOSharp.Client
{
    partial class EngineIOClient
    {
        private void HandleEnginePacket(EngineIOPacket Packet)
        {
            if (Packet != null)
            {
                switch (Packet.EnginePacketType)
                {
                    case EngineIOPacketType.OPEN:
                        HandleOpen(JObject.Parse(Packet.Data));
                        break;

                    case EngineIOPacketType.CLOSE:
                        HandleClose();
                        break;

                    case EngineIOPacketType.PING:
                        CallEventHandler(EngineIOEvent.PING);
                        break;

                    case EngineIOPacketType.PONG:
                        CallEventHandler(EngineIOEvent.PONG);
                        break;

                    case EngineIOPacketType.MESSAGE:
                        CallEventHandler(EngineIOEvent.MESSAGE, Packet);
                        break;

                    default:
                        HandleEtc();
                        break;
                }
            }
        }

        private void HandleOpen(JObject JsonData)
        {
            if (JsonData != null)
            {
                SocketID = JsonData["sid"].ToString();

                StartHeartbeat(int.Parse(JsonData["pingInterval"].ToString()), int.Parse(JsonData["pingTimeout"].ToString()));
                CallEventHandler(EngineIOEvent.OPEN);
            }
        }

        private void HandleClose()
        {
            Close();
        }

        private void HandleEtc()
        {
        }
    }
}
