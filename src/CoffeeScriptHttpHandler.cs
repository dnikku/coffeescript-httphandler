using System.Globalization;
using System.IO;
using System.Web;
using System.Configuration;

namespace CoffeeScript
{
    /// <summary>
    /// Configs:
    ///  coffee-script.path - absolute path to the coffee-script.js compiler, default one is 1.7.1
    ///  coffee-script.cscriptPath - absolute path to windows cscript.exe, default: c:\windows\system32\cscript.exe
    ///  coffee-script.injectVersion - flag to inject in header of compiled js file, the coffee-script.js version used; default true
    ///  coffee-script.enableCache - flag to make cacheable with dependency of original .coffee file, default true
    /// </summary>
    public class CoffeeScriptHttpHandler : IHttpHandler
    {
        private readonly bool _enableCache;
        readonly ICoffee2Js _engine;
        public CoffeeScriptHttpHandler()
        {
            _engine = new V8Compiler(GetCoffeeScript());

            _enableCache = GetConfig("coffee-script.enableCache", true);
        }

        public void ProcessRequest(HttpContext context)
        {
            var coffeescriptFile = context.Request.PhysicalPath;
            var cachejsFile = coffeescriptFile + ".jscoffee";

            context.Response.Cache.SetCacheability(HttpCacheability.NoCache);
            context.Response.ContentType = "application/javascript";
  
            if (_enableCache &&
                File.Exists(cachejsFile) && new FileInfo(cachejsFile).LastWriteTimeUtc == new FileInfo(coffeescriptFile).LastWriteTimeUtc)
            {
                var jscript = File.ReadAllText(cachejsFile);
                context.Response.Write(jscript);
            }
            else
            {
                var jscript = _engine.Compile(coffeescriptFile);
                if (jscript.Ok)
                {
                    context.Response.Write(jscript.Value);
                    File.WriteAllText(cachejsFile, jscript.Value);
                    new FileInfo(cachejsFile).LastWriteTimeUtc = new FileInfo(coffeescriptFile).LastWriteTimeUtc;
                }
                else
                {
                    context.Response.Write(jscript.Value);
                    context.Response.StatusCode = 250;
                }
            }
        }

        public bool IsReusable
        {
            get { return true; }
        }

        internal static string GetCoffeeScript()
        {
            var scriptPath = GetConfig("coffee-script.path", null);
            return scriptPath != null ? Loader.FromFile(scriptPath) : Loader.Default();
        }

        internal static string GetConfig(string keyName, string defaultValue)
        {
            var result = ConfigurationManager.AppSettings[keyName];
            return result ?? defaultValue;
        }

        internal static bool GetConfig(string keyName, bool defaultValue)
        {
            return bool.Parse(GetConfig(keyName, defaultValue.ToString()));
        }

        internal static int GetConfig(string keyName, int defaultValue)
        {
            return int.Parse(GetConfig(keyName, defaultValue.ToString(CultureInfo.InvariantCulture)));
        }
    }
}
