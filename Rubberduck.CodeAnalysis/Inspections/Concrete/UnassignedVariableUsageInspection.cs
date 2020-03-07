using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Rubberduck.Inspections.Abstract;
using Rubberduck.JunkDrawer.Extensions;
using Rubberduck.Parsing;
using Rubberduck.Parsing.Grammar;
using Rubberduck.Resources.Inspections;
using Rubberduck.Parsing.Symbols;
using Rubberduck.Parsing.VBA;
using Rubberduck.Parsing.VBA.DeclarationCaching;
using Rubberduck.VBEditor;

namespace Rubberduck.Inspections.Concrete
{
    /// <summary>
    /// Warns when a variable is referenced prior to being assigned.
    /// </summary>
    /// <why>
    /// An uninitialized variable is being read, but since it's never assigned, the only value ever read would be the data type's default initial value. 
    /// Reading a variable that was never written to in any code path (especially if Option Explicit isn't specified), is likely to be a bug.
    /// </why>
    /// <remarks>
    /// This inspection may produce false positives when the variable is an array, or if it's passed by reference (ByRef) to a procedure that assigns it.
    /// </remarks>
    /// <example hasResults="true">
    /// <![CDATA[
    /// Public Sub DoSomething()
    ///     Dim i As Long
    ///     Debug.Print i ' i was never assigned
    /// End Sub
    /// ]]>
    /// </example>
    /// <example hasResults="false">
    /// <![CDATA[
    /// Public Sub DoSomething()
    ///     Dim i As Long
    ///     i = 42
    ///     Debug.Print i
    /// End Sub
    /// ]]>
    /// </example>
    [SuppressMessage("ReSharper", "LoopCanBeConvertedToQuery")]
    internal sealed class UnassignedVariableUsageInspection : IdentifierReferenceInspectionFromDeclarationsBase
    {
        public UnassignedVariableUsageInspection(IDeclarationFinderProvider declarationFinderProvider)
            : base(declarationFinderProvider)
        {}

        //See https://github.com/rubberduck-vba/Rubberduck/issues/2010 for why these are being excluded.
        private static readonly List<string> IgnoredFunctions = new List<string>
        {
            "VBE7.DLL;VBA.Strings.Len",
            "VBE7.DLL;VBA.Strings.LenB",
            "VBA6.DLL;VBA.Strings.Len",
            "VBA6.DLL;VBA.Strings.LenB"
        };

        protected override IEnumerable<Declaration> ObjectionableDeclarations(DeclarationFinder finder)
        {
            return finder.UserDeclarations(DeclarationType.Variable)
                .Where(declaration => !declaration.IsArray
                                      && !declaration.IsSelfAssigned
                                      && finder.MatchName(declaration.AsTypeName)
                                          .All(d => d.DeclarationType != DeclarationType.UserDefinedType)
                                      && !declaration.References
                                          .Any(reference => reference.IsAssignment)
                                      && !declaration.References
                                          .Any(reference => IsAssignedByRefArgument(reference.ParentScoping, reference, finder)));
        }

        //We override this in order to look up the argument usage exclusion references only once.
        protected override IEnumerable<IdentifierReference> ObjectionableReferences(DeclarationFinder finder)
        {
            var excludedReferenceSelections = DeclarationsWithExcludedArgumentUsage(finder)
                .SelectMany(SingleVariableArgumentSelections)
                .ToHashSet();

            return base.ObjectionableReferences(finder)
                .Where(reference => !excludedReferenceSelections.Contains(reference.QualifiedSelection));
        }

