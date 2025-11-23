using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace UnionizeNow;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SwitchExhaustivenessSuppressor : DiagnosticSuppressor {
    private static readonly SuppressionDescriptor Rule = new(
        id: "UNION0001",
        suppressedDiagnosticId: "CS8509",
        justification: "For solidarity with the union members."
    );

    public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions => [Rule];

    public override void ReportSuppressions(SuppressionAnalysisContext context) {
        SwitchExpressionSyntax? GetSwitch(Diagnostic diagnostic) =>
            (SwitchExpressionSyntax?)(diagnostic.Location.SourceTree?.GetRoot(context.CancellationToken).FindNode(diagnostic.Location.SourceSpan));

        bool ShouldSuppress(SwitchExpressionSyntax switchExpression, SuppressionAnalysisContext context) =>
            context.GetSemanticModel(switchExpression.SyntaxTree) is SemanticModel semanticModel
            && semanticModel.GetTypeInfo(switchExpression.GoverningExpression) is TypeInfo unionType && unionType.Type is not null
            && (
                unionType.Type.AllInterfaces.Any(i => i.Name == "IUnionizeNow" && i.ContainingNamespace?.ToDisplayString() == "UnionizeNow")
                || (unionType.Type.Name == "IUnionizeNow" && unionType.Type.ContainingNamespace?.ToDisplayString() == "UnionizeNow")
            )
            && IncrementalUnionGenerator.GetUnionMembers(unionType.Type) is IEnumerable<INamedTypeSymbol> unionMembers
            && unionMembers.Any()
            && GetCoveredTypes(switchExpression, semanticModel) is HashSet<ITypeSymbol> coveredTypes
            && unionMembers.All(m =>
                coveredTypes.Contains(m, SymbolEqualityComparer.Default)
                || coveredTypes.Any(t => m.AllInterfaces.Contains(t, SymbolEqualityComparer.Default))
            );

        foreach (var diagnostic in context.ReportedDiagnostics.Where(d => GetSwitch(d) is SwitchExpressionSyntax s && ShouldSuppress(s, context))) {
            context.ReportSuppression(Suppression.Create(Rule, diagnostic));
        }
    }

    private static HashSet<ITypeSymbol> GetCoveredTypes(SwitchExpressionSyntax switchExpr, SemanticModel semanticModel) {
        var coveredTypes = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);

        foreach (var arm in switchExpr.Arms) {
            switch (arm.Pattern) {
                case DeclarationPatternSyntax declarationPattern:
                    if (semanticModel.GetTypeInfo(declarationPattern.Type).Type is ITypeSymbol declarationType) {
                        coveredTypes.Add(declarationType);
                    }

                    break;

                case ConstantPatternSyntax constantPattern:
                    if (semanticModel.GetTypeInfo(constantPattern.Expression).Type is ITypeSymbol constantType) {
                        coveredTypes.Add(constantType);
                    }

                    break;

                case TypePatternSyntax typePattern:
                    if (semanticModel.GetTypeInfo(typePattern.Type).Type is ITypeSymbol typeType) {
                        coveredTypes.Add(typeType);
                    }

                    break;
            }
        }

        return coveredTypes;
    }
}