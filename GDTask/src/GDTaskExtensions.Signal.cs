using System;
using System.Threading;
using Godot;

namespace GodotTask;

partial struct GDTask
{
     /// <summary>
    /// Create a new <see cref="SignalAwaiter"/> awaiter configured to complete when the instance source emits the signal specified by the signal parameter, and wrap it in a <see cref="GDTask"/>.
    /// </summary>
    public static async GDTask<Variant[]> FromSignal(GodotObject signalOwner, StringName signalName, CancellationToken cancellationToken)
    {
        var tcs = new GDTaskCompletionSource<Variant[]>();
        cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
        Create(async () =>
        {
            var result = await signalOwner.ToSignal(signalOwner, signalName);
            tcs.TrySetResult(result);
        }).Forget();
        return await tcs.Task;
    }

    /// <inheritdoc cref="FromSignal(Godot.GodotObject,Godot.StringName,System.Threading.CancellationToken)"/>
    public static async GDTask<T> FromSignal<[MustBeVariant] T>(GodotObject signalOwner, StringName signalName, CancellationToken cancellationToken)
    {
        var tcs = new GDTaskCompletionSource<T>();
        cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
        Create(async () =>
        {
            var result = await signalOwner.ToSignal(signalOwner, signalName);
            if (result.Length < 1) throw new IndexOutOfRangeException("SignalAwaiter result is empty!");
            tcs.TrySetResult(result[0].As<T>());
        }).Forget();
        return await tcs.Task;
    }

    /// <inheritdoc cref="FromSignal(Godot.GodotObject,Godot.StringName,System.Threading.CancellationToken)"/>
    public static async GDTask<(T1, T2)> FromSignal<[MustBeVariant] T1, [MustBeVariant] T2>(GodotObject signalOwner, StringName signalName, CancellationToken cancellationToken)
    {
        var tcs = new GDTaskCompletionSource<(T1, T2)>();
        cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
        Create(async () =>
        {
            var result = await signalOwner.ToSignal(signalOwner, signalName);
            if (result.Length < 2) throw new IndexOutOfRangeException($"SignalAwaiter result is {result.Length}, which is less than 2!");
            tcs.TrySetResult((result[0].As<T1>(), result[1].As<T2>()));
        }).Forget();
        return await tcs.Task;
    }

    /// <inheritdoc cref="FromSignal(Godot.GodotObject,Godot.StringName,System.Threading.CancellationToken)"/>
    public static async GDTask<(T1, T2, T3)> FromSignal<[MustBeVariant] T1, [MustBeVariant] T2, [MustBeVariant] T3>(GodotObject signalOwner, StringName signalName, CancellationToken cancellationToken)
    {
        var tcs = new GDTaskCompletionSource<(T1, T2, T3)>();
        cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
        Create(async () =>
        {
            var result = await signalOwner.ToSignal(signalOwner, signalName);
            if (result.Length < 3) throw new IndexOutOfRangeException($"SignalAwaiter result is {result.Length}, which is less than 3!");
            tcs.TrySetResult((result[0].As<T1>(), result[1].As<T2>(), result[2].As<T3>()));
        }).Forget();
        return await tcs.Task;
    }

    /// <inheritdoc cref="FromSignal(Godot.GodotObject,Godot.StringName,System.Threading.CancellationToken)"/>
    public static async GDTask<(T1, T2, T3, T4)> FromSignal<[MustBeVariant] T1, [MustBeVariant] T2, [MustBeVariant] T3, [MustBeVariant] T4>(GodotObject signalOwner, StringName signalName, CancellationToken cancellationToken)
    {
        var tcs = new GDTaskCompletionSource<(T1, T2, T3, T4)>();
        cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
        Create(async () =>
        {
            var result = await signalOwner.ToSignal(signalOwner, signalName);
            if (result.Length < 4) throw new IndexOutOfRangeException($"SignalAwaiter result is {result.Length}, which is less than 4!");
            tcs.TrySetResult((result[0].As<T1>(), result[1].As<T2>(), result[2].As<T3>(), result[3].As<T4>()));
        }).Forget();
        return await tcs.Task;
    }

    /// <inheritdoc cref="FromSignal(Godot.GodotObject,Godot.StringName,System.Threading.CancellationToken)"/>
    public static async GDTask<(T1, T2, T3, T4, T5)> FromSignal<[MustBeVariant] T1, [MustBeVariant] T2, [MustBeVariant] T3, [MustBeVariant] T4, [MustBeVariant] T5>(GodotObject signalOwner, StringName signalName, CancellationToken cancellationToken)
    {
        var tcs = new GDTaskCompletionSource<(T1, T2, T3, T4, T5)>();
        cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
        Create(async () =>
        {
            var result = await signalOwner.ToSignal(signalOwner, signalName);
            if (result.Length < 5) throw new IndexOutOfRangeException($"SignalAwaiter result is {result.Length}, which is less than 5!");
            tcs.TrySetResult((result[0].As<T1>(), result[1].As<T2>(), result[2].As<T3>(), result[3].As<T4>(), result[4].As<T5>()));
        }).Forget();
        return await tcs.Task;
    }
    
    /// <inheritdoc cref="FromSignal(Godot.GodotObject,Godot.StringName,System.Threading.CancellationToken)"/>
    public static GDTask<Variant[]> FromSignal(GodotObject signalOwner, StringName signalName) => 
        signalOwner.ToSignal(signalOwner, signalName).AsGDTask();
    
