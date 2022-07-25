using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions.Mef;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces;
// ReSharper disable HeuristicUnreachableCode


//#nullable enable
namespace BusinessCentral.LinterCop.CodeFixer
{
  [CodeFixProvider(nameof(Fix0001FlowFieldsShouldNotBeEditableCodeFixProvider))]
  public sealed class Fix0001FlowFieldsShouldNotBeEditableCodeFixProvider : ICodeFixProvider
  {
    public ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create<string>(DiagnosticDescriptors.Rule0001FlowFieldsShouldNotBeEditable.Id);

    public async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
      Document document = context.Document;
      TextSpan span = context.Span;
      SyntaxNode node = (await document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false)).FindNode(span);
      if (node == null)
        return;
      else
        await Task.Run(() => context.RegisterCodeFix(
            new AddEditableFalseToFlowFieldCodeAction(
            "Set Editable = false",
            c => AddEditableFalseToSingleField(document, node, c),
            null),
          context.Diagnostics));
    }

    private async Task<Document> AddEditableFalseToSingleField(
      Document document,
      SyntaxNode oldNode,
      CancellationToken cancellationToken)
    {
      if (oldNode.Kind != SyntaxKind.IdentifierName)
        return document;

      var oldFieldSyntaxNode = (FieldSyntax)oldNode.Parent;
      PropertySyntax propertySyntax = oldFieldSyntaxNode.GetProperty("Editable");
      // ReSharper disable once ConditionIsAlwaysTrueOrFalse
      if (propertySyntax == null)
      {
        SyntaxNode newNode = oldFieldSyntaxNode.AddPropertyListProperties(SyntaxFactory.Property(PropertyKind.Editable,
          SyntaxFactory.BooleanPropertyValue(
            SyntaxFactory.BooleanLiteralValue(SyntaxFactory.Token(SyntaxKind.FalseKeyword)))).WithLeadingTrivia());

        return document.WithSyntaxRoot(
          (await document.GetSyntaxRootAsync(cancellationToken)).ReplaceNode<SyntaxNode>(oldNode.Parent, newNode));
      }
      else
      {
        var newPropertySyntax = propertySyntax.WithValue((PropertyValueSyntax)SyntaxFactory.BooleanPropertyValue(
          SyntaxFactory.BooleanLiteralValue(SyntaxFactory.Token(SyntaxKind.FalseKeyword))));

        return document.WithSyntaxRoot(
          (await document.GetSyntaxRootAsync(cancellationToken)).ReplaceNode<SyntaxNode>(propertySyntax, newPropertySyntax));
      }
    }


    private class AddEditableFalseToFlowFieldCodeAction : CodeAction.DocumentChangeAction
    {
      public override CodeActionKind Kind => CodeActionKind.QuickFix;

      public AddEditableFalseToFlowFieldCodeAction(
        string title,
        Func<CancellationToken, Task<Document>> createChangedDocument,
        string equivalenceKey)
        : base(title, createChangedDocument, equivalenceKey)
      {
      }
    }

  }
}