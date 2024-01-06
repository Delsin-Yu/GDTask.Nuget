using System;
using System.Linq;
using System.Threading;
using Chickensoft.GoDotTest;
using Godot;
using Environment = System.Environment;

namespace Fractural.Tasks.Tests;

public class UsageTest : TestClass
{
	public static async GDTask UsageDemo()
	{
		int result;

		await GDTask.Yield();
		await GDTask.Yield(PlayerLoopTiming.Process);
		await GDTask.Yield(CancellationToken.None);
		await GDTask.Yield(PlayerLoopTiming.PhysicsProcess, CancellationToken.None);
		await GDTask.NextFrame();
		await GDTask.NextFrame(PlayerLoopTiming.Process);
		await GDTask.NextFrame(CancellationToken.None);
		await GDTask.NextFrame(PlayerLoopTiming.PhysicsProcess, CancellationToken.None);
		await GDTask.WaitForEndOfFrame();
		await GDTask.WaitForEndOfFrame(CancellationToken.None);
		await GDTask.WaitForPhysicsProcess();
		await GDTask.WaitForPhysicsProcess(CancellationToken.None);
		await GDTask.DelayFrame(1);
		await GDTask.Delay(1000);
		await GDTask.Delay(1000, DelayType.Realtime, PlayerLoopTiming.PhysicsProcess, CancellationToken.None);
		await GDTask.Delay(TimeSpan.FromSeconds(1), DelayType.Realtime, PlayerLoopTiming.PhysicsProcess);
		await GDTask.Delay(TimeSpan.FromSeconds(1), DelayType.Realtime, PlayerLoopTiming.PhysicsProcess, CancellationToken.None);

		await GDTask.FromException(new());
		await GDTask.FromException<int>(new());
		result = await GDTask.FromResult(5);
		await GDTask.FromCanceled(CancellationToken.None);
		await GDTask.FromCanceled<int>(CancellationToken.None);
		await GDTask.Create(() => GDTask.Delay(5));
		result = await GDTask.Create(() => GDTask.Delay(5).ContinueWith(() => 5));
		var lazy1 = GDTask.Lazy(() => GDTask.Delay(5)).Task;
		var lazy2 = GDTask.Lazy(() => GDTask.Delay(5).ContinueWith(() => 5)).Task;
		await lazy1;
		result = await lazy2;
		GDTask.Void(async () => await GDTask.Delay(5));
		GDTask.Void(async cancellationToken => await GDTask.Delay(5, cancellationToken: cancellationToken), CancellationToken.None);
		GDTask.Void(async value => await GDTask.Delay(5).ContinueWith(() => GD.Print(value)), 5);
		GDTask.Action(async () => await GDTask.Delay(5)).Invoke();
		GDTask.Action(async cancellationToken => await GDTask.Delay(5, cancellationToken: cancellationToken), CancellationToken.None).Invoke();
		var deferred1 = GDTask.Defer(() => GDTask.Delay(5));
		var deferred2 = GDTask.Defer(() => GDTask.Delay(5).ContinueWith(() => 5));
		await deferred1;
		result = await deferred2;
		await GDTask.Never(CancellationToken.None);
		await GDTask.Never<int>(CancellationToken.None);

		await GDTask.RunOnThreadPool(() => GD.Print("Run"), true, CancellationToken.None);
		await GDTask.RunOnThreadPool(obj => GD.Print((string)obj), "Run", true, CancellationToken.None);
		await GDTask.RunOnThreadPool(() => GDTask.Delay(5).ContinueWith(() => GD.Print("Run")), true, CancellationToken.None);
		await GDTask.RunOnThreadPool(obj => GDTask.Delay(5).ContinueWith(() => GD.Print((string)obj)), "Run", true, CancellationToken.None);
		result = await GDTask.RunOnThreadPool(() => 5, true, CancellationToken.None);
		result = await GDTask.RunOnThreadPool(() => GDTask.Delay(5).ContinueWith(() => 5), true, CancellationToken.None);
		result = await GDTask.RunOnThreadPool(obj => (int)obj, 5, true, CancellationToken.None);
		result = await GDTask.RunOnThreadPool(obj => GDTask.Delay(5).ContinueWith(() => (int)obj), 5, true, CancellationToken.None);

		await GDTask.SwitchToThreadPool();
		GD.Print("ThreadPool: ThreadId =" + Environment.CurrentManagedThreadId);
		await GDTask.SwitchToMainThread(CancellationToken.None);
		GD.Print("MainThread: ThreadId =" + Environment.CurrentManagedThreadId);
		await GDTask.SwitchToThreadPool();
		GD.Print("ThreadPool: ThreadId =" + Environment.CurrentManagedThreadId);
		await GDTask.SwitchToMainThread(PlayerLoopTiming.Process, CancellationToken.None);
		GD.Print("MainThread: ThreadId =" + Environment.CurrentManagedThreadId);
		await using (GDTask.ReturnToMainThread(CancellationToken.None))
		{
			await GDTask.SwitchToThreadPool();
			GD.Print("ThreadPool: ThreadId =" + Environment.CurrentManagedThreadId);
		}
		GD.Print("MainThread: ThreadId =" + Environment.CurrentManagedThreadId);
		await using (GDTask.ReturnToMainThread(PlayerLoopTiming.Process, CancellationToken.None))
		{
			await GDTask.SwitchToThreadPool();
			GD.Print("ThreadPool: ThreadId =" + Environment.CurrentManagedThreadId);
			GDTask.Post(() => GD.Print("ThreadPool Post: ThreadId =" + Environment.CurrentManagedThreadId), PlayerLoopTiming.Process);
		}
		GD.Print("MainThread: ThreadId =" + Environment.CurrentManagedThreadId);
		await GDTask.SwitchToSynchronizationContext(SynchronizationContext.Current, CancellationToken.None);
		GD.Print("SynchronizationContext: ThreadId =" + Environment.CurrentManagedThreadId);
		await using (GDTask.ReturnToSynchronizationContext(SynchronizationContext.Current, CancellationToken.None))
		{
			await GDTask.SwitchToThreadPool();
			GD.Print("ThreadPool: ThreadId =" + Environment.CurrentManagedThreadId);
		}

		await using (GDTask.ReturnToCurrentSynchronizationContext(true, CancellationToken.None))
		{
			await GDTask.SwitchToThreadPool();
			GD.Print("ThreadPool: ThreadId =" + Environment.CurrentManagedThreadId);
		}
		
		await GDTask.WaitUntil(() => true, PlayerLoopTiming.Process, CancellationToken.None);
		await GDTask.WaitWhile(() => false, PlayerLoopTiming.Process, CancellationToken.None);
		await GDTask.WaitUntilCanceled(CancellationToken.None, PlayerLoopTiming.Process);
		GDTask.WaitUntilValueChanged((object)null, obj => Time.GetTicksMsec(), PlayerLoopTiming.Process, null, CancellationToken.None);


		int[] resultArray;
		await GDTask.WhenAll(GDTask.CompletedTask);
		await GDTask.WhenAll(Enumerable.Repeat(GDTask.CompletedTask, 20));
		resultArray = await GDTask.WhenAll(GDTask.FromResult(1));
		resultArray = await GDTask.WhenAll(Enumerable.Range(0, 20).Select(GDTask.FromResult));	
		(result, result) = await GDTask.WhenAll(GDTask.FromResult(1), GDTask.FromResult(2));
		
		result = await GDTask.WhenAny(GDTask.CompletedTask);
		result = await GDTask.WhenAny(Enumerable.Repeat(GDTask.CompletedTask, 20));
		(result, result) = await GDTask.WhenAny(GDTask.FromResult(1));
		(result, result) = await GDTask.WhenAny(Enumerable.Range(0, 20).Select(GDTask.FromResult));	
		(result, result, result) = await GDTask.WhenAny(GDTask.FromResult(1), GDTask.FromResult(2));
		
		
	}

	public UsageTest(Node testScene) : base(testScene) { }
}