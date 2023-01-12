using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Swashbuckle.AspNetCore.Annotations;
using System.Threading.Tasks;
using VerifyCS = OperationIdAnalyzer.Test.CSharpCodeFixVerifier<
    OperationIdAnalyzer.OperationIdAnalyzer,
    OperationIdAnalyzer.OperationIdAnalyzerCodeFixProvider>;

namespace OperationIdAnalyzer.Test
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.CodeAnalysis.Testing;
    using Swashbuckle.AspNetCore.Annotations;

    [TestClass]
    public class OperationIdAnalyzerUnitTest
    {
        [TestMethod]
        public async Task EmptySnippetShowsNoDiagnostic()
        {
            var test = @"";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task MissingOperationIdAttributeOnHttpGetShowsDiagnostic()
        {
            var test = @"
namespace Microsoft.AspNetCore.Mvc
{
    using System;

    class HttpGetAttribute : Attribute
    {

    }

    class ApiControllerAttribute : Attribute
    {

    }
}

namespace Swashbuckle.AspNetCore.Annotations
{
    using System;

    class SwaggerOperationAttribute : Attribute
    {
        public string OperationId { get; set; }
    }
}

namespace Test
{
    using System;
    using Microsoft.AspNetCore.Mvc;
    using Swashbuckle.AspNetCore.Annotations;

    [ApiController]
    class TestController
    {
        [HttpGet]
        public string Get() { 
            return string.Empty; 
        }
    }
}";
            var expected = VerifyCS.Diagnostic().WithSpan(37, 23, 37, 26).WithArguments("Get");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task ExistingOperationIdAttributeOnHttpGetShowsNoDiagnostic()
        {
            var test = @"
namespace Microsoft.AspNetCore.Mvc
{
    using System;

    class HttpGetAttribute : Attribute
    {

    }

    class ApiControllerAttribute : Attribute
    {

    }
}

namespace Swashbuckle.AspNetCore.Annotations
{
    using System;

    class SwaggerOperationAttribute : Attribute
    {
        public string OperationId { get; set; }
    }
}

namespace Test
{
    using System;
    using Microsoft.AspNetCore.Mvc;
    using Swashbuckle.AspNetCore.Annotations;

    [ApiController]
    class TestController
    {
        [HttpGet]
        [SwaggerOperation(OperationId = nameof(Get))]
        public string Get() { 
            return string.Empty; 
        }
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task MissingOperationIdAttributeOnHttpPostShowsDiagnostic()
        {
            var test = @"
namespace Microsoft.AspNetCore.Mvc
{
    using System;

    class HttpPostAttribute : Attribute
    {

    }

    class ApiControllerAttribute : Attribute
    {

    }
}

namespace Swashbuckle.AspNetCore.Annotations
{
    using System;

    class SwaggerOperationAttribute : Attribute
    {
        public string OperationId { get; set; }
    }
}

namespace Test
{
    using System;
    using Microsoft.AspNetCore.Mvc;
    using Swashbuckle.AspNetCore.Annotations;

    [ApiController]
    class TestController
    {
        [HttpPost]
        public string Get() { 
            return string.Empty; 
        }
    }
}";

            var expected = VerifyCS.Diagnostic().WithSpan(37, 23, 37, 26).WithArguments("Get");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }
    }
}
