using System;
using Godot;

namespace GodotTask;

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