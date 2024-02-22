﻿#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace GodotTask.CompilerServices
{
    [StructLayout(LayoutKind.Auto)]
    public struct AsyncGDTaskVoidMethodBuilder
    {
        private IStateMachineRunner runner;

        // 1. Static Create method.
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AsyncGDTaskVoidMethodBuilder Create()
        {
            return default;
        }

        // 2. TaskLike Task property(void)
        public GDTaskVoid Task
        {
            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => default;
        }

        // 3. SetException
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetException(Exception exception)
        {
            // runner is finished, return first.
            if (runner != null)
            {
                runner.Return();
                runner = null;
            }

            GDTaskExceptionHandler.PublishUnobservedTaskException(exception);
        }

        // 4. SetResult
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetResult()
        {
            // runner is finished, return.
            if (runner != null)
            {
                runner.Return();
                runner = null;
            }
        }

        // 5. AwaitOnCompleted
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            if (runner == null)
            {
                AsyncGDTaskVoid<TStateMachine>.SetStateMachine(ref stateMachine, ref runner);
            }

            awaiter.OnCompleted(runner.MoveNext);
        }

        // 6. AwaitUnsafeOnCompleted
        [DebuggerHidden]
        [SecuritySafeCritical]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            if (runner == null)
            {
                AsyncGDTaskVoid<TStateMachine>.SetStateMachine(ref stateMachine, ref runner);
            }

            awaiter.UnsafeOnCompleted(runner.MoveNext);
        }

        // 7. Start
        [DebuggerHidden]
        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine
        {
            stateMachine.MoveNext();
        }

        // 8. SetStateMachine
        [DebuggerHidden]
        public void SetStateMachine(IAsyncStateMachine stateMachine)
        {
            // don't use boxed stateMachine.
        }

#if DEBUG
        // Important for IDE debugger.
        private object debuggingId;
        private object ObjectIdForDebugger
        {
            get
            {
                if (debuggingId == null)
                {
                    debuggingId = new object();
                }
                return debuggingId;
            }
        }
#endif
    }
}