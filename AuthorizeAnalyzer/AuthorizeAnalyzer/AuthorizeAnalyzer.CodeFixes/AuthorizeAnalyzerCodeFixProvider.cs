using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AuthorizeAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AuthorizeAnalyzerCodeFixProvider)), Shared]
    public class AuthorizeAnalyzerCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(AuthorizeAnalyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();

            var trivia = await this.GetCommentAsync(context.Document, diagnostic, context.CancellationToken);

            if (trivia == null)
            {
                return;
            }

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: CodeFixResources.CodeFixTitle,
                    createChangedDocument: c => UncommentAsync(context.Document, trivia, c),
                    equivalenceKey: nameof(CodeFixResources.CodeFixTitle)),
                diagnostic);
        }

        private async Task<SyntaxTrivia> GetCommentAsync(Document document, Diagnostic diagnostic, CancellationToken ct)
        {
            var tree = await document.GetSyntaxTreeAsync(ct).ConfigureAwait(false);

            var diagnosticSpan = diagnostic.Location;

            var trivia = (await tree.GetRootAsync(ct)).DescendantTrivia(x =>
                x.IsKind(SyntaxKind.SingleLineCommentTrivia) && x.GetLocation() == diagnosticSpan).SingleOrDefault();

            return trivia;
        }

        private async Task<Document> UncommentAsync(Document document, SyntaxTrivia location, CancellationToken cancellationToken)
        {
            /*
            var semanticModel = await document.GetSyntaxTreeAsync(cancellationToken);

            // Compute new uppercase name.
            var identifierToken = trivia.Span.ToString();
            var newLine = identifierToken.TrimStart('/').TrimStart();

            // Get the symbol representing the type to be renamed.
            
            var typeSymbol = semanticModel.Get
            semanticModel.
            */
            // Produce a new solution that has all references to that type renamed, including the declaration.
            var oldRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            //SyntaxNode newRoot = oldRoot.ReplaceNode(typeDecl, typeDecl);

            return document.WithSyntaxRoot(oldRoot);

        }
    }
}
