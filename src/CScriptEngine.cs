using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace CoffeeScript
{
    public class CScriptEngine : ICoffee2Js
    {
        public CScriptEngine(string coffeScript)
        {
            ExePath = @"c:\Windows\System32\cscript.exe";
            CoffeeScript = coffeScript;
            InjectVersion = true;

            var exepath = CoffeeScriptHttpHandler.GetConfig("coffee-script.cscriptPath", null);
            if (exepath != null)
                ExePath = exepath;

            InjectVersion = CoffeeScriptHttpHandler.GetConfig("coffee-script.injectVersion", true);
        }

        public string CoffeeScript { get; private set; }
        public string ExePath { get; set; }
        public bool InjectVersion { get; set; }

        private string _computedVersion;
        public string Version
        {
            get
            {
                if (_computedVersion == null)
                {
                    var coffeeVersion = new Regex("VERSION=\"(?<vv>.+?)\"").Match(CoffeeScript).Groups["vv"].Value;

                    _computedVersion = string.Format("{{NET:'{0}', CoffeScript:'{1}', cscript:'{2}'}}",
                        Environment.Version + "-" + (IntPtr.Size == 4 ? "x86" : "x64"),
                        coffeeVersion,
                        ExePath != null ? FileVersionInfo.GetVersionInfo(ExePath).FileVersion : "N/A");
                }
                return _computedVersion;
            }
        }

        public CompileResult Compile(string scriptPath)
        {
            var writer = new StringWriter();
            writer.Write(CoffeeScript);
            writer.WriteLine();
            writer.WriteLine();
            writer.Write(@"

function printDebug(str) {
    WScript.Echo('/**\n  ' + str + '\n**/');
}

function loadFile(scriptPath) {
    // use adodb.stream to handle utf-8 files. 
    // see: http://stackoverflow.com/questions/13851473/read-utf-8-text-file-in-vbscript
    //var fso = new ActiveXObject('Scripting.FileSystemObject');
    //return fso.OpenTextFile(script_path, 1).ReadAll();

    var stream = new ActiveXObject('ADODB.Stream');
    stream.Charset = 'utf-8'
    stream.Open
    stream.LoadFromFile(scriptPath);
    return stream.ReadText();
}

function compileFile(scriptPath) {
    var code = loadFile(scriptPath);
    //printDebug('loadFile script=' + scriptPath);
    //printDebug(code);
 
    return CoffeeScript.compile(code, {no_wrap: true});
}

if (<inject-version>){
    WScript.Echo(
        '/**'
    + '\n * CoffeeScript-Handler: <coffeescript-handler-version>'
    + '\n * CoffeeScript Compiler: ' + CoffeeScript.VERSION 
    + '\n * Compiled at: ' + new Date() 
    + '\n */');
}

WScript.Echo(compileFile('<script-path>'));
".Replace("<script-path>", scriptPath.Replace("\\", "/"))
 .Replace("<inject-version>", InjectVersion.ToString().ToLowerInvariant())
 .Replace("<coffeescript-handler-version>", FileVersionInfo.GetVersionInfo(GetType().Assembly.Location).FileVersion)
 );
            writer.WriteLine();

            return RunEngine(writer.ToString());
        }

        private CompileResult RunEngine(string code)
        {
            using (var tmpFile = new TmpJsFile())
            {
                File.WriteAllText(tmpFile.FilePath, code);

                var cmd = string.Format("\"{0}\" //NoLogo", tmpFile.FilePath);
                var info = new ProcessStartInfo(ExePath, cmd);
                info.UseShellExecute = false;
                info.RedirectStandardOutput = true;
                info.RedirectStandardError = true;
                using (var process = Process.Start(info))
                {
                    var stdOut = process.StandardOutput.ReadToEnd();
                    var stdErr = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    return (!string.IsNullOrEmpty(stdErr))
                        ? new CompileResult {Ok = false, Value = stdOut + stdErr}
                        : new CompileResult {Ok = true, Value = stdOut};
                }
            }
        }

        class TmpJsFile : IDisposable
        {
            public TmpJsFile()
            {
                var tmpDir = Path.Combine(Path.GetTempPath(), "coffee-script");
                Directory.CreateDirectory(tmpDir);

                FilePath = Path.Combine(tmpDir, Path.GetRandomFileName() + ".js");
            }
            public string FilePath { get; private set; }

            public void Dispose()
            {
                File.Delete(FilePath);
            }
        }
    }
}
