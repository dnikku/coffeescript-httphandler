using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoffeeScript
{
    public struct CompileResult
    {
        public bool Ok;
        public string Value;
    }

    public interface ICoffee2Js
    {
        CompileResult Compile(string scriptPath);
    }
}
