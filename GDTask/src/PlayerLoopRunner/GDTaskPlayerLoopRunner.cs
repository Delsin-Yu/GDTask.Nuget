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
        private static readonly ConditionalWeakTable<ICustomPlayerLoop, CustomPlayerLoopScheduler> customSchedulers = new();

        public static int MainThreadId => DefaultScheduler.MainThreadId;
        public static bool IsMainThread => DefaultScheduler.IsMainThread;
        public static void AddAction(PlayerLoopTiming timing, IPlayerLoopItem action) => DefaultScheduler.AddAction(timing, action);
        public static void ThrowInvalidLoopTiming(PlayerLoopTiming playerLoopTiming) => throw new InvalidOperationException("Target playerLoopTiming is not injected. Please check PlayerLoopHelper.Initialize. PlayerLoopTiming:" + playerLoopTiming);
        public static void AddContinuation(PlayerLoopTiming timing, Action continuation) => DefaultScheduler.AddContinuation(timing, continuation);
        public static void AddDeferredAction(IPlayerLoopItem action) => DefaultScheduler.AddDeferredAction(action);
        public static void AddDeferredContinuation(Action continuation) => DefaultScheduler.AddDeferredContinuation(continuation);

        internal static IPlayerLoopScheduler DefaultScheduler => Global.scheduler;

        internal static IPlayerLoopScheduler GetScheduler(ICustomPlayerLoop customPlayerLoop)
        {
            ArgumentNullException.ThrowIfNull(customPlayerLoop);
            return customSchedulers.GetValue(customPlayerLoop, static loop => new CustomPlayerLoopScheduler(loop));
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
        private DefaultPlayerLoopScheduler scheduler;

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
            scheduler = new DefaultPlayerLoopScheduler(System.Environment.CurrentManagedThreadId);
            scheduler.Initialize();
        }

        public override void _Notification(int what)
        {
            if (what == NotificationPredelete)
            {
                if (Global == this)
                    s_Global = null;
                if (scheduler != null)
                {
                    scheduler.Clear();
                }
            }
        }

        public override void _Process(double delta)
        {
            scheduler.DispatchProcess(delta);
            CallDeferred(MethodName.DeferredProcess);
        }

        public override void _PhysicsProcess(double delta)
        {
            scheduler.DispatchPhysicsProcess(delta);
        }

        public void PauseProcess(double delta)
        {
            scheduler.DispatchIsolatedProcess(delta);
        }

        public void PausePhysicsProcess(double delta)
        {
            scheduler.DispatchIsolatedPhysicsProcess(delta);
        }

        public void DeferredProcess()
        {
            scheduler.RunDeferred();
        }
    }
}
