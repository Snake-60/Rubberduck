using System.Linq;
using NUnit.Framework;
using Rubberduck.CodeAnalysis.Inspections;
using Rubberduck.Inspections.Concrete;
using Rubberduck.Parsing.VBA;
using Rubberduck.VBEditor.SafeComWrappers;
using RubberduckTests.Mocks;

namespace RubberduckTests.Inspections
{
    [TestFixture]
    public class UnassignedVariableUsageInspectionTests : InspectionTestsBase
    {
        [Test]
        [Category("Inspections")]
        public void IgnoresExplicitArrays()
        {
            const string code = @"
Sub Foo()
    Dim bar() As String
    bar(1) = ""value""
End Sub
";
            Assert.AreEqual(0, InspectionResultsForStandardModule(code).Count());
        }

        [Test]
        [Category("Inspections")]
        public void IgnoresArrayReDim()
        {
            const string code = @"
Sub Foo()
    Dim bar As Variant
    ReDim bar(1 To 10)
End Sub
";
            Assert.AreEqual(0, InspectionResultsForStandardModule(code).Count());
        }

        [Test]
        [Category("Inspections")]
        public void DoNotIgnoresArrayReDimBounds()
        {
            const string code = @"
Sub Foo()
    Dim bar As Variant
    Dim baz As Variant
    Dim foo As Variant
    ReDim bar(baz To foo)
End Sub
";
            Assert.AreEqual(2, InspectionResultsForStandardModule(code).Count());
        }

        [Test]
        [Category("Inspections")]
        public void IgnoresArraySubscripts_Let()
        {
            const string code = @"
Sub Foo()
    Dim bar As Variant
    ReDim bar(1 To 10)
    bar(1) = 42
End Sub
";
            Assert.AreEqual(0, InspectionResultsForStandardModule(code).Count());
        }

        [Test]
        [Category("Inspections")]
        public void DoNotIgnoreArrayIndexes_Let()
        {
            const string code = @"
Sub Foo()
    Dim bar As Variant
    Dim foo As Variant
    ReDim bar(1 To 10)
    bar(foo) = 42
End Sub
";
            Assert.AreEqual(1, InspectionResultsForStandardModule(code).Count());
        }

        [Test]
        [Category("Inspections")]
        public void DoNotIgnoreValuesAssignedToArraySubscripts_Let()
        {
            const string code = @"
Sub Foo()
    Dim bar As Variant
    Dim foo As Variant
    ReDim bar(1 To 10)
    bar(1) = foo
End Sub
";
            Assert.AreEqual(1, InspectionResultsForStandardModule(code).Count());
        }

        [Test]
        [Category("Inspections")]
        public void DoNotIgnoreIndexedPropertyAccess_Let()
        {
            const string code = @"
Sub Foo()
    Dim foo As Variant
    ReDim bar(1 To 10)
    foo.Bar(1) = 42
End Sub
";
            Assert.AreEqual(1, InspectionResultsForStandardModule(code).Count());
        }

        [Test]
        [Category("Inspections")]
        public void IgnoresArraySubscripts_Set()
        {
            const string code = @"
Sub Foo()
    Dim bar As Variant
    ReDim bar(1 To 10)
    Set bar(1) = Nothing
End Sub
";
            Assert.AreEqual(0, InspectionResultsForStandardModule(code).Count());
        }

        [Test]
        [Category("Inspections")]
        public void DoNotIgnoreArrayIndexes_Set()
        {
            const string code = @"
Sub Foo()
    Dim bar As Variant
    Dim foo As Variant
    ReDim bar(1 To 10)
    Set bar(foo) = Nothing
End Sub
";
            Assert.AreEqual(1, InspectionResultsForStandardModule(code).Count());
        }

        [Test]
        [Category("Inspections")]
        public void DoNotIgnoreValuesAssignedToArraySubscripts_Set()
        {
            const string code = @"
Sub Foo()
    Dim bar As Variant
    Dim foo As Variant
    ReDim bar(1 To 10)
    Set bar(1) = foo
End Sub
";
            Assert.AreEqual(1, InspectionResultsForStandardModule(code).Count());
        }

        [Test]
        [Category("Inspections")]
        public void DoNotIgnoreIndexedPropertyAccess_Set()
        {
            const string code = @"
Sub Foo()
    Dim foo As Variant
    ReDim bar(1 To 10)
    Set foo.Bar(1) = 42
End Sub
";
            Assert.AreEqual(1, InspectionResultsForStandardModule(code).Count());
        }

        [Test]
        [Category("Inspections")]
        public void DoesNotIgnoreWithBlockVariableUse()
        {
            const string code = @"
Sub Foo()
    Dim foo As Variant
    With foo
    End With
End Sub
";
            Assert.AreEqual(1, InspectionResultsForStandardModule(code).Count());
        }

        [Test]
        [Category("Inspections")]
        public void IgnoreUseViaWithBlockVariableInWithBlock()
        {
            const string code = @"
Sub Foo()
    Dim foo As Variant
    Dim bar As Variant
    With foo
        bar = .Baz + 23
        bar = .Baz + 42
    End With
End Sub
";
            Assert.AreEqual(1, InspectionResultsForStandardModule(code).Count());
        }

        [Test]
        [Category("Inspections")]
        public void UnassignedVariableUsage_ReturnsResult()
        {
            const string code = @"
Sub Foo()
    Dim b As Boolean
    Dim bb As Boolean
    bb = b
End Sub
";
            Assert.AreEqual(1, InspectionResultsForStandardModule(code).Count());
        }

