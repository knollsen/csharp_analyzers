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
        public async Task EmptySnippetShowsNoDiagnostic()
        {
            var test = @"";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task MethodInvocationWithoutTryCatchShowsDiagnostic()
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
        public async Task MethodInvocationWithTryCatchShowsDiagnostic()
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
        public async Task StaticMethodInvocationWithTryCatchShowsDiagnostic()
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

        [TestMethod]
        public async Task CatchingCorrectExceptionShowsNoDiagnostic()
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
        catch (ArgumentException) {}
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
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task CatchingBaseTypeOfExceptionShowsNoDiagnostic()
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
        catch (Exception) {}
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
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task CodeFixForAddingTryCatchWorks()
        {
            var test = @"using System;

public class SomeClass
{
    public void Execute()
    {
        this.ThrowsException();
    }

    /// <summary>
    /// Will throw an exception.
    /// </summary>
    /// <exception cref = ""ArgumentException"">Will throw this exception.</exception>
    private void ThrowsException()
    {
        throw new ArgumentException();
    }
}";
            var codeFix = @"using System;

public class SomeClass
{
    public void Execute()
    {
        try
        {
            this.ThrowsException();
        }
        catch (System.ArgumentException)
        {
        }
    }

    /// <summary>
    /// Will throw an exception.
    /// </summary>
    /// <exception cref = ""ArgumentException"">Will throw this exception.</exception>
    private void ThrowsException()
    {
        throw new ArgumentException();
    }
}";
            var expected = VerifyCS.Diagnostic(ExceptionAnalyzer.DiagnosticId).WithSpan(7, 9, 7, 31);
            await VerifyCS.VerifyCodeFixAsync(test, expected, codeFix);
        }

        [TestMethod]
        public async Task CodeFixForExistingTryCatchWorks()
        {
            var test = @"using System;

public class SomeClass
{
    public void Execute()
    {
        try
        {
            var x = 5;
            var f = ThrowsException();
        }
        catch (InvalidOperationException)
        {
        }
    }

    /// <summary>
    /// Will throw an exception.
    /// </summary>
    /// <exception cref = ""ArgumentException"">Will throw this exception.</exception>
    private int ThrowsException()
    {
        throw new ArgumentException();
        return 1;
    }
}";
            var codeFix = @"using System;

public class SomeClass
{
    public void Execute()
    {
        try
        {
            var x = 5;
            var f = ThrowsException();
        }
        catch (InvalidOperationException)
        {
        }
        catch (System.ArgumentException)
        {
        }
    }

    /// <summary>
    /// Will throw an exception.
    /// </summary>
    /// <exception cref = ""ArgumentException"">Will throw this exception.</exception>
    private int ThrowsException()
    {
        throw new ArgumentException();
        return 1;
    }
}";
            var expected = VerifyCS.Diagnostic(ExceptionAnalyzer.DiagnosticId).WithSpan(10, 21, 10, 38);
            await VerifyCS.VerifyCodeFixAsync(test, expected, codeFix);
        }
    }
}
