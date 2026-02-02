using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Godot;
using GodotTask.Internal;

[assembly: InternalsVisibleTo("GDTask.Tests")]
namespace GodotTask
{
    /// <summary>
    /// Indicates one of the functions from the player loop.
    /// </summary>
    public enum PlayerLoopTiming
    {
        /// <summary>
        /// The <see cref="Node._Process"/> from the player loop.
        /// </summary>
        Process = 0,
        
        /// <summary>
        /// The <see cref="Node._PhysicsProcess"/> from the player loop.
        /// </summary>
        PhysicsProcess = 1,
        
        /// <summary>
        /// The <see cref="Node._Process"/> from the player loop, but also runs when the scene tree has paused.
        /// </summary>
        IsolatedProcess = 2,
        
        /// <summary>
        /// The <see cref="Node._PhysicsProcess"/> from the player loop, but also runs when the scene tree has paused.
        /// </summary>
        IsolatedPhysicsProcess = 3,
    }

    internal interface IPlayerLoopItem
    {
        bool MoveNext();
    }

    /// <summary>
    /// Singleton that forwards Godot calls and values to GDTasks.
    /// </summary>
    internal partial class GDTaskPlayerLoopRunner : Node
    {
        public static int MainThreadId => Global.mainThreadId;
        public static bool IsMainThread => System.Environment.CurrentManagedThreadId == Global.mainThreadId;
        public static void AddAction(PlayerLoopTiming timing, IPlayerLoopItem action) => Global.LocalAddAction(timing, action);
        public static void ThrowInvalidLoopTiming(PlayerLoopTiming playerLoopTiming) => throw new InvalidOperationException("Target playerLoopTiming is not injected. Please check PlayerLoopHelper.Initialize. PlayerLoopTiming:" + playerLoopTiming);
        public static void AddContinuation(PlayerLoopTiming timing, Action continuation) => Global.LocalAddContinuation(timing, continuation);
        public static void AddDeferredAction(IPlayerLoopItem action) => Global.LocalAddDeferredAction(action);
        public static void AddDeferredContinuation(Action continuation) => Global.LocalAddDeferredContinuation(continuation);
        
        public void LocalAddAction(PlayerLoopTiming timing, IPlayerLoopItem action)
        {
            var runner = runners[(int)timing];
            if (runner == null)
            {
                ThrowInvalidLoopTiming(timing);
            }
            runner!.AddAction(action);
        }

        // NOTE: Continuation means a asynchronous task invoked by another task after the other task finishes.
        public void LocalAddContinuation(PlayerLoopTiming timing, Action continuation)
        {
            var q = yielders[(int)timing];
            if (q == null)
            {
                ThrowInvalidLoopTiming(timing);
            }
            q!.Enqueue(continuation);
        }
        
        public void LocalAddDeferredAction(IPlayerLoopItem action)
        {
            deferredRunner.AddAction(action);
        }
        
        public void LocalAddDeferredContinuation(Action continuation)
        {
            deferredYielder.Enqueue(continuation);
        }

        private GDTaskPlayerLoopRunner() { }

        public static GDTaskPlayerLoopRunner Global
        {
            get
            {
                RuntimeChecker.ThrowIfEditor();
                if (s_Global != null) return s_Global;

                var newInstance = new GDTaskPlayerLoopRunner();
                var isolatedPlayerLoopRunner = new IsolatedGDTaskPlayerLoopRunner(newInstance);
                Dispatcher.SynchronizationContext.Send(instance =>
                {
                    var runner = ((GDTaskPlayerLoopRunner)instance)!;
                    runner.Initialize();
                }, newInstance);
                var root = ((SceneTree)Engine.GetMainLoop()).Root;
                root.CallDeferred(Node.MethodName.AddChild, newInstance, false, Variant.From(InternalMode.Front));
                newInstance.Name = "GDTaskPlayerLoopRunner";
                isolatedPlayerLoopRunner.Name = "IsolatedGDTaskPlayerLoopRunner";
                newInstance.AddChild(isolatedPlayerLoopRunner);
                s_Global = newInstance;

                return s_Global;
            }
        }
        public double DeltaTime => GetProcessDeltaTime();
        public double PhysicsDeltaTime => GetPhysicsProcessDeltaTime();

        private static GDTaskPlayerLoopRunner s_Global;
        private int mainThreadId;
        private ContinuationQueue[] yielders;
        private ContinuationQueue deferredYielder;
        private PlayerLoopRunner[] runners;
        private PlayerLoopRunner deferredRunner;

        public override void _Ready()
        {
            if (s_Global == null)
            {
                Initialize();
                s_Global = this;
                return;
            }

            if (s_Global != this)
            {
                QueueFree();
            }
        }

        private void Initialize()
        {
            mainThreadId = System.Environment.CurrentManagedThreadId;
            yielders = [new(), new(), new(), new()];
            runners = [new(), new(), new(), new()];
            deferredYielder = new();
            deferredRunner = new();
        }

        public override void _Notification(int what)
        {
            if (what == NotificationPredelete)
            {
                if (Global == this)
                    s_Global = null;
                if (yielders != null)
                {
                    foreach (var yielder in yielders)
                        yielder.Clear();
                    foreach (var runner in runners)
                        runner.Clear();
                }
            }
        }

        public override void _Process(double delta)
        {
            yielders[(int)PlayerLoopTiming.Process].Run();
            runners[(int)PlayerLoopTiming.Process].Run();
            CallDeferred(MethodName.DeferredProcess);
        }

        public override void _PhysicsProcess(double delta)
        {
            yielders[(int)PlayerLoopTiming.PhysicsProcess].Run();
            runners[(int)PlayerLoopTiming.PhysicsProcess].Run();
        }

        public void PauseProcess()
        {
            yielders[(int)PlayerLoopTiming.IsolatedProcess].Run();
            runners[(int)PlayerLoopTiming.IsolatedProcess].Run();
        }

        public void PausePhysicsProcess()
        {
            yielders[(int)PlayerLoopTiming.IsolatedPhysicsProcess].Run();
            runners[(int)PlayerLoopTiming.IsolatedPhysicsProcess].Run();
        }

        public void DeferredProcess()
        {
            deferredYielder.Run();
            deferredRunner.Run();
        }
    }
}
