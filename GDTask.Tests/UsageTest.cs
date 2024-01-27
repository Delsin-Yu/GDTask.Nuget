using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using GodotTask.Tasks.Triggers;
using Godot;
using Environment = System.Environment;

namespace GodotTask.Tasks.Tests;

public class UsageTest
{
	public async GDTask UsageDemo()
	{
		// ReSharper disable RedundantAssignment
		// ReSharper disable once NotAccessedVariable
		// ReSharper disable once JoinDeclarationAndInitializer
		// ReSharper disable NotAccessedVariable
		
		int result;

		#region GDTask

		var gdTask = GDTask.Delay(1);
		GD.Print(gdTask.Status);
		GD.Print(gdTask.ToString());
		var completed = false;
		gdTask.GetAwaiter().OnCompleted(() => completed = true);
		await GDTask.WaitUntil(() => completed);
		var cancellationTokenSource = new CancellationTokenSource();
		cancellationTokenSource.CancelAfter(1);
		gdTask = GDTask.Delay(10, cancellationToken: cancellationTokenSource.Token).SuppressCancellationThrow();
		await gdTask;
		gdTask = GDTask.Delay(1);
		gdTask = gdTask.Preserve();
		await gdTask;
		await gdTask;
		await gdTask;
		await gdTask;
		var asyncUnitGDTask = GDTask.Delay(1).AsAsyncUnitGDTask();
		await asyncUnitGDTask;
				
		var intGDTask = GDTask.Delay(1).ContinueWith(() => 5);
		GD.Print(intGDTask.Status);
		GD.Print(intGDTask.ToString());
		completed = false;
		intGDTask.GetAwaiter().OnCompleted(() => completed = true);
		await GDTask.WaitUntil(() => completed);
		cancellationTokenSource = new();
		cancellationTokenSource.CancelAfter(1);
		gdTask = GDTask.Delay(10, cancellationToken: cancellationTokenSource.Token).ContinueWith(() => 5).SuppressCancellationThrow();
		await intGDTask;
		intGDTask = GDTask.Delay(1).ContinueWith(() => 5);
		intGDTask = intGDTask.Preserve();
		result = await intGDTask;
		result = await intGDTask;
		result = await intGDTask;
		result = await intGDTask;
		gdTask = GDTask.Delay(1).ContinueWith(() => 5);
		await gdTask;
		
		#endregion
		
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
		await GDTask.WaitUntilValueChanged((object)null, obj => Time.GetTicksMsec(), PlayerLoopTiming.Process, null, CancellationToken.None);

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

		cancellationTokenSource = new CancellationTokenSource();
		try
		{
			await GDTask.Never(CancellationToken.None).Timeout(TimeSpan.FromMilliseconds(5), DelayType.DeltaTime, PlayerLoopTiming.Process, cancellationTokenSource);
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
		}
		var isCancellationRequested = cancellationTokenSource.IsCancellationRequested;
		cancellationTokenSource = new();
		try
		{
			_ = await GDTask.Never<int>(CancellationToken.None).Timeout(TimeSpan.FromMilliseconds(5), DelayType.DeltaTime, PlayerLoopTiming.Process, cancellationTokenSource);
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
		}
		isCancellationRequested = cancellationTokenSource.IsCancellationRequested;
		
		cancellationTokenSource = new();
		var isCanceled = await GDTask.Never(CancellationToken.None).TimeoutWithoutException(TimeSpan.FromMilliseconds(5), DelayType.DeltaTime, PlayerLoopTiming.Process, cancellationTokenSource);
		isCancellationRequested = cancellationTokenSource.IsCancellationRequested;

		cancellationTokenSource = new();
		(isCanceled, result) = await GDTask.Never<int>(CancellationToken.None).TimeoutWithoutException(TimeSpan.FromMilliseconds(5), DelayType.DeltaTime, PlayerLoopTiming.Process, cancellationTokenSource);
		isCancellationRequested = cancellationTokenSource.IsCancellationRequested;

		var continued = false;
		GDTask.Delay(1).ContinueWith(void () => continued = true).Forget();
		await GDTask.WaitUntil(() => continued);
		Exception exception = null;
		async GDTask ThrowTask()
		{
			await GDTask.Yield();
			throw new();
		}
		ThrowTask().Forget(exp => exception = exp);
		continued = false;
		GDTask.Delay(1).ContinueWith(() => continued = true).Forget();
		await GDTask.WaitUntil(() => continued);
		exception = null;
		async GDTask<int> ThrowTaskInt()
		{
			await GDTask.Yield();
			throw new();
		}
		ThrowTaskInt().Forget(exp => exception = exp);
		await GDTask.WaitUntil(() => exception != null);

		await GDTask.Delay(1).ContinueWith(() => 5).ContinueWith(val => { result = val; });
		await GDTask.Delay(1).ContinueWith(() => 5).ContinueWith(_ => GDTask.CompletedTask);
		var resultStr = await GDTask.Delay(1).ContinueWith(() => 5).ContinueWith(val => val.ToString());
		resultStr = await GDTask.Delay(1).ContinueWith(() => 5).ContinueWith(val => GDTask.FromResult(val.ToString()));
		await GDTask.Delay(1).ContinueWith(void () => result = 5);
		await GDTask.Delay(1).ContinueWith(() => GDTask.CompletedTask);
		result = await GDTask.Delay(1).ContinueWith(() => 5);
		result = await GDTask.Delay(1).ContinueWith(() => GDTask.FromResult(5));

		result = await GDTask.FromResult(GDTask.FromResult(5)).Unwrap();
		await GDTask.FromResult(GDTask.CompletedTask).Unwrap();
		result = await Task.FromResult(GDTask.FromResult(5)).Unwrap();
		result = await Task.FromResult(GDTask.FromResult(5)).Unwrap(true);
		await Task.FromResult(GDTask.CompletedTask).Unwrap();
		await Task.FromResult(GDTask.CompletedTask).Unwrap(true);
		result = await GDTask.FromResult(Task.FromResult(5)).Unwrap();
		result = await GDTask.FromResult(Task.FromResult(5)).Unwrap(true);
		await GDTask.FromResult(Task.CompletedTask).Unwrap();
		await GDTask.FromResult(Task.CompletedTask).Unwrap(true);

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
		// GDTask.Delay(1).ContinueWith(() => TestScene.AddChild(node)).Forget();
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
		
		#region CancellationTokenEqualityComparer

		var isEqual = CancellationTokenEqualityComparer.Default.Equals(CancellationToken.None, CancellationToken.None);

		#endregion

		#region CancellationTokenExtensions

		var oneSecondsToken = GDTask.Delay(1).ToCancellationToken();
		await GDTask.Delay(5, cancellationToken: oneSecondsToken).SuppressCancellationThrow();
		oneSecondsToken = GDTask.Delay(5).ToCancellationToken(GDTask.Delay(1).ToCancellationToken());
		await GDTask.Delay(5, cancellationToken: oneSecondsToken).SuppressCancellationThrow();

		oneSecondsToken = GDTask.Delay(1).ContinueWith(() => 5).ToCancellationToken();
		await GDTask.Delay(5, cancellationToken: oneSecondsToken).SuppressCancellationThrow();
		oneSecondsToken = GDTask.Delay(5).ContinueWith(() => 5).ToCancellationToken(GDTask.Delay(1).ToCancellationToken());
		await GDTask.Delay(5, cancellationToken: oneSecondsToken).SuppressCancellationThrow();

		cancellationTokenSource = new CancellationTokenSource();
		(gdTask, var registration) = cancellationTokenSource.Token.ToGDTask();
		cancellationTokenSource.CancelAfter(20);
		await gdTask;
		await registration.DisposeAsync();
		cancellationTokenSource = new();
		cancellationTokenSource.CancelAfter(20);
		await cancellationTokenSource.Token.WaitUntilCanceled();

		var disposable = new AwaitableDisposable();
		cancellationTokenSource = new();
		cancellationTokenSource.CancelAfter(20);
		disposable.AddTo(cancellationTokenSource.Token);
		await disposable;

		#endregion

		#region CancellationTokenExtensions

		cancellationTokenSource = new();
		cancellationTokenSource.CancelAfterSlim(1);
		(gdTask, _) = cancellationTokenSource.Token.ToGDTask();
		await gdTask;
		cancellationTokenSource = new();
		cancellationTokenSource.CancelAfterSlim(TimeSpan.FromMilliseconds(1));
		(gdTask, _) = cancellationTokenSource.Token.ToGDTask();
		await gdTask;
		cancellationTokenSource = new();
		node = new();
		cancellationTokenSource.RegisterRaiseCancelOnPredelete(node);
		node.QueueFree();
		(gdTask, _) = cancellationTokenSource.Token.ToGDTask();
		await gdTask;

		#endregion

		#region GDTask Synchornization Context

		var currentContext = (GDTaskSynchronizationContext)SynchronizationContext.Current!;

		await using (GDTask.ReturnToMainThread())
		{
			await GDTask.SwitchToThreadPool();
			currentContext.Send(_ => GD.Print("SynchronizationContext Send: ThreadId =" + Environment.CurrentManagedThreadId), null);
			currentContext.Post(_ => GD.Print("SynchronizationContext Post: ThreadId =" + Environment.CurrentManagedThreadId), null);
		}
		
		#endregion
	}

	private class AwaitableDisposable : IDisposable
	{
		private bool _disposed;

		public GDTask.Awaiter GetAwaiter() => 
			GDTask.WaitUntil(() => _disposed).GetAwaiter();
		
		public void Dispose()
		{
			_disposed = true;
		}
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