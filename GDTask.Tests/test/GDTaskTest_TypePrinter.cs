using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using GdUnit4;
using GodotTask.Internal;

namespace GodotTask.Tests;

[TestSuite]
public class GDTaskTest_TypePrinter
{
    [TestCase]
    public static void ConstructTypeName_FormatsCommonGenericTypes()
    {
        Assertions.AssertThat(TypePrinter.ConstructTypeName(typeof(List<int>)))
            .IsEqual("List<int>");
        Assertions.AssertThat(TypePrinter.ConstructTypeName(typeof(int?)))
            .IsEqual("int?");
        Assertions.AssertThat(TypePrinter.ConstructTypeName(typeof((int, string))))
            .IsEqual("(int, string)");
        Assertions.AssertThat(TypePrinter.ConstructTypeName(typeof(Dictionary<string, int>[])))
            .IsEqual("Dictionary<string, int>[]");
        Assertions.AssertThat(TypePrinter.ConstructTypeName(typeof(ValueTuple<int>)))
            .IsEqual("(int)");
        Assertions.AssertThat(TypePrinter.ConstructTypeName(typeof(void)))
            .IsEqual("void");
    }

    [TestCase]
    public static void ConstructTypeName_FormatsNestedTypesInGenericOwners()
    {
        Assertions.AssertThat(TypePrinter.ConstructTypeName(typeof(TypePrinterOuter<int>.Inner<string>)))
            .IsEqual("TypePrinterOuter<int>.Inner<string>");
        Assertions.AssertThat(TypePrinter.ConstructTypeName(typeof(TypePrinterOuter<int>.OnlyOuter)))
            .IsEqual("TypePrinterOuter<int>.OnlyOuter");
        Assertions.AssertThat(TypePrinter.ConstructTypeName(typeof(TypePrinterOuter<int>.Mid.Inner<string>)))
            .IsEqual("TypePrinterOuter<int>.Mid.Inner<string>");
        Assertions.AssertThat(TypePrinter.ConstructTypeName(typeof(TypePrinterOuter<int>.Mid.Leaf)))
            .IsEqual("TypePrinterOuter<int>.Mid.Leaf");
        Assertions.AssertThat(TypePrinter.ConstructTypeName(typeof(TypePrinterOuter<int>.Mid.Inner<string>.Deep)))
            .IsEqual("TypePrinterOuter<int>.Mid.Inner<string>.Deep");
        Assertions.AssertThat(TypePrinter.ConstructTypeName(typeof(TypePrinterWeird<int, string>.ChildOwn<bool>)))
            .IsEqual("TypePrinterWeird<int, string>.ChildOwn<bool>");
    }

    [TestCase]
    public static void ConstructTypeName_FormatsNonGenericNestedDeclaringChains()
    {
        Assertions.AssertThat(TypePrinter.ConstructTypeName(typeof(TypePrinterPlain.Nested)))
            .IsEqual("TypePrinterPlain.Nested");
        Assertions.AssertThat(TypePrinter.ConstructTypeName(typeof(TypePrinterPlain.Mid.Inner<string>)))
            .IsEqual("TypePrinterPlain.Mid.Inner<string>");
    }

    [TestCase]
    public static void ConstructTypeName_FormatsArraysTuplesAndWrappers()
    {
        Assertions.AssertThat(TypePrinter.ConstructTypeName(typeof(int[,])))
            .IsEqual("int[,]");
        Assertions.AssertThat(TypePrinter.ConstructTypeName(typeof(int[][])))
            .IsEqual("int[][]");
        Assertions.AssertThat(TypePrinter.ConstructTypeName(typeof(int[,][])))
            .IsEqual("int[,][]");
        Assertions.AssertThat(TypePrinter.ConstructTypeName(typeof(int[][,])))
            .IsEqual("int[][,]");
        Assertions.AssertThat(TypePrinter.ConstructTypeName(typeof((int, int, int, int, int, int, int, int, string))))
            .IsEqual("(int, int, int, int, int, int, int, int, string)");
        Assertions.AssertThat(TypePrinter.ConstructTypeName(typeof(((int, string), (bool, byte)))))
            .IsEqual("((int, string), (bool, byte))");
        Assertions.AssertThat(TypePrinter.ConstructTypeName(typeof(TypePrinterOuter<int>.Inner<string>[][])))
            .IsEqual("TypePrinterOuter<int>.Inner<string>[][]");
        Assertions.AssertThat(TypePrinter.ConstructTypeName(typeof(TypePrinterOuter<int>.StructInner?)))
            .IsEqual("TypePrinterOuter<int>.StructInner?");
        Assertions.AssertThat(TypePrinter.ConstructTypeName(typeof(int).MakePointerType()))
            .IsEqual("int*");
        Assertions.AssertThat(TypePrinter.ConstructTypeName(typeof(int).MakeByRefType()))
            .IsEqual("int&");
        Assertions.AssertThat(TypePrinter.ConstructTypeName(typeof(List<int>).MakePointerType()))
            .IsEqual("List<int>*");
        Assertions.AssertThat(TypePrinter.ConstructTypeName(typeof(List<int>).MakeByRefType()))
            .IsEqual("List<int>&");
        Assertions.AssertThat(TypePrinter.ConstructTypeName(typeof(List<>)))
            .IsEqual("List<T>");
        Assertions.AssertThat(TypePrinter.ConstructTypeName(typeof(TypePrinterOuter<>.Mid.Inner<>)))
            .IsEqual("TypePrinterOuter<T>.Mid.Inner<U>");
    }

