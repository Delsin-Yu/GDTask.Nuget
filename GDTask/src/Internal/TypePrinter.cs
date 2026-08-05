#nullable enable
using System;
using System.Collections.Generic;
using System.Text;

namespace GodotTask.Internal;

class TypePrinter
{
    [ThreadStatic] private static StringBuilder? TypeNameBuilder;
    private static readonly HashSet<Type>? TupleTypeSet;
    private static readonly Dictionary<Type, string>? BuiltinTypeNameDictionary;

    static TypePrinter()
    {
        TupleTypeSet =
        [
            typeof(ValueTuple<>),
            typeof(ValueTuple<,>),
            typeof(ValueTuple<,,>),
            typeof(ValueTuple<,,,>),
            typeof(ValueTuple<,,,,>),
            typeof(ValueTuple<,,,,,>),
            typeof(ValueTuple<,,,,,,>),
            typeof(ValueTuple<,,,,,,,>),
        ];

        BuiltinTypeNameDictionary ??= new()
        {
            { typeof(sbyte), "sbyte" },
            { typeof(byte), "byte" },
            { typeof(short), "short" },
            { typeof(ushort), "ushort" },
            { typeof(int), "int" },
            { typeof(uint), "uint" },
            { typeof(long), "long" },
            { typeof(ulong), "ulong" },
            { typeof(nint), "nint" },
            { typeof(nuint), "nuint" },
            { typeof(float), "float" },
            { typeof(double), "double" },
            { typeof(decimal), "decimal" },
            { typeof(bool), "bool" },
            { typeof(char), "char" },
            { typeof(string), "string" },
            { typeof(object), "object" },
            { typeof(void), "void" },
        };
    }

    internal static string ConstructTypeName(Type? type)
    {
        // return type.Name; <=== Implement Conditional Compiling

        // Down below is the method for printing the type definition in editor

        if (type is null) return string.Empty;

        // Fast path for plain named types (no arrays, generics, nested paths, or wrappers).
        if (type is
            {
                IsArray: false,
                IsGenericType: false,
                IsByRef: false,
                IsPointer: false,
                IsNested: false,
                IsGenericParameter: false
            })
            return GetSimpleTypeName(type);

        TypeNameBuilder ??= new();

        var sb = TypeNameBuilder;
        AppendType(sb, type);
        var result = sb.ToString();
        sb.Clear();
        return result;

        static void AppendType(StringBuilder sb, Type type)
        {
            // Generic parameters report a DeclaringType; never walk it or open generics loop forever.
            if (type.IsGenericParameter)
            {
                sb.Append(type.Name);
                return;
            }

            if (type.IsByRef)
            {
                AppendType(sb, type.GetElementType()!);
                sb.Append('&');
                return;
            }

            if (type.IsPointer)
            {
                AppendType(sb, type.GetElementType()!);
                sb.Append('*');
                return;
            }

            if (type.IsArray) AppendArray(sb, type);
            else if (type.IsGenericType) AppendGeneric(sb, type);
            else if (type.IsNested) AppendNestedSimple(sb, type);
            else sb.Append(GetSimpleTypeName(type));
        }

        static void AppendNestedSimple(StringBuilder sb, Type type)
        {
            // Non-generic nested type: keep the full declaring-type path.
            AppendType(sb, type.DeclaringType!);
            sb.Append('.');
            sb.Append(GetSimpleTypeName(type));
        }

        static void AppendArray(StringBuilder sb, Type type)
        {
            // append inner most non-array element
            var elementType = type.GetElementType()!;
            while (elementType.IsArray) elementType = elementType.GetElementType()!;

            AppendType(sb, elementType);
            // append brackets
            AppendArrayRecursive(sb, type);

            static void AppendArrayRecursive(StringBuilder sb, Type type)
            {
                while (true)
                {
                    //append bracket with rank
                    var rank = type.GetArrayRank();
                    sb.Append('[');
                    sb.Append(',', rank - 1);
                    sb.Append(']');
                    //recursive call
                    var elementType = type.GetElementType()!;

                    if (elementType.IsArray)
                    {
                        type = elementType;
                        continue;
                    }

                    break;
                }
            }
        }

        static void AppendGeneric(StringBuilder sb, Type type)
        {
            // Prefer GetGenericArguments so open generic definitions still expose type parameters.
            var genericArgs = type.GetGenericArguments();
            var genericDefinition = type.GetGenericTypeDefinition();

            //Nullable
            if (genericDefinition == typeof(Nullable<>))
            {
                AppendType(sb, genericArgs[0]);
                sb.Append('?');
                return;
            }

            //ValueTuple
            if (TupleTypeSet!.Contains(genericDefinition))
            {
                sb.Append('(');

                while (true)
                {
                    // We assume that ValueTuple has 1~8 elements.
                    // And the 8th element (TRest) is always another ValueTuple.

                    // This is a hard coded tuple element length check.
                    if (genericArgs.Length != 8)
                    {
                        AppendParamTypes(sb, genericArgs);
                        break;
                    }

                    AppendParamTypes(sb, genericArgs.AsSpan(0, 7));
                    sb.Append(", ");

                    // TRest should be a ValueTuple!
                    var nextTuple = genericArgs[7];

                    genericArgs = nextTuple.GetGenericArguments();
                }

                sb.Append(')');
                return;
            }

            // Nested compiler-generated state machines inherit outer type arguments but
            // their Name has no arity marker (`N`). Format declaring types first and only
            // emit type arguments that belong to the current type level.
            AppendGenericType(sb, type, genericArgs, genericArgs.Length);
        }

        static void AppendGenericType(StringBuilder sb, Type type, Type[] genericArgs, int length)
        {
            var offset = 0;
            if (type.IsNested)
            {
                var declaringType = type.DeclaringType!;
                offset = declaringType.GetGenericArguments().Length;

                if (declaringType.IsGenericType)
                    AppendGenericType(sb, declaringType, genericArgs, offset);
                else
                    AppendType(sb, declaringType);

                sb.Append('.');
            }

            var typeName = type.Name.AsSpan();
            var backtickIndex = typeName.LastIndexOf('`');
            if (backtickIndex < 0)
            {
                // Nested type that only inherits outer generic arguments.
                sb.Append(typeName);
                return;
            }

            sb.Append(typeName[..backtickIndex]);

            var ownArgCount = length - offset;
            if (ownArgCount <= 0) return;

            sb.Append('<');
            AppendParamTypes(sb, genericArgs.AsSpan(offset, ownArgCount));
            sb.Append('>');
        }

        static void AppendParamTypes(StringBuilder sb, ReadOnlySpan<Type> genericArgs)
        {
            if (genericArgs.Length == 0) return;

            var n = genericArgs.Length - 1;

            for (var i = 0; i < n; i += 1)
            {
                AppendType(sb, genericArgs[i]);
                sb.Append(", ");
            }

            AppendType(sb, genericArgs[n]);
        }

        static string GetSimpleTypeName(Type type)
        {
            return BuiltinTypeNameDictionary!.TryGetValue(type, out var name) ? name : type.Name;
        }
    }
}
