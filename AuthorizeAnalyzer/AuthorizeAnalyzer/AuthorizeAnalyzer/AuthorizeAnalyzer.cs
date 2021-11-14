using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace AuthorizeAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AuthorizeAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "AuthorizeAnalyzer";

        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Naming";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxTreeAction(HandleSyntaxTree);
        }

        private void HandleSyntaxTree(SyntaxTreeAnalysisContext context)
        {
            SyntaxNode root = context.Tree.GetCompilationUnitRoot(context.CancellationToken);
            var descendantTrivia = root.DescendantTrivia();
            var commentNodes = root.DescendantTrivia().Where(x => x.IsKind(SyntaxKind.SingleLineCommentTrivia)).ToList();

            if (!commentNodes.Any())
            {
                return;
            }

            foreach (var node in commentNodes)
            {
                var commentText = node.ToString().TrimStart('/');

                if (!commentText.TrimStart().StartsWith("[Authorize]"))
                {
                    continue;
                }

                var diagnostic = Diagnostic.Create(Rule, node.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