    [TestCase]
    public static void ConstructTypeName_FormatsCompilerGeneratedStateMachineInGenericOwner()
    {
        var nestedOpen = Array.Find(
            typeof(TypePrinterGenericOwner<>).GetNestedTypes(BindingFlags.NonPublic),
            static type => type.Name.Contains("RunAsync", StringComparison.Ordinal));
        Assertions.AssertThat(nestedOpen).IsNotNull();

        var nestedClosed = nestedOpen!.MakeGenericType(typeof(int));
        var wrapper = typeof(TypePrinterWrapper<>).MakeGenericType(nestedClosed);

        var nestedName = TypePrinter.ConstructTypeName(nestedClosed);
        Assertions.AssertThat(nestedName.StartsWith("TypePrinterGenericOwner<int>.<RunAsync>d__", StringComparison.Ordinal)).IsTrue();
        Assertions.AssertThat(TypePrinter.ConstructTypeName(wrapper))
            .IsEqual($"TypePrinterWrapper<{nestedName}>");
    }

    [TestCase]
    public static void ConstructTypeName_FormatsGenericMethodStateMachines()
    {
        var methodStateMachine = Array.Find(
            typeof(TypePrinterMethodOwner).GetNestedTypes(BindingFlags.NonPublic),
            static type => type.Name.Contains("RunAsync", StringComparison.Ordinal) && type.GetGenericArguments().Length == 1);
        Assertions.AssertThat(methodStateMachine).IsNotNull();
        var methodName = TypePrinter.ConstructTypeName(methodStateMachine!.MakeGenericType(typeof(int)));
        Assertions.AssertThat(methodName.StartsWith("TypePrinterMethodOwner.<RunAsync", StringComparison.Ordinal)).IsTrue();
        Assertions.AssertThat(methodName.EndsWith("<int>", StringComparison.Ordinal)).IsTrue();

        var dual = Array.Find(
            typeof(TypePrinterMethodOwner).GetNestedTypes(BindingFlags.NonPublic),
            static type => type.GetGenericArguments().Length == 2);
        Assertions.AssertThat(dual).IsNotNull();
        var dualName = TypePrinter.ConstructTypeName(dual!.MakeGenericType(typeof(int), typeof(string)));
        Assertions.AssertThat(dualName.Contains("<int, string>", StringComparison.Ordinal)).IsTrue();

        var genericMethod = Array.Find(
            typeof(TypePrinterGenericMethodOwner<>).GetNestedTypes(BindingFlags.NonPublic),
            static type => type.Name.Contains("RunAsync", StringComparison.Ordinal));
        Assertions.AssertThat(genericMethod).IsNotNull();
        var combined = TypePrinter.ConstructTypeName(genericMethod!.MakeGenericType(typeof(byte), typeof(long)));
        Assertions.AssertThat(combined.StartsWith("TypePrinterGenericMethodOwner<byte>.<RunAsync", StringComparison.Ordinal)).IsTrue();
        Assertions.AssertThat(combined.EndsWith("<long>", StringComparison.Ordinal)).IsTrue();
    }

    [TestCase]
    public static void ConstructTypeName_DoesNotThrow_ForCrazyTypeShapes()
    {
        var failures = new List<string>();
        foreach (var type in TypePrinterShapeFactory.CrazyShapes())
        {
            try
            {
                var name = TypePrinter.ConstructTypeName(type);
                if (name is null)
                    failures.Add($"{type.FullName}: returned null");
                else if (name.Contains('`', StringComparison.Ordinal))
                    failures.Add($"{type.FullName}: still contains arity marker => {name}");
            }
            catch (Exception ex)
            {
                failures.Add($"{type.FullName}: {ex.GetType().Name}: {ex.Message}");
            }
        }

        Assertions.AssertThat(string.Join("\n", failures)).IsEmpty();
    }

    [TestCase, RequireGodotRuntime]
    public static async Task TaskTracker_DoesNotThrow_ForAsyncGDTaskInGenericContainingType()
    {
        await Constants.WaitForTaskReadyAsync();

        var previousTracking = TaskTracker.EnableTracking;
        var previousStackTrace = TaskTracker.EnableStackTrace;
        try
        {
            TaskTracker.EnableTracking = true;
            TaskTracker.EnableStackTrace = true;
            await TypePrinterGenericOwner<int>.RunAsync();
            await TypePrinterMethodOwner.RunAsync<string>();
            await TypePrinterGenericMethodOwner<int>.RunAsync<string>();
        }
        finally
        {
            TaskTracker.EnableTracking = previousTracking;
            TaskTracker.EnableStackTrace = previousStackTrace;
        }
    }
}
