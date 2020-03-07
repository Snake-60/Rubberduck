using Rubberduck.CodeAnalysis.Inspections;
using Rubberduck.CodeAnalysis.QuickFixes.Abstract;
using Rubberduck.Inspections.Concrete;
using Rubberduck.Parsing.Rewriter;

namespace Rubberduck.Inspections.QuickFixes
{
    /// <summary>
    /// Removes 'Option Base 0' statement from a module, making it implicit (0 being the default implicit lower bound for implicitly-sized arrays).
    /// </summary>
    /// <inspections>
    /// <inspection name="RedundantOptionInspection" />
    /// </inspections>
    /// <canfix procedure="false" module="false" project="false" />
    /// <example>
    /// <before>
    /// <![CDATA[
    /// Option Explicit
    /// Option Base 0
    /// 
    /// Public Sub DoSomething()
    ///     Dim values(10) ' implicit lower bound is 0
    ///     Debug.Print LBound(values), UBound(values)
    /// End Sub
    /// ]]>
    /// </before>
    /// <after>
    /// <![CDATA[
    /// Option Explicit
    /// 
    /// Public Sub DoSomething()
    ///     Dim values(10) ' implicit lower bound is 0
    ///     Debug.Print LBound(values), UBound(values)
    /// End Sub
    /// ]]>
    /// </after>
    /// </example>
    internal sealed class RemoveOptionBaseStatementQuickFix : QuickFixBase
    {
        public RemoveOptionBaseStatementQuickFix()
            : base(typeof(RedundantOptionInspection))
        {}

        public override void Fix(IInspectionResult result, IRewriteSession rewriteSession)
        {
            var rewriter = rewriteSession.CheckOutModuleRewriter(result.QualifiedSelection.QualifiedName);
            rewriter.Remove(result.Context);
        }

        public override string Description(IInspectionResult result) => Resources.Inspections.QuickFixes.RemoveOptionBaseStatementQuickFix;

        public override bool CanFixInProcedure => false;
        public override bool CanFixInModule => false;
        public override bool CanFixInProject => false;
    }
}