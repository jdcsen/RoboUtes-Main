using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RoboUtes
{
    public static class Extensions
    {

        public static Stream GetEmbeddedResourceStream(string resourceName)
        {
            return Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
        }
        public static string[] GetEmbeddedResourceNames()
        {
            return Assembly.GetExecutingAssembly().GetManifestResourceNames();
        }

    }

    public static class UriHelper
    {
        /// <summary>
        /// Gets absulute URI for provided relative path
        /// </summary>
        /// <param name="baseType">Base type for ussage as URI root</param>
        /// <param name="relativePath">Relative path</param>
        /// <returns>Absolute Uri</returns>
        public static Uri GetUri(Type baseType, string relativePath)
        {
            Assembly oAssembly = Assembly.GetAssembly(baseType);
            AssemblyName oName = oAssembly.GetName();
            return new Uri(
                    String.Format(
                        "pack://application:,,,/{0};v{1};component/{2}",
                        oName.Name,
                        oName.Version.ToString(),
                        relativePath),
                    UriKind.Absolute);
        }
    }
}
