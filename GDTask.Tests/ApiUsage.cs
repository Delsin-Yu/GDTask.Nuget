using System;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Environment = System.Environment;

namespace GodotTask.Tests;

public partial class ApiUsage : Node
{
	public override void _Ready()
	{
		base._Ready();
		// TaskTracker.ShowTrackerWindow();
		ApiUsage_Method().Forget();
	}

	public static async GDTask ApiUsage_Method()
	{
		await GDTaskTest_Threading.GDTask_SwitchToThreadPool();
		await GDTaskTest_Threading.GDTask_SwitchToMainThread_Process();
		await GDTaskTest_Threading.GDTask_SwitchToMainThread_Process_Token();
		await GDTaskTest_Threading.GDTask_SwitchToMainThread_CustomPlayerLoop();
		
		await GDTask.SwitchToMainThread();
		
		// Delay the execution after frame(s).
		await GDTask.DelayFrame(100);

		// Delay the execution after delayTimeSpan.
		await GDTask.Delay(TimeSpan.FromSeconds(1), DelayType.Realtime);

		// Delay the execution until the next Process.
		await GDTask.Yield(PlayerLoopTiming.Process);

		// The same APIs also accept any IPlayerLoop implementation.
		IPlayerLoop processLoop = PlayerLoopRunnerProvider.Process;
		await GDTask.Delay(TimeSpan.FromMilliseconds(10), DelayType.DeltaTime, processLoop);

		// Delay the execution until the next PhysicsProcess.
		await GDTask.WaitForPhysicsProcess();

		// Creates a task that will complete at the next provided PlayerLoopTiming when the supplied predicate evaluates to true
		await GDTask.WaitUntil(() => Time.GetTimeDictFromSystem()["second"].AsInt32() % 2 == 0);
		
		// Creates a task that will complete at the next provided PlayerLoopTiming when the provided monitorFunction returns a different value.
		await GDTask.WaitUntilValueChanged(Time.Singleton, timeInstance => timeInstance.GetTimeDictFromSystem()["second"]);
		
		// Creates an awaitable that asynchronously yields to ThreadPool when awaited.
		await GDTask.SwitchToThreadPool();
		
		/* Threaded work */
		GD.Print("Current Thread: " + Environment.CurrentManagedThreadId);
		
		// Creates an awaitable that asynchronously yields back to the next Process from the main thread when awaited.
		await GDTask.SwitchToMainThread();
		
		/* Main thread work */
		GD.Print("Current Thread: " + Environment.CurrentManagedThreadId);
		
		await GDTask.NextFrame();

		// Creates a continuation that executes when the target GDTask completes.
		int taskResult = await GDTask.Delay(10).ContinueWith(() => 5);
		
		GDTask<int> task1 = GDTask.Delay(10).ContinueWith(() => 5);
		GDTask<string> task2 = GDTask.Delay(20).ContinueWith(() => "Task Result");
		GDTask<bool> task3 = GDTask.Delay(30).ContinueWith(() => true);

		// Creates a task that will complete when all of the supplied tasks have completed.
		var (task1Result, task2Result, task3Result) = await GDTask.WhenAll(task1, task2, task3);

		try
		{
			// Creates a GDTask that has completed with the specified exception.
			await GDTask.FromException(new ExpectedException());
		}
		catch (ExpectedException) { }
		
		try
		{
			// Creates a GDTask that has completed due to cancellation with the specified cancellation token.
			await GDTask.FromCanceled(CancellationToken.None);
		}
		catch (OperationCanceledException) { }
		
		// Or use an alternative pattern to handle cancellation without exception:
		var isCanceled = await GDTask.FromCanceled(CancellationToken.None).SuppressCancellationThrow();
		GD.Print(isCanceled); // Output: True
		
		try
		{
			var source = new CancellationTokenSource();
			source.CancelAfter(100);
			// Creates a task that never completes, with specified CancellationToken.
			await GDTask.Never(source.Token);
		}
		catch (OperationCanceledException) { }
		
		// Queues the specified work to run on the ThreadPool and returns a GDTask handle for that work.
		await GDTask.RunOnThreadPool(
			() => GD.Print(Environment.CurrentManagedThreadId.ToString()),
			configureAwait: true,
			cancellationToken: CancellationToken.None
		);

		// Create a GDTask that wraps around this task.
		await Task.Delay(5).AsGDTask(useCurrentSynchronizationContext: true);

		// Associate a time out to the current GDTask.
		try
		{
			await GDTask.Never(CancellationToken.None).Timeout(TimeSpan.FromMilliseconds(5));
		}
		catch (TimeoutException)
		{
			
		}
		
	}
}
