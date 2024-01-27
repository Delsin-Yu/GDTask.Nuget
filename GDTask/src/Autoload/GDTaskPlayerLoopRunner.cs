using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Godot;
using GodotTask.Tasks.Internal;

[assembly: InternalsVisibleTo("GDTask.Tests")]
namespace GodotTask.Tasks
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

        private GDTaskPlayerLoopRunner() { }

        public static GDTaskPlayerLoopRunner Global
        {
            get
            {
                if (s_Global != null) return s_Global;

                SynchronizationContext.SetSynchronizationContext(new GDTaskSynchronizationContext());
                var newInstance = new GDTaskPlayerLoopRunner();
                newInstance.Initialize();
                var root = ((SceneTree)Engine.GetMainLoop()).Root;
                root.CallDeferred(Node.MethodName.AddChild, newInstance);
                root.CallDeferred(Node.MethodName.MoveChild, newInstance, 0);
                newInstance.Name = "GDTaskPlayerLoopAutoload";
                s_Global = newInstance;

                return s_Global;
            }
        }
        public double DeltaTime => GetProcessDeltaTime();
        public double PhysicsDeltaTime => GetPhysicsProcessDeltaTime();

        private static GDTaskPlayerLoopRunner s_Global;
        private int mainThreadId;
        private ContinuationQueue[] yielders;
        private PlayerLoopRunner[] runners;

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
            yielders = new[] {
                new ContinuationQueue(PlayerLoopTiming.Process),
                new ContinuationQueue(PlayerLoopTiming.PhysicsProcess),
            };
            runners = new[] {
                new PlayerLoopRunner(PlayerLoopTiming.Process),
                new PlayerLoopRunner(PlayerLoopTiming.PhysicsProcess),
            };
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
            GDTaskSynchronizationContext.Run();
        }

        public override void _PhysicsProcess(double delta)
        {
            yielders[(int)PlayerLoopTiming.PhysicsProcess].Run();
            runners[(int)PlayerLoopTiming.PhysicsProcess].Run();
        }
    }
}

