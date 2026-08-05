using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GodotTask.Tests;

internal sealed class TypePrinterWrapper<T>;

internal static class TypePrinterGenericOwner<T>
{
    public static async GDTask RunAsync()
    {
        await GDTask.Deferred();
    }
}

internal static class TypePrinterMethodOwner
{
    public static async GDTask RunAsync<T>()
    {
        await GDTask.Deferred();
    }

    public static async GDTask RunAsync<T, TSecond>()
    {
        await GDTask.Deferred();
    }
}

internal static class TypePrinterGenericMethodOwner<T>
{
    public static async GDTask RunAsync<TSecond>()
    {
        await GDTask.Deferred();
    }
}

internal class TypePrinterOuter<T>
{
    public class Inner<U>;
    public class OnlyOuter;

    public class Mid
    {
        public class Inner<U>
        {
            public class Deep;
        }

        public class Leaf;
    }

    public struct StructInner;
}

internal class TypePrinterPlain
{
    public class Mid
    {
        public class Inner<T>;
    }

    public class Nested;
}

internal class TypePrinterWeird<T, TSecond>
{
    public class Child;
    public class ChildOwn<TThird>;
}

internal static class TypePrinterShapeFactory
{
    public static IEnumerable<Type> CrazyShapes()
    {
        yield return typeof(int);
        yield return typeof(string);
        yield return typeof(void);
        yield return typeof(List<>);
        yield return typeof(Dictionary<,>);
        yield return typeof(List<int>);
        yield return typeof(int?);
        yield return typeof(Nullable<>);
        yield return typeof((int, string));
        yield return typeof((int, string, bool));
        yield return typeof((int, int, int, int, int, int, int, int));
        yield return typeof((int, int, int, int, int, int, int, int, string));
        yield return typeof((int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, string));
        yield return typeof(((int, string), (bool, byte)));
        yield return typeof(ValueTuple<>);
        yield return typeof(ValueTuple<int>);
        yield return typeof(int[]);
        yield return typeof(int[,]);
        yield return typeof(int[,,]);
        yield return typeof(int[][]);
        yield return typeof(int[][][]);
        yield return typeof(int[,][]);
        yield return typeof(int[][,]);
        yield return typeof(List<int>[]);
        yield return typeof(int?[]);
        yield return typeof(Dictionary<List<int>, Dictionary<string, int?>>);
        yield return typeof(Action<int, string>);
        yield return typeof(Func<int, Task<string>>);
        yield return typeof(TypePrinterOuter<int>.OnlyOuter);
        yield return typeof(TypePrinterOuter<int>.Inner<string>);
        yield return typeof(TypePrinterOuter<int>.Mid);
        yield return typeof(TypePrinterOuter<int>.Mid.Inner<string>);
        yield return typeof(TypePrinterOuter<int>.Mid.Leaf);
        yield return typeof(TypePrinterOuter<int>.Mid.Inner<string>.Deep);
        yield return typeof(TypePrinterOuter<>.Mid.Inner<>);
        yield return typeof(TypePrinterPlain.Mid.Inner<string>);
        yield return typeof(TypePrinterPlain.Nested);
        yield return typeof(TypePrinterWeird<int, string>.Child);
        yield return typeof(TypePrinterWeird<int, string>.ChildOwn<bool>);
        yield return typeof(TypePrinterOuter<List<int?>>.Inner<Dictionary<string, (int, bool)>>);
        yield return typeof(TypePrinterOuter<int>.Inner<string>[]);
        yield return typeof(TypePrinterOuter<int>.Inner<string>[,]);
        yield return typeof(TypePrinterOuter<int>.Inner<string>[][]);
        yield return typeof(TypePrinterOuter<int>.StructInner?);
        yield return typeof(int).MakePointerType();
        yield return typeof(int).MakeByRefType();
        yield return typeof(List<int>).MakePointerType();
        yield return typeof(List<int>).MakeByRefType();
        yield return typeof(List<>).MakeArrayType();
        yield return typeof(TypePrinterWrapper<>).MakeGenericType(
            typeof(TypePrinterOuter<>).GetNestedType("OnlyOuter")!.MakeGenericType(typeof(int)));

        foreach (var nested in typeof(TypePrinterGenericOwner<>).GetNestedTypes(
                     System.Reflection.BindingFlags.NonPublic))
        {
            yield return nested;
            yield return nested.MakeGenericType(typeof(int));
            yield return typeof(TypePrinterWrapper<>).MakeGenericType(nested.MakeGenericType(typeof(string)));
        }

        foreach (var nested in typeof(TypePrinterOuter<>.Mid.Inner<>).GetNestedTypes(
                     System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public))
        {
            if (!nested.Name.Contains("Deep", System.StringComparison.Ordinal))
                continue;
            yield return nested;
            yield return nested.MakeGenericType(typeof(int), typeof(string));
        }

        foreach (var nested in typeof(TypePrinterMethodOwner).GetNestedTypes(
                     System.Reflection.BindingFlags.NonPublic))
        {
            var arity = nested.GetGenericArguments().Length;
            if (arity == 1)
                yield return nested.MakeGenericType(typeof(int));
            else if (arity == 2)
                yield return nested.MakeGenericType(typeof(int), typeof(string));
            else
                yield return nested;
        }

        foreach (var nested in typeof(TypePrinterGenericMethodOwner<>).GetNestedTypes(
                     System.Reflection.BindingFlags.NonPublic))
        {
            var args = nested.GetGenericArguments();
            var closed = new Type[args.Length];
            for (var i = 0; i < args.Length; i++)
                closed[i] = i == 0 ? typeof(byte) : typeof(long);
            var stateMachine = nested.IsGenericTypeDefinition ? nested.MakeGenericType(closed) : nested;
            yield return stateMachine;
            yield return typeof(TypePrinterWrapper<>).MakeGenericType(stateMachine);
        }
    }
}
