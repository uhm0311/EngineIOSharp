using System;
using System.Timers;

namespace EngineIOSharp.Common
{
    internal class EngineIOTimeout : Timer
    {
        public EngineIOTimeout(Action Callback, double Delay)
        {
            AutoReset = false;
            Interval = Delay;

            Elapsed += (_, __) => Callback?.Invoke();
            Start();
        }
    }
}
