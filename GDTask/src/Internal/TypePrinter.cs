#nullable enable
using System;
using System.Collections.Generic;
using System.Text;

namespace GodotTask.Internal;

internal class TypePrinter
{
    [ThreadStatic] private static StringBuilder? _typeNameBuilder;
    private static readonly HashSet<Type>? _tupleTypeSet;
    private static readonly Dictionary<Type, string>? _builtinTypeNameDictionary;

    static TypePrinter()
    {
        _tupleTypeSet =
        [
            typeof(ValueTuple<,>),
            typeof(ValueTuple<,,>),
            typeof(ValueTuple<,,,>),
            typeof(ValueTuple<,,,,>),
            typeof(ValueTuple<,,,,,>),
            typeof(ValueTuple<,,,,,,>),
            typeof(ValueTuple<,,,,,,,>)
        ];

        _builtinTypeNameDictionary ??= new()
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
        };
    }
    
    internal static string ConstructTypeName(Type? type)
    {
        // return type.Name; <=== Implement Conditional Compiling
        
        // Down below is the method for printing the type definition in editor

        if (type is null) return string.Empty;
        
        if (type is { IsArray: false, IsGenericType: false })
        {
            return GetSimpleTypeName(type);
        }

        _typeNameBuilder ??= new StringBuilder();

        var sb = _typeNameBuilder;
        AppendType(sb, type);
        var result = sb.ToString();
        sb.Clear();
        return result;

        static void AppendType(StringBuilder sb, Type type)
        {
            if (type.IsArray)
            {
                AppendArray(sb, type);
            }
            else if (type.IsGenericType)
            {
                AppendGeneric(sb, type);
            }
            else
            {
                sb.Append(GetSimpleTypeName(type));
            }
        }

        static void AppendArray(StringBuilder sb, Type type)
        {
            // append inner most non-array element
            var elementType = type.GetElementType()!;
            while (elementType.IsArray)
            {
                elementType = elementType.GetElementType()!;
            }

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

            var genericArgs = type.GenericTypeArguments;
            var genericDefinition = type.GetGenericTypeDefinition();
            //Nullable
            if (genericDefinition == typeof(Nullable<>))
            {
                AppendType(sb, genericArgs[0]);
                sb.Append('?');
                return;
            }

            //ValueTuple
            if (_tupleTypeSet!.Contains(genericDefinition))
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
                    else
                    {
                        AppendParamTypes(sb, genericArgs.AsSpan(0, 7));
                        sb.Append(", ");

                        // TRest should be a ValueTuple!
                        var nextTuple = genericArgs[7];

                        genericArgs = nextTuple.GenericTypeArguments;
                    }
                }
                sb.Append(')');
                return;
            }


            //normal generic
            var typeName = type.Name.AsSpan();
            sb.Append(typeName[..typeName.LastIndexOf('`')]);
            sb.Append('<');
            AppendParamTypes(sb, genericArgs);
            sb.Append('>');

            static void AppendParamTypes(StringBuilder sb, ReadOnlySpan<Type> genericArgs)
            {
                var n = genericArgs.Length - 1;
                for (int i = 0; i < n; i += 1)
                {
                    AppendType(sb, genericArgs[i]);
                    sb.Append(", ");
                }

                AppendType(sb, genericArgs[n]);
            }
        }

        static string GetSimpleTypeName(Type type)
        {
            return _builtinTypeNameDictionary!.TryGetValue(type, out var name) ? name : type.Name;
        }
    }
}