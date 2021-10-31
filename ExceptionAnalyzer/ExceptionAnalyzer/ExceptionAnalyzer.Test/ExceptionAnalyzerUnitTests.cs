﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using VerifyCS = ExceptionAnalyzer.Test.CSharpCodeFixVerifier<
    ExceptionAnalyzer.ExceptionAnalyzer,
    ExceptionAnalyzer.ExceptionAnalyzerCodeFixProvider>;

namespace ExceptionAnalyzer.Test
{
    [TestClass]
    public class ExceptionAnalyzerUnitTest
    {
        //No diagnostics expected to show up
        [TestMethod]
        public async Task TestMethod1()
        {
            var test = @"";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task Test()
        {
            var test = @"using System;

namespace AnalyzerTestApplication
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(""Hello World!""); 
            var x = Ret();
            try {
                Throws();
            }
            catch (Exception) {}
            // var c = new Class1();
        }

        /// <summary>
        /// Method that throws exceptions.
        /// </summary>
        /// <exception cref=""ArgumentException"">In case of failure.</exception>
        private static void Throws()
        {
            throw new ArgumentException(""Failure"");
        }

        /// <summary> returns 1</summary>
        public static int Ret()
        {
            return 1;
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test, DiagnosticResult.CompilerWarning("ExceptionAnalyzer_MissingXmlDocumentation").WithSpan(7, 21, 7, 25).WithArguments("Main"));
        }

        //Diagnostic and CodeFix both triggered and checked for
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
        class {|#0:TypeName|}
        {   
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
        class TYPENAME
        {   
        }
    }";

            var expected = VerifyCS.Diagnostic("ExceptionAnalyzer").WithLocation(0).WithArguments("TypeName");
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
        }
    }
}