    /// <inheritdoc cref="FromSignal(Godot.GodotObject,Godot.StringName,System.Threading.CancellationToken)"/>
    public static GDTask<T> FromSignal<[MustBeVariant] T>(GodotObject signalOwner, StringName signalName) =>
        signalOwner.ToSignal(signalOwner, signalName).AsGDTask<T>();
    
    /// <inheritdoc cref="FromSignal(Godot.GodotObject,Godot.StringName,System.Threading.CancellationToken)"/>
    public static GDTask<(T1, T2)> FromSignal<[MustBeVariant] T1, [MustBeVariant] T2>(GodotObject signalOwner, StringName signalName) =>
        signalOwner.ToSignal(signalOwner, signalName).AsGDTask<T1, T2>();
    
    /// <inheritdoc cref="FromSignal(Godot.GodotObject,Godot.StringName,System.Threading.CancellationToken)"/>
    public static GDTask<(T1, T2, T3)> FromSignal<[MustBeVariant] T1, [MustBeVariant] T2, [MustBeVariant] T3>(GodotObject signalOwner, StringName signalName) => 
        signalOwner.ToSignal(signalOwner, signalName).AsGDTask<T1, T2, T3>();

    /// <inheritdoc cref="FromSignal(Godot.GodotObject,Godot.StringName,System.Threading.CancellationToken)"/>
    public static GDTask<(T1, T2, T3, T4)> FromSignal<[MustBeVariant] T1, [MustBeVariant] T2, [MustBeVariant] T3, [MustBeVariant] T4>(GodotObject signalOwner, StringName signalName) =>
        signalOwner.ToSignal(signalOwner, signalName).AsGDTask<T1, T2, T3, T4>();

    /// <inheritdoc cref="FromSignal(Godot.GodotObject,Godot.StringName,System.Threading.CancellationToken)"/>
    public static GDTask<(T1, T2, T3, T4, T5)> FromSignal<[MustBeVariant] T1, [MustBeVariant] T2, [MustBeVariant] T3, [MustBeVariant] T4, [MustBeVariant] T5>(GodotObject signalOwner, StringName signalName) =>
        signalOwner.ToSignal(signalOwner, signalName).AsGDTask<T1, T2, T3, T4, T5>();
}

public static partial class GDTaskExtensions
{
    /// <summary>
    /// Create a <see cref="GDTask"/> that wraps around this <see cref="SignalAwaiter"/>.
    /// </summary>
    public static async GDTask<Variant[]> AsGDTask(this SignalAwaiter signalAwaiter)
    {
        return await signalAwaiter;
    }
    
    /// <inhritdoc cref="AsGDTask(SignalAwaiter)"/>
    public static async GDTask<T> AsGDTask<[MustBeVariant] T>(this SignalAwaiter signalAwaiter)
    {
        var result = await signalAwaiter;
        if(result.Length < 1) throw new IndexOutOfRangeException("SignalAwaiter result is empty!");
        return result[0].As<T>();
    }
    
    /// <inhritdoc cref="AsGDTask(SignalAwaiter)"/>
    public static async GDTask<(T1, T2)> AsGDTask<[MustBeVariant] T1, [MustBeVariant] T2>(this SignalAwaiter signalAwaiter)
    {
        var result = await signalAwaiter;
        if(result.Length < 2) throw new IndexOutOfRangeException($"SignalAwaiter result is {result.Length}, which is less than 2!");
        return (result[0].As<T1>(), result[1].As<T2>());
    }
    
    /// <inhritdoc cref="AsGDTask(SignalAwaiter)"/>
    public static async GDTask<(T1, T2, T3)> AsGDTask<[MustBeVariant] T1, [MustBeVariant] T2, [MustBeVariant] T3>(this SignalAwaiter signalAwaiter)
    {
        var result = await signalAwaiter;
        if(result.Length < 3) throw new IndexOutOfRangeException($"SignalAwaiter result is {result.Length}, which is less than 3!");
        return (result[0].As<T1>(), result[1].As<T2>(), result[2].As<T3>());
    }
    
    /// <inhritdoc cref="AsGDTask(SignalAwaiter)"/>
    public static async GDTask<(T1, T2, T3, T4)> AsGDTask<[MustBeVariant] T1, [MustBeVariant] T2, [MustBeVariant] T3, [MustBeVariant] T4>(this SignalAwaiter signalAwaiter)
    {
        var result = await signalAwaiter;
        if(result.Length < 4) throw new IndexOutOfRangeException($"SignalAwaiter result is {result.Length}, which is less than 4!");
        return (result[0].As<T1>(), result[1].As<T2>(), result[2].As<T3>(), result[3].As<T4>());
    }
    
    /// <inhritdoc cref="AsGDTask(SignalAwaiter)"/>
    public static async GDTask<(T1, T2, T3, T4, T5)> AsGDTask<[MustBeVariant] T1, [MustBeVariant] T2, [MustBeVariant] T3, [MustBeVariant] T4, [MustBeVariant] T5>(this SignalAwaiter signalAwaiter)
    {
        var result = await signalAwaiter;
        if(result.Length < 5) throw new IndexOutOfRangeException($"SignalAwaiter result is {result.Length}, which is less than 5!");
        return (result[0].As<T1>(), result[1].As<T2>(), result[2].As<T3>(), result[3].As<T4>(), result[4].As<T5>());
    }
}