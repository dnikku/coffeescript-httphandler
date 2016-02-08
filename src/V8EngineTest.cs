using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace CoffeeScript
{
    [TestFixture]
    public class V8EngineTest
    {
        [Test]
        public void TestOne()
        {
            var compiler = new V8Compiler(CoffeeScriptHttpHandler.GetCoffeeScript());
            var tt =
                compiler.Compile(@"F:\dnikku\projects\wp\ms40\src\Web\MatterSpace.Web\mvc\app2\members\app_admin.coffee");
            Assert.AreEqual(tt.Value, "");
        }
    }
}
