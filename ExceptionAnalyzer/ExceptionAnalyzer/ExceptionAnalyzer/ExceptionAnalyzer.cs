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
        /*
        public const string UncommentedExceptionId = "ExceptionAnalyzer_UncommentedException";

        private static readonly LocalizableString UncommentedExceptionTitle = new LocalizableResourceString(nameof(Resources.UncommentedExceptionTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString UncommentedExceptionMessageFormat = new LocalizableResourceString(nameof(Resources.UncommentedExcetionMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString UncommentedExceptionDescription = new LocalizableResourceString(nameof(Resources.UncommentedExceptionDescription), Resources.ResourceManager,  typeof(Resources));

        private static readonly DiagnosticDescriptor UncommentedExceptionFoundRule = new DiagnosticDescriptor(UncommentedExceptionId, UncommentedExceptionTitle, UncommentedExceptionMessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: UncommentedExceptionDescription);

        public const string MissingXmlDocumentationId = "ExceptionAnalyzer_MissingXmlDocumentation";

        private static readonly LocalizableString MissingXmlDocumentationTitle = new LocalizableResourceString(nameof(Resources.MissingXmlCommentTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MissingXmlDocumentationMessageFormat = new LocalizableResourceString(nameof(Resources.MissingXmlCommentMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MissingXmlDocumentationDescription = new LocalizableResourceString(nameof(Resources.MissingXmlCommentDescription), Resources.ResourceManager, typeof(Resources));

        private static readonly DiagnosticDescriptor MissingXmlDocumentationRule = new DiagnosticDescriptor(MissingXmlDocumentationId, MissingXmlDocumentationTitle, MissingXmlDocumentationMessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: false, description: MissingXmlDocumentationDescription);
        */

        public const string DiagnosticId = "ExceptionAnalyzer_DocumentedExceptionNotCaught";

        private static readonly LocalizableString ReferenceTitle = new LocalizableResourceString(nameof(Resources.ReferenceTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString ReferenceMessageFormat = new LocalizableResourceString(nameof(Resources.ReferenceMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString ReferenceDescription = new LocalizableResourceString(nameof(Resources.ReferenceDescription), Resources.ResourceManager, typeof(Resources));

        private static readonly DiagnosticDescriptor ReferenceRule = new DiagnosticDescriptor(DiagnosticId, ReferenceTitle, ReferenceMessageFormat, Category, DiagnosticSeverity.Error, true, ReferenceDescription);

        // UncommentedExceptionFoundRule, MissingXmlDocumentationRule,
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(ReferenceRule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            
            // method definition should document thrown exceptions
            //context.RegisterSymbolAction(AnalyzeMethodDefinition, SymbolKind.Method);
            // method invocation should catch documented exceptions
            context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;
            var semanticModel = context.SemanticModel;

            var method = semanticModel.GetSymbolInfo(invocation.Expression);

            if (method.Equals(new SymbolInfo()))
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
                    context.ReportDiagnostic(Diagnostic.Create(ReferenceRule, invocation.GetLocation()));
                }
            }
        }


        /*
        private static void AnalyzeMethodDefinition(SymbolAnalysisContext context)
        {
            var method = (IMethodSymbol)context.Symbol;

            var reference = method.DeclaringSyntaxReferences.SingleOrDefault();
            
            var throws = reference?.GetSyntax()?.DescendantNodes()?.Where(x => x.IsKind(SyntaxKind.ThrowStatement));

            var thrownExceptions = new List<TypeSyntax>();
            if (throws != null)
            {
                foreach (var throwStatement in throws)
                {
                    var statement = (ThrowStatementSyntax)throwStatement;

                    var creation = (ObjectCreationExpressionSyntax)statement.ChildNodes().SingleOrDefault(x => x.IsKind(SyntaxKind.ObjectCreationExpression));
                    var exceptionType = creation?.Type;

                    if (exceptionType != null)
                    {
                        thrownExceptions.Add(exceptionType);
                    }
                }
            }
            
            var xmlComment = method.GetDocumentationCommentXml();

            if (string.IsNullOrEmpty(xmlComment))
            {
                context.ReportDiagnostic(Diagnostic.Create(MissingXmlDocumentationRule, context.Symbol.Locations.First(), method.Name));
            }
            else
            {
                var documentedExceptions = GetExceptionsFromXmlComment(xmlComment);

                foreach (var documentedException in documentedExceptions)
                {
                    var match = thrownExceptions.SingleOrDefault(x => documentedException.EndsWith(x.ToFullString()));

                    thrownExceptions.Remove(match);
                }

                foreach (var uncommentedException in thrownExceptions)
                {
                    context.ReportDiagnostic(Diagnostic.Create(UncommentedExceptionFoundRule, uncommentedException.GetLocation(), method.Name));
                }
            }
        }
        */
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

        private static bool InheritsFrom(INamedTypeSymbol symbol, ITypeSymbol type)
        {
            var baseType = symbol.BaseType;
            while (baseType != null)
            {
                if (type.Equals(baseType))
                    return true;

                baseType = baseType.BaseType;
            }

            return false;
        }
    }
}
