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
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;

namespace AuthorizeAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AuthorizeAnalyzerCodeFixProvider)), Shared]
    public class AuthorizeAnalyzerCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(AuthorizeAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();

            var nodeAndTrivia = await this.GetCommentAsync(context.Document, diagnostic, context.CancellationToken);

            if (nodeAndTrivia == null)
            {
                return;
            }

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: CodeFixResources.CodeFixTitle,
                    createChangedDocument: c => UncommentAsync(context.Document, nodeAndTrivia.Value.Item1, nodeAndTrivia.Value.Item2, c),
                    equivalenceKey: nameof(CodeFixResources.CodeFixTitle)),
                diagnostic);
        }

        private async Task<(SyntaxNode, SyntaxTrivia)?> GetCommentAsync(Document document, Diagnostic diagnostic, CancellationToken ct)
        {
            var tree = await document.GetSyntaxTreeAsync(ct).ConfigureAwait(false);

            var trivia = (await tree.GetRootAsync(ct))
                .DescendantTrivia()
                .SingleOrDefault(t => t.IsKind(SyntaxKind.SingleLineCommentTrivia) && t.GetLocation() == diagnostic.Location);

            if (trivia == new SyntaxTrivia())
            {
                return null;
            }

            var node = (await tree.GetRootAsync(ct))
                .DescendantNodes()
                .SingleOrDefault(n => n.GetLeadingTrivia().Contains(trivia));

            if (node == null)
            {
                return null;
            }

            return (node, trivia);
        }

        private async Task<Document> UncommentAsync(Document document, SyntaxNode node, SyntaxTrivia trivia, CancellationToken ct)
        {
            // Produce a new solution
            var oldRoot = await document.GetSyntaxRootAsync(ct).ConfigureAwait(false);
            
            var authNode = SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("Authorize"));

            var gen = SyntaxGenerator.GetGenerator(document);

            var leadingTrivia = node.GetLeadingTrivia();

            var newLeadingTrivia = leadingTrivia.Remove(trivia).NormalizeWhitespace();

            var newNode = gen.InsertAttributes(node, 0, authNode)
                .WithoutLeadingTrivia().WithLeadingTrivia(newLeadingTrivia);

            var newRoot = gen.ReplaceNode(oldRoot, node, newNode);
            
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