        private IEnumerable<ModuleBodyElementDeclaration> DeclarationsWithExcludedArgumentUsage(DeclarationFinder finder)
        {
            var vbaProjects = finder.Projects
                .Where(project => project.IdentifierName == "VBA" && !project.IsUserDefined)
                .ToList();

            if (!vbaProjects.Any())
            {
                return new List<ModuleBodyElementDeclaration>();
            }

            var stringModules = vbaProjects
                .Select(project => finder.FindStdModule("Strings", project, true))
                .OfType<ModuleDeclaration>()
                .ToList();

            if (!stringModules.Any())
            {
                return new List<ModuleBodyElementDeclaration>();
            }

            return stringModules
                .SelectMany(module => module.Members)
                .Where(decl => IgnoredFunctions.Contains(decl.QualifiedName.ToString()))
                .OfType<ModuleBodyElementDeclaration>();
        }

        private static IEnumerable<QualifiedSelection> SingleVariableArgumentSelections(ModuleBodyElementDeclaration member)
        {
            return member.Parameters
                .SelectMany(parameter => parameter.ArgumentReferences)
                .Select(SingleVariableArgumentSelection)
                .Where(maybeSelection => maybeSelection.HasValue)
                .Select(selection => selection.Value);
        }

        private static QualifiedSelection? SingleVariableArgumentSelection(ArgumentReference argumentReference)
        {
            var argumentContext = argumentReference.Context as VBAParser.LExprContext;
            if (!(argumentContext?.lExpression() is VBAParser.SimpleNameExprContext name))
            {
                return null;
            }

            return new QualifiedSelection(argumentReference.QualifiedModuleName, name.GetSelection());
        }

        protected override bool IsResultReference(IdentifierReference reference, DeclarationFinder finder)
        {
            return reference != null
                   && !IsArraySubscriptAssignment(reference) 
                   && !IsArrayReDim(reference);
        }

        protected override string ResultDescription(IdentifierReference reference)
        {
            var identifierName = reference.IdentifierName;
            return string.Format(
                InspectionResults.UnassignedVariableUsageInspection,
                identifierName);
        }

        private static bool IsAssignedByRefArgument(Declaration enclosingProcedure, IdentifierReference reference, DeclarationFinder finder)
        {
            var argExpression = ImmediateArgumentExpressionContext(reference);

            if (argExpression is null)
            {
                return false;
            }

            var parameter = finder.FindParameterOfNonDefaultMemberFromSimpleArgumentNotPassedByValExplicitly(argExpression, enclosingProcedure);

            // note: not recursive, by design.
            return parameter != null
                && (parameter.IsImplicitByRef || parameter.IsByRef)
                && parameter.References.Any(r => r.IsAssignment);
        }

        private static VBAParser.ArgumentExpressionContext ImmediateArgumentExpressionContext(IdentifierReference reference)
        {
            var context = reference.Context;
            //The context is either already a simpleNameExprContext or an IdentifierValueContext used in a sub-rule of some other lExpression alternative. 
            var lExpressionNameContext = context is VBAParser.SimpleNameExprContext simpleName
                ? simpleName
                : context.GetAncestor<VBAParser.LExpressionContext>();

            //To be an immediate argument and, thus, assignable by ref, the structure must be argumentExpression -> expression -> lExpression.
            return lExpressionNameContext?
                .Parent?
                .Parent as VBAParser.ArgumentExpressionContext;
        }

        private static bool IsArraySubscriptAssignment(IdentifierReference reference)
        {
            var nameExpression = reference.Context;
            if (!(nameExpression.Parent is VBAParser.IndexExprContext indexExpression))
            {
                return false;
            }

            var callingExpression = indexExpression.Parent;

            return callingExpression is VBAParser.SetStmtContext 
                   || callingExpression is VBAParser.LetStmtContext;
        }

        private static bool IsArrayReDim(IdentifierReference reference)
        {
            var nameExpression = reference.Context;
            if (!(nameExpression.Parent is VBAParser.IndexExprContext indexExpression))
            {
                return false;
            }

            var reDimVariableStmt = indexExpression.Parent?.Parent;

            return reDimVariableStmt is VBAParser.RedimVariableDeclarationContext;
        }
    }
}
