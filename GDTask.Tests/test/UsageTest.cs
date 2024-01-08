using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Fractural.Tasks.Triggers;
using Godot;
using Environment = System.Environment;

namespace Fractural.Tasks.Tests;

public class UsageTest : TestClass
{
	public UsageTest(Node testScene) : base(testScene) { }

	
	public async GDTask UsageDemo()
	{
		// ReSharper disable RedundantAssignment
		// ReSharper disable once NotAccessedVariable
		// ReSharper disable once JoinDeclarationAndInitializer
		// ReSharper disable NotAccessedVariable
		
		int result;

		#region GDTask.Delay

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

		#endregion


		#region GDTasl.Factory

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

		#endregion

		
		#region GDTask.Threading

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

		#endregion

		
		#region GDTask.Wait

		await GDTask.WaitUntil(() => true, PlayerLoopTiming.Process, CancellationToken.None);
		await GDTask.WaitWhile(() => false, PlayerLoopTiming.Process, CancellationToken.None);
		await GDTask.WaitUntilCanceled(CancellationToken.None, PlayerLoopTiming.Process);
		GDTask.WaitUntilValueChanged((object)null, obj => Time.GetTicksMsec(), PlayerLoopTiming.Process, null, CancellationToken.None);

		#endregion

		
		#region GDTask.WhenAll

		int[] resultArray;
		await GDTask.WhenAll(GDTask.CompletedTask);
		await GDTask.WhenAll(Enumerable.Repeat(GDTask.CompletedTask, 20));
		resultArray = await GDTask.WhenAll(GDTask.FromResult(1));
		resultArray = await GDTask.WhenAll(Enumerable.Range(0, 20).Select(GDTask.FromResult));
		(result, result) = await GDTask.WhenAll(GDTask.FromResult(1), GDTask.FromResult(2));

		#endregion

		
		#region GDTask.WhenAny

		result = await GDTask.WhenAny(GDTask.CompletedTask);
		result = await GDTask.WhenAny(Enumerable.Repeat(GDTask.CompletedTask, 20));
		(result, result) = await GDTask.WhenAny(GDTask.FromResult(1));
		(result, result) = await GDTask.WhenAny(Enumerable.Range(0, 20).Select(GDTask.FromResult));
		(result, result, result) = await GDTask.WhenAny(GDTask.FromResult(1), GDTask.FromResult(2));

		#endregion

		
		#region GDTask.Extensions.Conversion

		await Task.Delay(5).AsGDTask(true);
		result = await Task.FromResult(5).AsGDTask(true);
		await GDTask.Delay(5).AsTask();
		result = await GDTask.FromResult(5).AsTask();
		await GDTask.Delay(5).ToAsyncLazy();
		result = await GDTask.FromResult(5).ToAsyncLazy();
		await GDTask.Delay(5).AttachExternalCancellation(CancellationToken.None);
		result = await GDTask.FromResult(5).AttachExternalCancellation(CancellationToken.None);

		#endregion

		
		#region GDTask.Extensions.Shorthand

		await new[] { GDTask.CompletedTask };
		await Enumerable.Repeat(GDTask.CompletedTask, 20);
		await new[] { GDTask.FromResult(1) };
		await Enumerable.Range(0, 20).Select(GDTask.FromResult);
		await (GDTask.FromResult(1), GDTask.FromResult(2));

		#endregion

		
		#region GDTask.Extensions.Observable

		var observable = new IntObservable();
		GDTask.Delay(100).ContinueWith(() => observable.UpdateValueAndFinish(5)).Forget();
		result = await observable.ToGDTask(true, CancellationToken.None);

		var asyncUnitObserver = new AsyncUnitObserver();
		var asyncUnitObservable = GDTask.Delay(1).ToObservable();
		using (asyncUnitObservable.Subscribe(asyncUnitObserver)) 
			await asyncUnitObserver;

		var unitObserver = new UnitObserver();
		var unitObservable = GDTask.Delay(1).ToObservable<Unit>();
		using (unitObservable.Subscribe(unitObserver)) 
			await asyncUnitObserver;

		var gdTaskObserver = new IntObserver();
		var gdTaskObservable = GDTask.Delay(1).ContinueWith(() => 5).ToObservable();
		using (gdTaskObservable.Subscribe(gdTaskObserver)) 
			await gdTaskObserver;

		#endregion

		#region GDTask.Extensions.Node

		var node = new Node();
		GDTask.Delay(1).ContinueWith(() => TestScene.AddChild(node)).Forget();
		await node.OnEnterTreeAsync();
		await node.OnReadyAsync();
		await node.GetAsyncProcessTrigger().OnProcessAsync();
		await node.GetAsyncPhysicsProcessTrigger().OnPhysicsProcessAsync();
		var predeleteCancellationToken = node.GetAsyncPredeleteCancellationToken();
		GDTask
			.Delay(15, cancellationToken: predeleteCancellationToken)
			.SuppressCancellationThrow()
			.ContinueWith(isCanceled => GD.Print(isCanceled))
			.Forget();
		node.QueueFree();
		await node.OnPredeleteAsync();

		#endregion
	}

	private class AsyncUnitObserver : AwaitableObserver<AsyncUnit> { }
	private class UnitObserver : AwaitableObserver<Unit> { }
	private class IntObserver : AwaitableObserver<int> { }

	
	private class AwaitableObserver<T> : IObserver<T> where T : struct
	{
		private T? value;
		private bool _isCompleted;
		void IObserver<T>.OnError(Exception error) { }
		void IObserver<T>.OnNext(T newValue) => value = newValue;
		void IObserver<T>.OnCompleted() => _isCompleted = true;
		public GDTask.Awaiter GetAwaiter() => GDTask.WaitUntil(() => _isCompleted).GetAwaiter();
	}
	
	private class IntObservable : IObservable<int>
	{
		private readonly List<IObserver<int>> _observers = new();

		public IDisposable Subscribe(IObserver<int> observer)
		{
			if (!_observers.Contains(observer))
				_observers.Add(observer);
			return new UnSubscriber(_observers, observer);
		}

		public void UpdateValueAndFinish(int newValue)
		{
			foreach (var observer in _observers) observer.OnNext(newValue);
			foreach (var observer in _observers.ToArray()) observer.OnCompleted();
			_observers.Clear();
		}

		private class UnSubscriber : IDisposable
		{
			private readonly List<IObserver<int>>_observers;
			private readonly IObserver<int> _observer;

			public UnSubscriber(List<IObserver<int>> observers, IObserver<int> observer)
			{
				_observers = observers;
				_observer = observer;
			}

			public void Dispose()
			{
				if (_observer != null && _observers.Contains(_observer))
					_observers.Remove(_observer);
			}
		}
	}
}