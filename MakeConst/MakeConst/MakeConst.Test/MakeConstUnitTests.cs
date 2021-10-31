using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using VerifyCS = MakeConst.Test.CSharpCodeFixVerifier<
    MakeConst.MakeConstAnalyzer,
    MakeConst.MakeConstCodeFixProvider>;

namespace MakeConst.Test
{
    [TestClass]
    public class MakeConstUnitTest
    {
        //No diagnostics expected to show up
        [TestMethod]
        public async Task TestMethod1()
        {
            var test = @"";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task TestMethod2()
        {
            var test = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1
    {
        class Program
        {   
            public static void Main(string[] args) {
                var x = 5;
                x = 4;
                const int y = 3;
                
                Console.WriteLine(x + y);
            }
        }
    }";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        //Diagnostic and CodeFix both triggered and checked for
        [TestMethod]
        public async Task TestMethod3()
        {
            var test = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1
    {
        class Program
        {   
            public static void Main(string[] args) {
                var x = 5;
                const int y = 3;
                
                Console.WriteLine(x + y);
            }
        }
    }";

            var fixtest = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1
    {
        class Program
        {   
            public static void Main(string[] args) {
                const x = 5;
                const int y = 3;
                
                Console.WriteLine(x + y);
            }
        }
    }";
            await VerifyCS.VerifyAnalyzerAsync(test, DiagnosticResult.CompilerWarning("/0/Test0.cs(14,17): warning MakeConst: Variable x can be made const"));

            var expected = VerifyCS.Diagnostic("MakeConst");
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
        }
    }
}
