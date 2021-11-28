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

namespace ExceptionAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ExceptionAnalyzerCodeFixProvider)), Shared]
    public class ExceptionAnalyzerCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(ExceptionAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();

            var properties = diagnostic.Properties;

            if (properties.IsEmpty || !properties.TryGetValue(ExceptionAnalyzer.PropertiesExceptionTypeKey, out var exceptionName))
            {
                return;
            }

            var invocationAndExpressionType = await GetStatementAsync(context.Document, diagnostic, context.CancellationToken);

            if (invocationAndExpressionType == null)
            {
                return;
            }

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: CodeFixResources.CodeFixTitle,
                    createChangedDocument: c => 
                        AddTryCatchAsync(context.Document, invocationAndExpressionType.Value.Item1, invocationAndExpressionType.Value.Item2, exceptionName, c),
                    equivalenceKey: nameof(CodeFixResources.CodeFixTitle)),
                diagnostic);
        }

        private static async Task<(InvocationExpressionSyntax, StatementSyntax)?> GetStatementAsync(Document document, Diagnostic diagnostic, CancellationToken ct)
        {
            var tree = await document.GetSyntaxRootAsync(ct);

            var invocationExpression = (InvocationExpressionSyntax)tree.DescendantNodesAndSelf()
                .SingleOrDefault(n =>
                    n.IsKind(SyntaxKind.InvocationExpression) && n.GetLocation() == diagnostic.Location);

            if (invocationExpression == null)
            {
                return null;
            }

            var node = tree.DescendantNodesAndSelf()
                .LastOrDefault(n =>
                    n is StatementSyntax && n.Contains(invocationExpression));

            if (node == null)
            {
                return null;
            }

            return (invocationExpression, (StatementSyntax)node);
        }

        private static async Task<Document> AddTryCatchAsync(Document document, InvocationExpressionSyntax invocation, StatementSyntax statement, string exceptionName, CancellationToken ct)
        {
            var tryStatement = (TryStatementSyntax)invocation.Ancestors().FirstOrDefault(n => n.IsKind(SyntaxKind.TryStatement));

            TypeSyntax exceptionIdentifier;
            if (exceptionName.Contains('.'))
            {
                var parts = exceptionName.Split('.');
                exceptionIdentifier = SyntaxFactory.QualifiedName(SyntaxFactory.IdentifierName(parts.First()), SyntaxFactory.ParseToken("."),
                    SyntaxFactory.IdentifierName(parts.Last()));
            }
            else
            {
                exceptionIdentifier = SyntaxFactory.IdentifierName(exceptionName);
            }
            
            if (tryStatement == null)
            {
                var block = (BlockSyntax) SyntaxFactory.ParseStatement("{\n" + statement.GetText() + "}");

                // generate try catch around node
                var tryCatch = SyntaxFactory.TryStatement(SyntaxFactory.SingletonList(SyntaxFactory.CatchClause()
                        .WithDeclaration(SyntaxFactory.CatchDeclaration(exceptionIdentifier))))
                    .WithBlock(block);

                var oldRoot = await document.GetSyntaxRootAsync(ct).ConfigureAwait(false);
                var newRoot = oldRoot.ReplaceNode(statement, tryCatch).NormalizeWhitespace();

                return document.WithSyntaxRoot(newRoot);
            }
            else
            {
                var newTryStatement = tryStatement.AddCatches(SyntaxFactory.CatchClause()
                    .WithDeclaration(SyntaxFactory.CatchDeclaration(exceptionIdentifier)));

                var oldRoot = await document.GetSyntaxRootAsync(ct).ConfigureAwait(false);

                var newRoot = oldRoot.ReplaceNode(tryStatement, newTryStatement).NormalizeWhitespace();

                return document.WithSyntaxRoot(newRoot);
            }
        }
    }
}
