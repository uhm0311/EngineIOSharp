using System;

namespace EngineIOSharp
{
    public static class EngineIOLogger
    {
        public delegate void ExceptionListener(object sender, Exception e);

        public static ExceptionListener E = (sender, e) =>
        {
            Console.WriteLine("{0} : {1}", sender, e);
        };
    }
}
