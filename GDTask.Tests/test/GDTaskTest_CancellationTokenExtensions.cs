using System;
using System.Threading;
using System.Threading.Tasks;
using GdUnit4;
using GdUnit4.Exceptions;

namespace GodotTask.Tests;

[TestSuite]
public class GDTaskTest_CancellationTokenExtensions
{
    [TestCase]
    public static async Task GDTask_ToCancellationToken()
    {
        var token = Constants.DelayRealtime().ToCancellationToken();
        using (new ScopedStopwatch())
        {
            try
            {
                await GDTask.Never(token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }

        throw new TestFailedException("Operation not Canceled");
    }

    [TestCase]
    public static async Task GDTask_ToCancellationTokenT()
    {
        var token = Constants.DelayRealtimeWithReturn().ToCancellationToken();
        using (new ScopedStopwatch())
        {
            try
            {
                await GDTask.Never(token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }

        throw new TestFailedException("Operation not Canceled");
    }

    [TestCase]
    public static async Task GDTask_ToCancellationToken_Linked()
    {
        var associatedToken = Constants.DelayRealtime().ToCancellationToken();
        var primaryToken = Constants.DelayRealtime(2).ToCancellationToken(associatedToken);
        using (new ScopedStopwatch())
        {
            try
            {
                await GDTask.Never(primaryToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }

        throw new TestFailedException("Operation not Canceled");
    }

    [TestCase]
    public static async Task GDTask_ToCancellationTokenT_Linked()
    {
        var associatedToken = Constants.DelayRealtimeWithReturn().ToCancellationToken();
        var primaryToken = Constants.DelayRealtimeWithReturn(2).ToCancellationToken(associatedToken);
        using (new ScopedStopwatch())
        {
            try
            {
                await GDTask.Never(primaryToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }

        throw new TestFailedException("Operation not Canceled");
    }

    [TestCase]
    public static async Task CancellationToken_ToGDTask()
    {
        var source = new CancellationTokenSource();
        source.CancelAfter(Constants.DelayTimeSpan);
        var (gdTask, registration) = source.Token.ToGDTask();
        using (new ScopedStopwatch()) await gdTask;
        await registration.DisposeAsync();
    }

    [TestCase]
    public static async Task CancellationToken_WaitUntilCanceled()
    {
        var source = new CancellationTokenSource();
        source.CancelAfter(Constants.DelayTimeSpan);
        using (new ScopedStopwatch()) await source.Token.WaitUntilCanceled();
    }

    [TestCase]
    public static async Task CancellationToken_CancelAfterSlim()
    {
        var source = new CancellationTokenSource();
        source.CancelAfterSlim(Constants.DelayTimeSpan, DelayType.Realtime);
        using (new ScopedStopwatch()) await source.Token.WaitUntilCanceled();
    }

    [TestCase]
    public static async Task CancellationToken_RegisterRaiseCancelOnPredelete()
    {
        var source = new CancellationTokenSource();
        var node = Constants.CreateTestNode("RegisterRaiseCancelOnPredelete");
        source.RegisterRaiseCancelOnPredelete(node);
        node.QueueFree();
        await source.Token.WaitUntilCanceled();
    }
    
    [TestCase]
    public static async Task Disposable_AddTo()
    {
        var source = new CancellationTokenSource();
        var disposable = new SimpleDisposable();
        source.CancelAfter(Constants.DelayTimeSpan);
        disposable.AddTo(source.Token);
        using (new ScopedStopwatch())
        {
            await GDTask.WaitUntil(() => disposable.Disposed);
        }
    }
    
    [TestCase]
    public static async Task GDTask_SuppressCancellationThrow()
    {
        var canceled = await GDTask
            .Never(Constants.Delay().ToCancellationToken())
            .SuppressCancellationThrow();

        Assertions.AssertThat(canceled).IsTrue();
    }
    
    private class SimpleDisposable : IDisposable
    {
        public bool Disposed { get; private set; }

        public GDTask.Awaiter GetAwaiter() => 
            GDTask.WaitUntil(() => Disposed).GetAwaiter();
		
        public void Dispose()
        {
            Disposed = true;
        }
    }
}