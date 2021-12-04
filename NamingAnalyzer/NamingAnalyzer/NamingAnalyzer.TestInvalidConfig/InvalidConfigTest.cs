using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VerifyCS = NamingAnalyzer.TestInvalidConfig.Verifiers.CSharpAnalyzerVerifier<NamingAnalyzer.NamingAnalyzer>;

namespace NamingAnalyzer.TestInvalidConfig
{
    [TestClass]
    public class InvalidConfigTest
    {
        [TestMethod]
        public async Task InvalidConfigShowsDiagnostic()
        {
            var test = @"
using System;

namespace ConsoleApplication1
{
    class TestClass
    {   
    }
}";
            var expected = VerifyCS.Diagnostic(NamingAnalyzer.MissingFileDiagnosticId);

            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }
    }
}
