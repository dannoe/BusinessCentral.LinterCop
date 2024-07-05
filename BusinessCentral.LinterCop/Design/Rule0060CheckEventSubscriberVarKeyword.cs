using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using System.Collections.Immutable;

namespace BusinessCentral.LinterCop.Design;

[DiagnosticAnalyzer]
public class Rule0060CheckEventSubscriberVarKeyword : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create<DiagnosticDescriptor>(DiagnosticDescriptors.Rule0060CheckEventSubscriberVarKeyword);

    public override void Initialize(AnalysisContext context)
    {
        context.RegisterSymbolAction(CheckForEventSubscriberVar, SymbolKind.Method);
    }

    private void CheckForEventSubscriberVar(SymbolAnalysisContext context)
    {
        var methodSymbol = (IMethodSymbol)context.Symbol;
        var eventSubscriberAttribute = methodSymbol.Attributes
            .FirstOrDefault(attr => attr.Name == "EventSubscriber");

        if (eventSubscriberAttribute == null) 
            return;

        if (!FindEventPublisherMethod(context, eventSubscriberAttribute, out var method))
            return;

        var publisherParameters = method.Parameters;

        foreach (var subscriberParameter in methodSymbol.Parameters)
        {
            var name = subscriberParameter.Name;
            var publisherParameter = publisherParameters.FirstOrDefault(p => p.Name == name.AsSpan());
            if (publisherParameter == null)
                continue;

            if (publisherParameter.IsVar && !subscriberParameter.IsVar)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.Rule0060CheckEventSubscriberVarKeyword,
                    subscriberParameter.GetLocation()));
            }
        }
    }

    private static bool FindEventPublisherMethod(SymbolAnalysisContext context, IAttributeSymbol eventSubscriberAttribute,
        out IMethodSymbol method)
    {
        var applicationObject = eventSubscriberAttribute.GetReferencedApplicationObject();
        if (applicationObject == null)
        {
            method = null;
            return false;
        }

        method = applicationObject.GetFirstMethod(eventSubscriberAttribute.Arguments[2].ValueText, context.Compilation);
        return method != null;
    }

}

