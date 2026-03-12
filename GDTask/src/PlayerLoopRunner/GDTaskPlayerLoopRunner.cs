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
        internal static IPlayerLoopChannel GetLoop(PlayerLoopTiming timing) => Global.LocalGetLoop(timing);
        internal static IPlayerLoopChannel GetLoop(ICustomPlayerLoop customPlayerLoop, PlayerLoopTiming timing) => CustomPlayerLoopRegistry.GetChannel(customPlayerLoop, timing);
        public static void AddAction(PlayerLoopTiming timing, IPlayerLoopItem action) => GetLoop(timing).AddAction(action);
        public static void ThrowInvalidLoopTiming(PlayerLoopTiming playerLoopTiming) => throw new InvalidOperationException("Target playerLoopTiming is not injected. Please check PlayerLoopHelper.Initialize. PlayerLoopTiming:" + playerLoopTiming);
        public static void AddContinuation(PlayerLoopTiming timing, Action continuation) => GetLoop(timing).AddContinuation(continuation);
        public static void AddDeferredAction(IPlayerLoopItem action) => Global.LocalAddDeferredAction(action);
        public static void AddDeferredContinuation(Action continuation) => Global.LocalAddDeferredContinuation(continuation);

        public void LocalAddDeferredAction(IPlayerLoopItem action)
        {
            deferredChannel.AddAction(action);
        }
        
        public void LocalAddDeferredContinuation(Action continuation)
        {
            deferredChannel.AddContinuation(continuation);
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
        public double DeltaTime => channels[(int)PlayerLoopTiming.Process].DeltaTime;
        public double PhysicsDeltaTime => channels[(int)PlayerLoopTiming.PhysicsProcess].DeltaTime;

        private static GDTaskPlayerLoopRunner s_Global;
        private int mainThreadId;
        private PlayerLoopChannel[] channels;
        private PlayerLoopChannel deferredChannel;

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
            channels =
            [
                new(() => IsMainThread),
                new(() => IsMainThread),
                new(() => IsMainThread),
                new(() => IsMainThread),
            ];
            deferredChannel = new(() => IsMainThread);
        }

        private IPlayerLoopChannel LocalGetLoop(PlayerLoopTiming timing)
        {
            var index = (int)timing;
            if ((uint)index >= (uint)channels.Length)
            {
                ThrowInvalidLoopTiming(timing);
            }

            return channels[index];
        }

        public override void _Notification(int what)
        {
            if (what == NotificationPredelete)
            {
                if (Global == this)
                    s_Global = null;
                if (channels != null)
                {
                    foreach (var channel in channels)
                        channel.Clear();
                    deferredChannel?.Clear();
                }
            }
        }

        public override void _Process(double delta)
        {
            channels[(int)PlayerLoopTiming.Process].Run(delta);
            CallDeferred(MethodName.DeferredProcess);
        }

        public override void _PhysicsProcess(double delta)
        {
            channels[(int)PlayerLoopTiming.PhysicsProcess].Run(delta);
        }

        public void PauseProcess(double delta)
        {
            channels[(int)PlayerLoopTiming.IsolatedProcess].Run(delta);
        }

        public void PausePhysicsProcess(double delta)
        {
            channels[(int)PlayerLoopTiming.IsolatedPhysicsProcess].Run(delta);
        }

        public void DeferredProcess()
        {
            deferredChannel.Run(0.0);
        }
    }
}