        [Test]
        [Category("Inspections")]
        public void UnassignedVariableUsage_DoesNotReturnResult()
        {
            const string code = @"
Sub Foo()
    Dim b As Boolean
    Dim bb As Boolean
    b = True
    bb = b
End Sub
";

            Assert.AreEqual(0, InspectionResultsForStandardModule(code).Count());
        }

        [Test]
        [Category("Inspections")]
        public void UnassignedVariableUsage_Ignored_DoesNotReturnResult()
        {
            const string code = @"
Sub Foo()
    Dim b As Boolean
    Dim bb As Boolean

'@Ignore UnassignedVariableUsage
    bb = b
End Sub
";
            Assert.AreEqual(0, InspectionResultsForStandardModule(code).Count());
        }

        [Test]
        [Category("Inspections")]
        public void UnassignedVariableUsage_Ignored_DoesNotReturnResultMultipleIgnores()
        {
            const string code = @"
Sub Foo()    
    Dim b As Boolean
    Dim bb As Boolean

'@Ignore UnassignedVariableUsage, VariableNotAssigned
    bb = b
End Sub
";
            Assert.AreEqual(0, InspectionResultsForStandardModule(code).Count());
        }

        [Test]
        [Category("Inspections")]
        public void UnassignedVariableUsage_NoResultForAssignedByRefReference()
        {
            const string code = @"
Sub DoSomething()
    Dim foo
    AssignThing foo
    Dim bar As Variant
    bar = foo
End Sub

Sub AssignThing(ByRef thing As Variant)
    thing = 42
End Sub
";
            Assert.AreEqual(0, InspectionResultsForStandardModule(code).Count());
        }

        [Test]
        [Category("Inspections")]
        public void UnassignedVariableUsage_ResultForUseStrictlyInsideArgumentToByRefArgument()
        {
            const string code = @"
Sub DoSomething()
    Dim foo
    AssignThing foo + 42
    Dim bar As Variant
    bar = foo
End Sub

Sub AssignThing(ByRef thing As Variant)
    thing = 42
End Sub
";
            Assert.AreEqual(2, InspectionResultsForStandardModule(code).Count());
        }

        [Test]
        [Category("Inspections")]
        public void UnassignedVariableUsage_NoResultIfNoReferences()
        {
            const string code = @"
Sub DoSomething()
    Dim foo
End Sub
";
            Assert.AreEqual(0, InspectionResultsForStandardModule(code).Count());
        }

        [Test]
        [Category("Inspections")]
        public void UnassignedVariableUsage_NoResultForLenFunction()
        {
            const string code = @"
Sub DoSomething()
    Dim foo As LongPtr
    Dim bar As Variant
    bar = Len(foo)
End Sub
";
            var inspectionResults = InspectionResultsForModules(("TestModule", code, ComponentType.StandardModule), ReferenceLibrary.VBA);
            Assert.AreEqual(0, inspectionResults.Count());
        }

        [Test]
        [Category("Inspections")]
        public void UnassignedVariableUsage_NoResultForLenBFunction()
        {
            const string code = @"
Sub DoSomething()
    Dim foo As LongPtr
    Dim bar As Variant
    bar = LenB(foo)
End Sub
";
            var inspectionResults = InspectionResultsForModules(("TestModule", code, ComponentType.StandardModule), ReferenceLibrary.VBA);
            Assert.AreEqual(0, inspectionResults.Count());
        }

        [Test]
        [Category("Inspections")]
        public void UnassignedVariableUsage_ResultForOthersIfLenFunctionIsUsed()
        {
            const string code = @"
Sub DoSomething()
    Dim foo As Variant
    Dim bar As Variant
    bar = Len(foo)
    bar = foo + 5
End Sub
";
            var inspectionResults = InspectionResultsForModules(("TestModule", code, ComponentType.StandardModule), ReferenceLibrary.VBA);
            Assert.AreEqual(1, inspectionResults.Count());
        }

        [Test]
        [Category("Inspections")]
        public void UnassignedVariableUsage_ResultForOthersIfLenBFunctionIsUsed()
        {
            const string code = @"
Sub DoSomething()
    Dim foo As Variant
    Dim bar As Variant
    bar = LenB(foo)
    bar = foo + 5
End Sub
";
            var inspectionResults = InspectionResultsForModules(("TestModule", code, ComponentType.StandardModule), ReferenceLibrary.VBA);
            Assert.AreEqual(1, inspectionResults.Count());
        }

        [Test]
        [Category("Inspections")]
        public void UnassignedVariableUsage_ResultForUsageInsideArgumentOfLen()
        {
            const string code = @"
Sub DoSomething()
    Dim foo As Variant
    Dim bar As Variant
    bar = Len(foo + 5)
End Sub
";
            var inspectionResults = InspectionResultsForModules(("TestModule", code, ComponentType.StandardModule), ReferenceLibrary.VBA);
            Assert.AreEqual(1, inspectionResults.Count());
        }

        [Test]
        [Category("Inspections")]
        public void UnassignedVariableUsage_ResultForUsageInsideArgumentOfLenB()
        {
            const string code = @"
Sub DoSomething()
    Dim foo As Variant
    Dim bar As Variant
    bar = LenB(foo + 5)
End Sub
";
            var inspectionResults = InspectionResultsForModules(("TestModule", code, ComponentType.StandardModule), ReferenceLibrary.VBA);
            Assert.AreEqual(1, inspectionResults.Count());
        }

        [Test]
        [Category("Inspections")]
        public void InspectionName()
        {
            var inspection = new UnassignedVariableUsageInspection(null);

            Assert.AreEqual(nameof(UnassignedVariableUsageInspection), inspection.Name);
        }

        protected override IInspection InspectionUnderTest(RubberduckParserState state)
        {
            return new UnassignedVariableUsageInspection(state);
        }
    }
}
