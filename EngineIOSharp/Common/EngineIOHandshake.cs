using Newtonsoft.Json.Linq;

namespace EngineIOSharp.Common
{
    public class EngineIOHandshake
    {
        private readonly JObject JSON;

        public string SID { get; private set; }
        public string[] Upgrades { get; private set; }
        public ulong PingInterval { get; private set; }
        public ulong PingTimeout { get; private set; }

        internal EngineIOHandshake(string JSON)
        {
            this.JSON = JObject.Parse(JSON);

            SID = this.JSON["sid"].ToString();
            Upgrades = this.JSON["upgrades"].ToObject<string[]>();
            PingInterval = this.JSON["pingInterval"].ToObject<ulong>();
            PingTimeout = this.JSON["pingTimeout"].ToObject<ulong>();
        }

        public override string ToString()
        {
            return JSON.ToString();
        }

        public override bool Equals(object obj)
        {
            if (obj is EngineIOHandshake)
            {
                return ToString().Equals(obj.ToString());
            }

            return false;
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }
    }
}
