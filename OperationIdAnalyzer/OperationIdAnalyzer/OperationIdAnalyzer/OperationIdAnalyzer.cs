using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace OperationIdAnalyzer
{
    using Microsoft.AspNetCore.Mvc;
    using Swashbuckle.AspNetCore.Annotations;
    using System.Xml.Linq;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class OperationIdAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "OperationIdAnalyzer";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Naming";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // TODO: Consider registering other actions that act on syntax instead of or in addition to symbols
            // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
            context.RegisterSymbolAction(AnalyzeType, SymbolKind.NamedType);
        }

        private static string ApiControllerAttributeQualifiedName = typeof(ApiControllerAttribute).FullName;

        private static string HttpGetAttributeQualifiedName = typeof(HttpGetAttribute).FullName;
        private static string HttpPostAttributeQualifiedName = typeof(HttpPostAttribute).FullName;
        private static string HttpPutAttributeQualifiedName = typeof(HttpPutAttribute).FullName;
        private static string HttpPatchAttributeQualifiedName = typeof(HttpPatchAttribute).FullName;
        private static string HttpDeleteAttributeQualifiedName = typeof(HttpDeleteAttribute).FullName;

        private static string SwaggerOperationAttributeQualifiedName = typeof(SwaggerOperationAttribute).FullName;

        private static void AnalyzeType(SymbolAnalysisContext context)
        {
            var namedTypeSymbol = (INamedTypeSymbol)context.Symbol;

            var typeAttributes = namedTypeSymbol.GetAttributes();

            if (!typeAttributes.Any(a => a.AttributeClass.ToDisplayString() == ApiControllerAttributeQualifiedName))
            {
                // the class we're looking at is not a http controller
                return;
            }
            
            // we are analyzing a controller class
            // should now look for endpoint methods
            var members = namedTypeSymbol.GetMembers();

            var methods = members.Where(m => m.Kind == SymbolKind.Method);

            foreach (var symbol in methods)
            {
                var method = (IMethodSymbol)symbol;

                var methodAttributes = method.GetAttributes();

                if (!methodAttributes.Any(m =>
                        m.AttributeClass.ToDisplayString() == HttpGetAttributeQualifiedName ||
                        m.AttributeClass.ToDisplayString() == HttpPostAttributeQualifiedName ||
                        m.AttributeClass.ToDisplayString() == HttpPutAttributeQualifiedName ||
                        m.AttributeClass.ToDisplayString() == HttpPatchAttributeQualifiedName ||
                        m.AttributeClass.ToDisplayString() == HttpDeleteAttributeQualifiedName))
                {
                    // it is not a http method
                    continue;
                }

                if (methodAttributes.Any(m =>
                        m.AttributeClass.ToDisplayString() == SwaggerOperationAttributeQualifiedName))
                {
                    // there is a swagger operation attribute!
                    continue;
                }

                // there is no swagger operation attribute -> show a diagnostic here
                var diagnostic = Diagnostic.Create(Rule, method.Locations.First(), method.Name);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
