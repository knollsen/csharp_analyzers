using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace ImportAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ImportAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "ImportAnalyzer";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Imports";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeUsingDirective, SyntaxKind.UsingDirective);
        }

        private void AnalyzeUsingDirective(SyntaxNodeAnalysisContext context)
        {
            var usingDirective = (UsingDirectiveSyntax)context.Node;

            var importedNamespace = usingDirective.Name.ToString();

            var parent = usingDirective.Parent;

            var namespaceDeclaration = (NamespaceDeclarationSyntax)parent.DescendantNodesAndSelf().SingleOrDefault(x => x is NamespaceDeclarationSyntax);

            if (namespaceDeclaration == null)
            {
                return;
            }

            var ownNamespace = namespaceDeclaration.Name.ToString();

            if (importedNamespace.StartsWith(ownNamespace))
            {
                var location = usingDirective.GetLocation();

                var diagnostic = Diagnostic.Create(Rule, location, usingDirective.ToString());

                context.ReportDiagnostic(diagnostic);
            }
        } 
    }
}
