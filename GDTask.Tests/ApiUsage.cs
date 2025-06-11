using System;
using System.Threading;
using System.Threading.Tasks;
using GodotTask.Triggers;
using Godot;
using Environment = System.Environment;

namespace GodotTask.Tests;

public partial class ApiUsage : Node
{
	public override void _Ready()
	{
		base._Ready();
		TaskTracker.ShowTrackerWindow();
		ApiUsage_Method().Forget();
	}

	public static async GDTask ApiUsage_Method()
	{
		// Delay the execution after frame(s).
		await GDTask.DelayFrame(100); 
		
		// Delay the execution after delayTimeSpan.
		await GDTask.Delay(TimeSpan.FromSeconds(10), DelayType.Realtime);
    
		// Delay the execution until the next Process.
		await GDTask.Yield(PlayerLoopTiming.Process);

		// Delay the execution until the next PhysicsProcess.
		await GDTask.WaitForPhysicsProcess();

		// Creates a task that will complete at the next provided PlayerLoopTiming when the supplied predicate evaluates to true
		await GDTask.WaitUntil(() => Time.GetTimeDictFromSystem()["minute"].AsInt32() % 2 == 0);
		
		// Creates a task that will complete at the next provided PlayerLoopTiming when the provided monitorFunction returns a different value.
		await GDTask.WaitUntilValueChanged(Time.Singleton, timeInstance => timeInstance.GetTimeDictFromSystem()["minute"]);
		
		// Creates an awaitable that asynchronously yields to ThreadPool when awaited.
		await GDTask.SwitchToThreadPool();
		
		/* Threaded work */
		
		// Creates an awaitable that asynchronously yields back to the next Process from the main thread when awaited.
		await GDTask.SwitchToMainThread();
		
		/* Main thread work */
		
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
		catch (TaskCanceledException) { }
		
		try
		{
			var source = new CancellationTokenSource();
			source.CancelAfter(100);
			// Creates a task that never completes, with specified CancellationToken.
			await GDTask.Never(source.Token);
		}
		catch (TaskCanceledException) { }
		
		// Queues the specified work to run on the ThreadPool and returns a GDTask handle for that work.
		await GDTask.RunOnThreadPool(
			() => GD.Print(Environment.CurrentManagedThreadId.ToString()),
			configureAwait: true,
			cancellationToken: CancellationToken.None
		);

		// Create a GDTask that wraps around this task.
		await Task.Delay(5).AsGDTask(useCurrentSynchronizationContext: true);

		// Associate a time out to the current GDTask.
		await GDTask.Never(CancellationToken.None).Timeout(TimeSpan.FromMilliseconds(5));

		var node = new Node();
		((SceneTree)Engine.GetMainLoop()).Root.CallDeferred(Node.MethodName.AddChild, node);
		
		// Creates a task that will complete when the _EnterTree() is called.
		await node.OnEnterTreeAsync();
		
		// Creates a task that will complete when the _Ready() is called.
		await node.OnReadyAsync();
		
		// Gets an instance of IAsyncProcessHandler for making repeatedly calls on OnProcessAsync().
		var processTrigger = node.GetAsyncProcessTrigger();
		
		// Creates a task that will complete when the next _Process(double) is called.
		await processTrigger.OnProcessAsync();
		await processTrigger.OnProcessAsync();
		await processTrigger.OnProcessAsync();
		await processTrigger.OnProcessAsync();
		
		// Gets an instance of IAsyncPhysicsProcessHandler for making repeatedly calls on OnPhysicsProcessAsync().
		var physicsProcessTrigger = node.GetAsyncPhysicsProcessTrigger();
		await physicsProcessTrigger.OnPhysicsProcessAsync();
		await physicsProcessTrigger.OnPhysicsProcessAsync();
		await physicsProcessTrigger.OnPhysicsProcessAsync();
		await physicsProcessTrigger.OnPhysicsProcessAsync();	
		
		node.QueueFree();
		
		// Creates a task that will complete when the Node is receiving NotificationPredelete.
		await node.OnPredeleteAsync();
	}
}