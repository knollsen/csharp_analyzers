using Microsoft.VisualStudio.TestTools.UnitTesting;
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
        public async Task Test2()
        {
            var test = @"
using System;

public class SomeClass
{
    public void Execute()
    {
        this.ThrowsException();
    }

    /// <summary>
    /// Will throw an exception.
    /// </summary>
    /// <exception cref=""ArgumentException"">Will throw this exception.</exception>
    private void ThrowsException()
    {
        throw new ArgumentException();
    }
}";
            var expected = VerifyCS.Diagnostic(ExceptionAnalyzer.DiagnosticId).WithSpan(8, 9, 8, 31);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task Test3()
        {
            var test = @"
using System;

public class SomeClass
{
    public void Execute()
    {
        try
        {
            this.ThrowsException();
        }
        catch (InvalidOperationException) {}
    }

    /// <summary>
    /// Will throw an exception.
    /// </summary>
    /// <exception cref=""ArgumentException"">Will throw this exception.</exception>
    private void ThrowsException()
    {
        throw new ArgumentException();
    }
}";
            var expected = VerifyCS.Diagnostic(ExceptionAnalyzer.DiagnosticId).WithSpan(10, 13, 10, 35);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task Test4()
        {
            var test = @"
using System;

public class SomeClass
{
    public void Execute()
    {
        try
        {
            ThrowsException();
        }
        catch (InvalidOperationException) {}
    }

    /// <summary>
    /// Will throw an exception.
    /// </summary>
    /// <exception cref=""ArgumentException"">Will throw this exception.</exception>
    private static void ThrowsException()
    {
        throw new ArgumentException();
    }
}";
            var expected = VerifyCS.Diagnostic(ExceptionAnalyzer.DiagnosticId).WithSpan(10, 13, 10, 30);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }
    }

}
