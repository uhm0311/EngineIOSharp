namespace EngineIOSharp.Common.Type.Abstract
{
    public abstract class EngineIOType<TEngineIOType, TData> where TEngineIOType : EngineIOType<TEngineIOType, TData>
    {
        public TData Data { get; private set; }

        protected EngineIOType(TData Data) 
        {
            this.Data = Data;
        }

        public override string ToString()
        {
            return Data?.ToString() ?? base.ToString();
        }

        public override bool Equals(object obj)
        {
            return obj is TEngineIOType && obj.ToString().Equals(ToString());
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }
    }
}
