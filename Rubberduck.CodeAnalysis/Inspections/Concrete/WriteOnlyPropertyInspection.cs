using System.Collections.Generic;
using System.Linq;
using Rubberduck.CodeAnalysis.Inspections;
using Rubberduck.Inspections.Abstract;
using Rubberduck.Resources.Inspections;
using Rubberduck.Parsing.Symbols;
using Rubberduck.Parsing.VBA;
using Rubberduck.Parsing.VBA.DeclarationCaching;
using Rubberduck.VBEditor;

namespace Rubberduck.Inspections.Concrete
{
    /// <summary>
    /// Warns about properties that don't expose a 'Property Get' accessor.
    /// </summary>
    /// <why>
    /// Write-only properties are suspicious: if the client code is able to set a property, it should be allowed to read that property as well. 
    /// Class design guidelines and best practices generally recommend against write-only properties.
    /// </why>
    /// <example hasResults="true">
    /// <![CDATA[
    /// Private internalFoo As Long
    ///
    /// Public Property Let Foo(ByVal value As Long)
    ///     internalFoo = value
    /// End Property
    /// ]]>
    /// </example>
    /// <example hasResults="false">
    /// <![CDATA[
    /// Private internalFoo As Long
    ///
    /// Public Property Let Foo(ByVal value As Long)
    ///     internalFoo = value
    /// End Property
    ///
    /// Public Property Get Foo() As Long
    ///     Foo = internalFoo
    /// End Property
    /// ]]>
    /// </example>
    internal sealed class WriteOnlyPropertyInspection : DeclarationInspectionBase
    {
        public WriteOnlyPropertyInspection(IDeclarationFinderProvider declarationFinderProvider)
            : base(declarationFinderProvider, DeclarationType.PropertyLet, DeclarationType.PropertySet) { }
        
        protected override IEnumerable<IInspectionResult> DoGetInspectionResults(QualifiedModuleName module, DeclarationFinder finder)
        {
            var setters = RelevantDeclarationsInModule(module, finder)
                .Where(declaration => IsResultDeclaration(declaration, finder))
                .GroupBy(declaration => declaration.DeclarationType)
                .Select(grouping => grouping.First()); // don't get both Let and Set accessors

            return setters
                .Select(InspectionResult)
                .ToList();
        }

        protected override bool IsResultDeclaration(Declaration declaration, DeclarationFinder finder)
        {
            return (declaration.Accessibility == Accessibility.Implicit
                    || declaration.Accessibility == Accessibility.Public
                    || declaration.Accessibility == Accessibility.Global)
                   && finder.MatchName(declaration.IdentifierName)
                       .All(accessor => accessor.DeclarationType != DeclarationType.PropertyGet);
        }

        protected override string ResultDescription(Declaration declaration)
        {
            return string.Format(InspectionResults.WriteOnlyPropertyInspection, declaration.IdentifierName);
        }
    }
}
