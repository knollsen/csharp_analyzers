using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using VerifyCS = AuthorizeAnalyzer.Test.CSharpCodeFixVerifier<
    AuthorizeAnalyzer.AuthorizeAnalyzer,
    AuthorizeAnalyzer.AuthorizeAnalyzerCodeFixProvider>;

namespace AuthorizeAnalyzer.Test
{
    [TestClass]
    public class AuthorizeAnalyzerUnitTest
    {
        [TestMethod]
        public async Task EmptySnippetShowsNoDiagnostic()
        {
            var test = @"";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }
        
        [TestMethod]
        public async Task UncommentedAuthorizeAttributeShowsDiagnostic()
        {
            var test = @"
using System;

namespace ConsoleApplication1
{
    // [Authorize]
    class Test
    {   
    }
}";
            
            var expected = VerifyCS.Diagnostic(AuthorizeAnalyzer.DiagnosticId).WithSpan(6, 5, 6, 19);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task CodeFixUncommentsAuthorizeAttribute()
        {
            var test = @"
using System;

namespace ConsoleApplication1
{
    // this is a comment
    // [Authorize]
    class Test
    {   
    }

    class AuthorizeAttribute : Attribute {}
}";
            var fix = @"
using System;

namespace ConsoleApplication1
{
    // this is a comment
    [Authorize]
    class Test
    {   
    }

    class AuthorizeAttribute : Attribute {}
}";

            var expected = VerifyCS.Diagnostic(AuthorizeAnalyzer.DiagnosticId).WithSpan(7, 5, 7, 19);
            await VerifyCS.VerifyCodeFixAsync(test, expected, fix);
        }

        [TestMethod]
        public async Task CodeFixRemovesCorrectComment()
        {
            var test = @"
using System;

namespace ConsoleApplication1
{
    // [Authorize]
    // this is a comment
    class Test
    {   
    }

    class AuthorizeAttribute : Attribute {}
}";
            var fix = @"
using System;

namespace ConsoleApplication1
{
    // this is a comment
    [Authorize]
    class Test
    {   
    }

    class AuthorizeAttribute : Attribute {}
}";

            var expected = VerifyCS.Diagnostic(AuthorizeAnalyzer.DiagnosticId).WithSpan(6, 5, 6, 19);
            await VerifyCS.VerifyCodeFixAsync(test, expected, fix);
        }
    }
}
