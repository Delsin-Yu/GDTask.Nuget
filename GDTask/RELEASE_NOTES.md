## 3.2.1
- Fixed `TaskTracker` / `TypePrinter` throwing `ArgumentOutOfRangeException` when formatting async state machines declared in generic containing types (fixes [#57](https://github.com/Delsin-Yu/GDTask.Nuget/issues/57)).

## 3.2.0
- Restored public access to `AutoResetGDTaskCompletionSource` and `AutoResetGDTaskCompletionSource<T>` (fixes [#55](https://github.com/Delsin-Yu/GDTask.Nuget/issues/55)).
- Public surface is `Create()`, `Task`, and `TrySet*` only.
- Awaiter members (`GetResult` / `GetStatus` / `OnCompleted` / `UnsafeGetStatus`), `CreateFrom*` factories, and pool-internal `NextNode` remain non-public.

## 3.1.0
- Added `ContinueWithVoid` overloads on `GDTask` and `GDTask<T>` as fire-and-forget alternatives to `ContinueWith` that return `void` instead of `GDTask`, eliminating the need for `.Forget()`.
  - `GDTask.ContinueWithVoid(Action)`
  - `GDTask.ContinueWithVoid(Func<GDTaskVoid>)`
  - `GDTask<T>.ContinueWithVoid(Action<T>)`
  - `GDTask<T>.ContinueWithVoid(Action)`
  - `GDTask<T>.ContinueWithVoid(Func<T, GDTaskVoid>)`
  - `GDTask<T>.ContinueWithVoid(Func<GDTaskVoid>)

## 3.0.0
- Breaking: `AsyncTriggers` are no longer included in the main GDTask package.
- Breaking: non-generic object-state overloads of `RunOnThreadPool` were removed; use the generic `TState` overloads instead.
  - Changed `GDTask.WhenAll` to wait for every input before completing.
  - Changed `GDTask.WhenAny` and `GDTask.WhenEach` semantics to better align with Microsoft's official `Task.WhenAny` and `Task.WhenEach` behavior.
- Added `IPlayerLoop`-based overloads across scheduling, delay, timeout, cancellation, and waiting APIs.
- Added simpler `CancellationToken` overloads for `GDTask.WaitUntil`, `GDTask.WaitWhile`, `GDTask.Delay`, and `GDTask.DelayFrame`.
- Added collection-friendly coordination improvements, including broader `IEnumerable` support for `GDTask.WhenAll` and `GDTask.WhenAny`, plus shorthand await support for task collections.
- Added public pool configuration through `TaskPool.MaxPoolSize`.
- Fixed `GDTask.WhenEach`, `SwitchToMainThread`, `TimeoutWithoutException`, `AsyncLazy`, and `GDTaskCompletionSource` semantics in edge-case and concurrency scenarios.
