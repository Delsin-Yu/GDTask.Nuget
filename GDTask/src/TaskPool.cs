using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace GodotTask;

/// <summary>
/// Global configuration for all GDTask internal object pools.
/// <para>
/// GDTask async operations (delays, waits, completion sources, state-machine runners, etc.)
/// internally pool and reuse <see cref="IGDTaskSource"/> instances to reduce GC pressure.
/// Each concrete type maintains its own independent <c>TaskPool&lt;T&gt;</c> instance;
/// for generic types, each closed generic type combination has its own separate pool as well.
/// </para>
/// <para>
/// The <see cref="MaxPoolSize"/> property exposed by this class acts as a unified capacity
/// cap applied to every pool individually.
/// </para>
/// </summary>
public static class TaskPool
{
    /// <summary>
    /// Gets or sets the maximum number of objects each object pool is allowed to retain.
    /// <para>
    /// When an async operation completes and attempts to return its object to the pool,
    /// the return is silently discarded if the pool has already reached this cap,
    /// and the object becomes eligible for normal garbage collection.
    /// </para>
    /// <para>
    /// <b>Runtime mutation behavior:</b> Changing this value only affects future return-to-pool
    /// operations. Objects already stored in a pool are never trimmed or evicted.
    /// For example, after lowering the value from <see cref="int.MaxValue"/> to 10,
    /// existing pooled objects can still be popped and reused, but no new objects will be
    /// accepted until the pool size drops below 10.
    /// </para>
    /// <para>
    /// This cap is enforced per pool instance, not as a sum across all pools.
    /// </para>
    /// <para>Default: <see cref="int.MaxValue"/> (effectively unbounded).</para>
    /// </summary>
    public static int MaxPoolSize { get; set; } = int.MaxValue;
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