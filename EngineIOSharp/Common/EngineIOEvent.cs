namespace EngineIOSharp.Common
{
    public abstract class EngineIOEvent<T> where T : EngineIOEvent<T>
    {
        public string Data { get; private set; }

        protected EngineIOEvent(string Data) 
        {
            this.Data = Data;
        }

        public override string ToString()
        {
            return Data ?? base.ToString();
        }

        public override bool Equals(object obj)
        {
            return obj is T && obj.ToString().Equals(ToString());
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }
    }
}
