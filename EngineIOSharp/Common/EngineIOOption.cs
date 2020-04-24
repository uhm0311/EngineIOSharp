using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace EngineIOSharp.Common
{
    internal static class EngineIOOption
    {
        internal static string PolishPath(string Path)
        {
            Path = Path ?? string.Empty;

            while (Path.IndexOf('/') != Path.LastIndexOf('/') && Path.EndsWith("/"))
            {
                Path = Path.Substring(0, Path.Length - 1);
            }

            return Path + '/';
        }

        internal static bool DefaultCertificateValidationCallback(object _1, X509Certificate _2, X509Chain _3, SslPolicyErrors _4)
        {
            return true;
        }
    }
}
