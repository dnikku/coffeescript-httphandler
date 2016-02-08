using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.ClearScript.V8;

namespace CoffeeScript
{
    internal class V8Compiler : ICoffee2Js
    {
        private readonly string _coffeeScript;

        public V8Compiler(string coffeeScript)
        {
            _coffeeScript = coffeeScript;
        }

        public CompileResult Compile(string scriptPath)
        {
            using (var engine = new V8ScriptEngine())
            {
                var output = new StringWriter();
                try
                {
                    engine.Execute(_coffeeScript);
                    output.WriteLine(@"/**
 * CoffeeScript-Handler: {0}
 * CoffeeScript Compiler: {1}
 * ScriptPath: {2} 
 * Compiled at: {3} 
 */",
                        FileVersionInfo.GetVersionInfo(GetType().Assembly.Location).FileVersion,
                        engine.Evaluate("CoffeeScript.VERSION"),
                        scriptPath,
                        DateTime.UtcNow);

                    var code = File.ReadAllText(scriptPath);
                    engine.AddHostObject("code", new {Str = code});
                    var result = engine.Evaluate("CoffeeScript.compile(code.Str, {no_wrap: true})");
                    output.WriteLine();
                    output.WriteLine(result);

                    return new CompileResult {Ok = true, Value = output.ToString()};
                }
                catch (Exception ex)
                {
                    output.WriteLine();
                    output.WriteLine(ex.Message);
                    return new CompileResult {Ok = false, Value = output.ToString()};
                }
            }
        }
    }
}
