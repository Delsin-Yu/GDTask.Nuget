using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Godot;

namespace GodotTask;

// Taken from GDTask library
// Holds static data about all task pools. Right now this is just the size of each pool.
static class TaskPool
{
    internal static int MaxPoolSize { get; set; }

    private static readonly ConcurrentDictionary<Type, Func<int>> _sizes = [];

    static TaskPool()
    {
        string value = OS.GetEnvironment("GDTASK_MAX_POOLSIZE");
        if (!string.IsNullOrEmpty(value) && int.TryParse(value, out var size))
        {
            MaxPoolSize = size;
        }
        else
        {
            MaxPoolSize = int.MaxValue;
        }
    }

    public static IEnumerable<(Type, int)> GetCacheSizeInfo()
    {
        foreach (var item in _sizes) yield return (item.Key, item.Value());
    }

    public static void RegisterSizeGetter(Type type, Func<int> getSize)
    {
        _sizes[type] = getSize;
    }
}

/// <summary>
/// Acts as a linked list for TaskSources.
/// </summary>
/// <typeparam name="T">Same type as the class that implements this</typeparam>
interface ITaskPoolNode<T>
{
    // Because interfaces cannot have fields, we store a reference to the field as a getter.
    // This is so we can directly set and get the field rather than using a property getter/setter, which might have more overhead.
    //
    // Disgusting, but efficient.
    ref T NextNode { get; }
}

// Mutable struct, don't mark readonly.
/// <summary>
/// Holds a linked list of <see cref="ITaskPoolNode{T}" />. Serves as a stack with push and pop operations.
/// </summary>
/// <typeparam name="T"></typeparam>
[StructLayout(LayoutKind.Auto)]
struct TaskPool<T>
    where T : class, ITaskPoolNode<T>
{
    // gate is basically a lock, which controls both popping and pushing to the TaskPool
    private int gate;

    // Linked list points backwards:
    // root <-- node2 <-- node3 <-- node4
    private T root;

    public int Size { get; private set; }

    // Methods are inlined, meaning the method body replaces all calls of the method, making the 
    // method run fast, but taking up more memory.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // Tries to pop.
    // If another thread is already popping/pushing to this pool, then return false (failure).
    // Otherwise, pop and return true.
    public bool TryPop(out T result)
    {
        // Interlocked class can perform single operations atomically (thread-safe)
        // Note that sequentialk Interlocked calls are not guaranteed to be thread-safe.
        //
        // CompareExchange:
        //      if gate == 0:
        //          gate = 1;
        //          return 0;   // Original value of gate
        //      return gate;    // Original value of gate
        if (Interlocked.CompareExchange(ref gate, 1, 0) == 0)
        {
            // If Interlocked.CompareExchange(ref gate, 1, 0) == 0, then the exchange worked!
            // Basically if the gate was 0, then the pool is free to be used, so we set it to 1
            // and start popping.
            var v = root;

            if (v is not null)
            {
                // Our pool is not empty, so we can pop.
                // Pop from start of linked list O(1) time
                ref var nextNode = ref v.NextNode;
                root = nextNode;
                nextNode = null;
                Size--;
                result = v;
                // Volatile writes ensure writes are thread safe?
                Volatile.Write(ref gate, 0);
                return true;
            }

            // Our pool is empty, so we can't pop.
            Volatile.Write(ref gate, 0);
        }

        result = default;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // Tries to push.
    // If another thread is already popping/pushing to this pool, then return false (failure).
    // Otherwise, pop and return true.
    public bool TryPush(T item)
    {
        if (Interlocked.CompareExchange(ref gate, 1, 0) == 0)
        {
            if (Size < TaskPool.MaxPoolSize)
            {
                // Push to start of linked list O(1) time
                item.NextNode = root;
                root = item;
                Size++;
                Volatile.Write(ref gate, 0);
                return true;
            }

            Volatile.Write(ref gate, 0);
        }

        return false;
    }
}