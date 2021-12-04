using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VerifyCS = NamingAnalyzer.TestEmptyConfig.Verifiers.CSharpAnalyzerVerifier<NamingAnalyzer.NamingAnalyzer>;

namespace NamingAnalyzer.TestEmptyConfig
{
    [TestClass]
    public class EmptyConfigTest
    {
        [TestMethod]
        public async Task EmptyConfigShowsDiagnostic()
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
