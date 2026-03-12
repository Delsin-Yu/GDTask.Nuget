using System;
using System.Runtime.CompilerServices;

namespace GodotTask.Internal
{
    internal interface IPlayerLoopChannel
    {
        bool IsCurrentThreadLoopThread { get; }
        double DeltaTime { get; }
        ulong FrameCount { get; }
        void AddAction(IPlayerLoopItem action);
        void AddContinuation(Action continuation);
        int Clear();
    }

    internal sealed class PlayerLoopChannel : IPlayerLoopChannel
    {
        private readonly ContinuationQueue continuationQueue = new();
        private readonly PlayerLoopRunner runner = new();
        private readonly Func<bool> isCurrentThreadLoopThread;

        public PlayerLoopChannel(Func<bool> isCurrentThreadLoopThread)
        {
            this.isCurrentThreadLoopThread = isCurrentThreadLoopThread;
        }

        public bool IsCurrentThreadLoopThread => isCurrentThreadLoopThread();

        public double DeltaTime { get; private set; }

        public ulong FrameCount { get; private set; }

        public void AddAction(IPlayerLoopItem action)
        {
            runner.AddAction(action);
        }

        public void AddContinuation(Action continuation)
        {
            continuationQueue.Enqueue(continuation);
        }

        public int Clear()
        {
            return continuationQueue.Clear() + runner.Clear();
        }

        public void Run(double delta)
        {
            DeltaTime = delta;
            FrameCount++;
            continuationQueue.Run();
            runner.Run();
        }
    }

    internal sealed class CustomPlayerLoopChannels
    {
        private readonly PlayerLoopChannel processChannel;
        private readonly PlayerLoopChannel physicsProcessChannel;
        private int processThreadId;
        private int physicsProcessThreadId;

        public CustomPlayerLoopChannels(ICustomPlayerLoop playerLoop)
        {
            processChannel = new PlayerLoopChannel(() => processThreadId != 0 && Environment.CurrentManagedThreadId == processThreadId);
            physicsProcessChannel = new PlayerLoopChannel(() => physicsProcessThreadId != 0 && Environment.CurrentManagedThreadId == physicsProcessThreadId);

            playerLoop.OnProcess += ProcessChannelRun;
            playerLoop.OnPhysicsProcess += PhysicsProcessChannelRun;
        }

        public IPlayerLoopChannel GetChannel(PlayerLoopTiming timing)
        {
            return timing switch
            {
                PlayerLoopTiming.Process => processChannel,
                PlayerLoopTiming.PhysicsProcess => physicsProcessChannel,
                _ => throw new ArgumentOutOfRangeException(nameof(timing), timing, "Custom player loops only support Process and PhysicsProcess timings."),
            };
        }

        private void ProcessChannelRun(double delta)
        {
            processThreadId = Environment.CurrentManagedThreadId;
            processChannel.Run(delta);
        }

        private void PhysicsProcessChannelRun(double delta)
        {
            physicsProcessThreadId = Environment.CurrentManagedThreadId;
            physicsProcessChannel.Run(delta);
        }
    }

    internal static class CustomPlayerLoopRegistry
    {
        private static readonly ConditionalWeakTable<ICustomPlayerLoop, CustomPlayerLoopChannels> loops = new();

        public static IPlayerLoopChannel GetChannel(ICustomPlayerLoop playerLoop, PlayerLoopTiming timing)
        {
            ArgumentNullException.ThrowIfNull(playerLoop);
            return loops.GetValue(playerLoop, static loop => new CustomPlayerLoopChannels(loop)).GetChannel(timing);
        }
    }
}
