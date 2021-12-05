using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace ExceptionAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ExceptionAnalyzer : DiagnosticAnalyzer
    {
        private const string Category = "Exceptions";

        public const string DiagnosticId = "ExceptionAnalyzer_DocumentedExceptionNotCaught";
        public const string PropertiesExceptionTypeKey = "exceptionType";

        private static readonly LocalizableString ReferenceTitle = new LocalizableResourceString(nameof(Resources.ReferenceTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString ReferenceMessageFormat = new LocalizableResourceString(nameof(Resources.ReferenceMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString ReferenceDescription = new LocalizableResourceString(nameof(Resources.ReferenceDescription), Resources.ResourceManager, typeof(Resources));

        private static readonly DiagnosticDescriptor ReferenceRule = new DiagnosticDescriptor(DiagnosticId, ReferenceTitle, ReferenceMessageFormat, Category, DiagnosticSeverity.Warning, true, ReferenceDescription, customTags:"exceptionName");

        // UncommentedExceptionFoundRule, MissingXmlDocumentationRule,
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(ReferenceRule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            
            // method invocation should catch documented exceptions
            context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;
            var semanticModel = context.SemanticModel;

            var method = semanticModel.GetSymbolInfo(invocation.Expression);

            if (method.Equals(default))
            {
                return;
            }

            var documentation = method.Symbol?.GetDocumentationCommentXml();

            if (string.IsNullOrEmpty(documentation))
            {
                return;
            }
            
            var documentedExceptions = GetExceptionsFromXmlComment(documentation).ToList();

            if (!documentedExceptions.Any())
            {
                return;
            }
            
            var tryCatch = (TryStatementSyntax)invocation.FirstAncestorOrSelf<SyntaxNode>(n => n.IsKind(SyntaxKind.TryStatement));
            var caughtExceptions = tryCatch?.Catches.Select(x => x.Declaration.Type).ToList();

            foreach (var documentedException in documentedExceptions)
            {
                var documentedExceptionSymbol = context.Compilation.GetTypeByMetadataName(documentedException);

                if (documentedExceptionSymbol == null)
                {
                    continue;
                }
                
                var caught = false;
                // check if documentedException inherits from any caughtException
                foreach (var caughtException in caughtExceptions ?? new List<TypeSyntax>())
                {
                    var caughtSymbolTypeInfo = semanticModel.GetTypeInfo(caughtException);
                    var caughtSymbol = context.Compilation.GetTypeByMetadataName(caughtSymbolTypeInfo.ConvertedType.ToString());
                    if (caughtSymbol != null && InheritsFrom(documentedExceptionSymbol, caughtSymbol))
                    {
                        caught = true;
                        break;
                    }
                }

                if (!caught)
                {
                    // give documented exception type as property
                    var properties = new Dictionary<string, string> { { PropertiesExceptionTypeKey, documentedException } }.ToImmutableDictionary();

                    context.ReportDiagnostic(Diagnostic.Create(ReferenceRule, invocation.GetLocation(), properties, invocation.ToString(), documentedException));
                }
            }
        }

        private static IEnumerable<string> GetExceptionsFromXmlComment(string xmlComment)
        {
            var doc = new XmlDocument();
            doc.LoadXml(xmlComment);

            var commentedExceptions = doc.GetElementsByTagName("exception");

            var result = new List<string>();
            foreach (var commentedException in commentedExceptions.Cast<XmlNode>())
            {
                var type = commentedException?.Attributes?.GetNamedItem("cref");

                var typeName = type?.InnerText?.Replace("T:", string.Empty);

                if (typeName != null)
                {
                    result.Add(typeName);
                }
            }

            return result;
        }

        private static bool InheritsFrom(INamedTypeSymbol symbol, INamedTypeSymbol type)
        {
            var baseType = symbol;
            while (baseType != null)
            {
                if (SymbolEqualityComparer.Default.Equals(type, baseType))
                {
                    return true;
                }

                baseType = baseType.BaseType;
            }

            return false;
        }
    }
}
