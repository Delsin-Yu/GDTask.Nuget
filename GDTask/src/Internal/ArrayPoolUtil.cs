using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GodotTask.Internal;


internal class EnumerableUtils
{
    public readonly ref struct ArrayPoolUsage<T>(T[] arrayPoolArray) : IDisposable
    {
        public void Dispose()
        {
            if(arrayPoolArray is null) return;
            ArrayPool<T>.Shared.Return(arrayPoolArray);
        }
    }

    public static ArrayPoolUsage<T> ToSpan<T>(IEnumerable<T> enumerable, out ReadOnlySpan<T> span)
    {
        switch (enumerable)
        {
            case T[] array:
            {
                span = array.AsSpan();
                return new(null);
            }
            case List<T> list:
            {
                span = CollectionsMarshal.AsSpan(list);
                return new(null);
            }
            case ICollection<T> collection:
            {
                var count = collection.Count;
                var arrayPoolArray = ArrayPool<T>.Shared.Rent(count);
                collection.CopyTo(arrayPoolArray, 0);
                span = arrayPoolArray.AsSpan(0, count);
                return new(arrayPoolArray);
            }
            case IReadOnlyCollection<T> readOnlyCollection:
            {
                var count = readOnlyCollection.Count;
                var arrayPoolArray = ArrayPool<T>.Shared.Rent(count);
                span = arrayPoolArray.AsSpan(0, count);
                var i = 0;
                foreach (var item in enumerable)
                {
                    arrayPoolArray[i++] = item;
                }
                return new(arrayPoolArray);
            }
            default:
            {
                var rentSize = 32;
                var pool = ArrayPool<T>.Shared;
                var arrayPoolArray = pool.Rent(rentSize);
                var i = 0;
                foreach (var item in enumerable)
                {
                    if (arrayPoolArray.Length <= i)
                    {
                        rentSize *= 2;
                        var newArray = pool.Rent(rentSize);
                        Array.Copy(arrayPoolArray, newArray, i);
                        pool.Return(arrayPoolArray, clearArray: !RuntimeHelpers.IsReferenceOrContainsReferences<T>());
                        arrayPoolArray = newArray;
                    }
                    arrayPoolArray[i++] = item;
                }
                span = arrayPoolArray.AsSpan(0, i);
                return new(arrayPoolArray);
            }
        }
    }
}
