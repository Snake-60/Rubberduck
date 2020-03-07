using System.Globalization;
using Rubberduck.CodeAnalysis.Inspections;
using Rubberduck.CodeAnalysis.QuickFixes.Abstract;
using Rubberduck.Inspections.Concrete;
using Rubberduck.Inspections.Inspections.Concrete;
using Rubberduck.Refactorings.Rename;
using Rubberduck.Resources;

namespace Rubberduck.Inspections.QuickFixes
{
    /// <summary>
    /// Prompts for a new name, renames a declaration accordingly, and updates all usages.
    /// </summary>
    /// <inspections>
    /// <inspection name="HungarianNotationInspection" />
    /// <inspection name="UseMeaningfulNameInspection" />
    /// <inspection name="DefaultProjectNameInspection" />
    /// <inspection name="UnderscoreInPublicClassModuleMemberInspection" />
    /// <inspection name="ExcelUdfNameIsValidCellReferenceInspection" />
    /// </inspections>
    /// <canfix procedure="false" module="false" project="false" />
    /// <example>
    /// <before>
    /// <![CDATA[
    /// Option Explicit
    /// 
    /// Public Sub DoSomething()
    ///     A1
    /// End Sub
    /// 
    /// Public Sub A1(ByVal value As Long)
    ///     Debug.Print value
    /// End Sub
    /// ]]>
    /// </before>
    /// <after>
    /// <![CDATA[
    /// Option Explicit
    /// 
    /// Public Sub DoSomething()
    ///     Renamed
    /// End Sub
    /// 
    /// Public Sub Renamed(ByVal value As Long)
    ///     Debug.Print value
    /// End Sub
    /// ]]>
    /// </after>
    /// </example>
    internal sealed class RenameDeclarationQuickFix : RefactoringQuickFixBase
    {
        public RenameDeclarationQuickFix(RenameRefactoring refactoring)
            : base(refactoring,
                typeof(HungarianNotationInspection), 
                typeof(UseMeaningfulNameInspection),
                typeof(DefaultProjectNameInspection), 
                typeof(UnderscoreInPublicClassModuleMemberInspection),
                typeof(ExcelUdfNameIsValidCellReferenceInspection))
        {}

        protected override void Refactor(IInspectionResult result)
        {
            Refactoring.Refactor(result.Target);
        }

        public override string Description(IInspectionResult result)
        {
            return string.Format(RubberduckUI.Rename_DeclarationType,
                RubberduckUI.ResourceManager.GetString("DeclarationType_" + result.Target.DeclarationType,
                    CultureInfo.CurrentUICulture));
        }
    }
}