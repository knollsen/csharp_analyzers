using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using VerifyCS = NamingAnalyzer.Test.Verifiers.CSharpAnalyzerVerifier<NamingAnalyzer.NamingAnalyzer>;

namespace NamingAnalyzer.Test
{
    [TestClass]
    public class NamingAnalyzerUnitTest
    {
        //No diagnostics expected to show up
        [TestMethod]
        public async Task EmptySnippetShowsNoDiagnostic()
        {
            var test = @"";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }
        
        [TestMethod]
        public async Task ClassNameDisallowedTermShowsDiagnostic()
        {
            var test = @"
using System;

namespace ConsoleApplication1
{
    class TestClass
    {   
    }
}";
            var expected = VerifyCS.Diagnostic(NamingAnalyzer.DisallowedTermsDiagnosticId)
                .WithSpan(6, 11, 6, 20)
                .WithArguments("TestClass", "Class");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task ClassNameWithNoDisallowedTermsHasNoDiagnostic()
        {
            var test = @"
using System;

namespace ConsoleApplication1
{
    class CorrectTest
    {
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task DisallowedTermCanBeAtTheStart()
        {
            var test = @"
using System;

namespace ConsoleApplication1
{
    class ClassTest
    {   
    }
}";
            var expected = VerifyCS.Diagnostic(NamingAnalyzer.DisallowedTermsDiagnosticId)
                .WithSpan(6, 11, 6, 20)
                .WithArguments("ClassTest", "Class");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task DisallowedTermCanBeAtInTheMiddle()
        {
            var test = @"
using System;

namespace ConsoleApplication1
{
    class SomeClassTest
    {   
    }
}";
            var expected = VerifyCS.Diagnostic(NamingAnalyzer.DisallowedTermsDiagnosticId)
                .WithSpan(6, 11, 6, 24)
                .WithArguments("SomeClassTest", "Class");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task DisallowedMemberSuffixesShowDiagnostic()
        {
            var test = @"
using System;

namespace ConsoleApplication1
{
    class TestFactory
    {
        private IRepository repository2 { get; }
        private readonly IRepository repository;
    }

    interface IRepository {}
}";
            var propertyDiagnostic = VerifyCS.Diagnostic(NamingAnalyzer.DisallowedSuffixDiagnosticId)
                .WithSpan(8, 9, 8, 49).WithArguments("TestFactory", "Repository");

            var fieldDiagnostic = VerifyCS.Diagnostic(NamingAnalyzer.DisallowedSuffixDiagnosticId)
                .WithSpan(9, 9, 9, 49).WithArguments("TestFactory", "Repository");

            await VerifyCS.VerifyAnalyzerAsync(test, propertyDiagnostic, fieldDiagnostic);
        }

        [TestMethod]
        public async Task AbstractClassWithDisallowedMemberSuffixesShowDiagnostic()
        {
            var test = @"
using System;

namespace ConsoleApplication1
{
    abstract class TestFactory
    {
        private readonly IRepository repository;
    }

    interface IRepository {}
}";
            var fieldDiagnostic = VerifyCS.Diagnostic(NamingAnalyzer.DisallowedSuffixDiagnosticId)
                .WithSpan(8, 9, 8, 49).WithArguments("TestFactory", "Repository");

            await VerifyCS.VerifyAnalyzerAsync(test, fieldDiagnostic);
        }
    }
}
