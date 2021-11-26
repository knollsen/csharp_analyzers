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

            var syntaxNodes = (await tree.GetRootAsync(ct)).DescendantNodesAndSelf();

            var node = syntaxNodes
                .SingleOrDefault(n =>
                {
                    if (!(n is ClassDeclarationSyntax || n is MethodDeclarationSyntax))
                    {
                        return false;
                    }

                    if (n.GetLeadingTrivia().Contains(trivia))
                    {
                        return true;
                    }

                    if (n.ChildTokens().Any(x => x.LeadingTrivia.Contains(trivia)))
                    {
                        return true;
                    }

                    return false;
                });

            if (node == null)
            {
                return null;
            }

            return (node, trivia);
        }

        private async Task<Document> UncommentAsync(Document document, SyntaxNode node, SyntaxTrivia trivia, CancellationToken ct)
        {
            var nodeLeadingTrivia = node.GetLeadingTrivia();

            SyntaxNode nodeWithCommentRemoved;
            if (nodeLeadingTrivia.Contains(trivia))
            {
                var newLeadingTrivia = nodeLeadingTrivia.Remove(trivia).NormalizeWhitespace();

                nodeWithCommentRemoved = node.WithLeadingTrivia(newLeadingTrivia);
            }
            else
            {
                var token = node.ChildTokens().SingleOrDefault(x => x.LeadingTrivia.Contains(trivia));
                
                var tokenLeadingTrivia = token.LeadingTrivia;
                var tokenLeadingTriviaWithoutComment = tokenLeadingTrivia.Remove(trivia);

                var newToken = token.WithLeadingTrivia(tokenLeadingTriviaWithoutComment);

                nodeWithCommentRemoved = node.ReplaceToken(token, newToken);
            }

            var gen = SyntaxGenerator.GetGenerator(document);

            var authNode = SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("Authorize"));

            var finalNode = gen.InsertAttributes(nodeWithCommentRemoved, 0, authNode);

            var oldRoot = await document.GetSyntaxRootAsync(ct).ConfigureAwait(false);

            var newRoot = gen.ReplaceNode(oldRoot, node, finalNode);

            return document.WithSyntaxRoot(newRoot);
        }
    }
}
