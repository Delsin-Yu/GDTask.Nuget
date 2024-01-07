
using Godot;
using System;

namespace Fractural.Tasks.Internal
{
    internal sealed class PlayerLoopRunner
    {
        const int InitialSize = 16;

        readonly PlayerLoopTiming timing;
        readonly object runningAndQueueLock = new object();
        readonly object arrayLock = new object();
        readonly Action<Exception> unhandledExceptionCallback;

        int tail = 0;
        bool running = false;
        IPlayerLoopItem[] loopItems = new IPlayerLoopItem[InitialSize];
        MinimumQueue<IPlayerLoopItem> waitQueue = new MinimumQueue<IPlayerLoopItem>(InitialSize);



        public PlayerLoopRunner(PlayerLoopTiming timing)
        {
            this.unhandledExceptionCallback = ex => GD.PrintErr(ex);
            this.timing = timing;
        }

        public void AddAction(IPlayerLoopItem item)
        {
            lock (runningAndQueueLock)
            {
                if (running)
                {
                    waitQueue.Enqueue(item);
                    return;
                }
            }

            lock (arrayLock)
            {
                // Ensure Capacity
                if (loopItems.Length == tail)
                {
                    Array.Resize(ref loopItems, checked(tail * 2));
                }
                loopItems[tail++] = item;
            }
        }

        public int Clear()
        {
            lock (arrayLock)
            {
                var rest = 0;

                for (var index = 0; index < loopItems.Length; index++)
                {
                    if (loopItems[index] != null)
                    {
                        rest++;
                    }

                    loopItems[index] = null;
                }

                tail = 0;
                return rest;
            }
        }

        // delegate entrypoint.
        public void Run()
        {
            // for debugging, create named stacktrace.
#if DEBUG
            switch (timing)
            {
                case PlayerLoopTiming.PhysicsProcess:
                    PhysicsProcess();
                    break;
                case PlayerLoopTiming.Process:
                    Process();
                    break;
            }
#else
            RunCore();
#endif
        }

        void PhysicsProcess() => RunCore();
        void Process() => RunCore();


        [System.Diagnostics.DebuggerHidden]
        void RunCore()
        {
            lock (runningAndQueueLock)
            {
                running = true;
            }

            lock (arrayLock)
            {
                var j = tail - 1;

                var loopItemSpan = loopItems.AsSpan();
                for (int i = 0; i < loopItemSpan.Length; i++)
                {
                    var action = loopItemSpan[i];
                    if (action != null)
                    {
                        try
                        {
                            if (!action.MoveNext())
                            {
                                loopItemSpan[i] = null;
                            }
                            else
                            {
                                continue; // next i 
                            }
                        }
                        catch (Exception ex)
                        {
                            loopItemSpan[i] = null;
                            try
                            {
                                unhandledExceptionCallback(ex);
                            }
                            catch { }
                        }
                    }

                    // find null, loop from tail
                    while (i < j)
                    {
                        var fromTail = loopItemSpan[j];
                        if (fromTail != null)
                        {
                            try
                            {
                                if (!fromTail.MoveNext())
                                {
                                    loopItemSpan[j] = null;
                                    j--;
                                    continue; // next j
                                }
                                else
                                {
                                    // swap
                                    loopItemSpan[i] = fromTail;
                                    loopItemSpan[j] = null;
                                    j--;
                                    goto NEXT_LOOP; // next i
                                }
                            }
                            catch (Exception ex)
                            {
                                loopItemSpan[j] = null;
                                j--;
                                try
                                {
                                    unhandledExceptionCallback(ex);
                                }
                                catch { }
                                continue; // next j
                            }
                        }
                        else
                        {
                            j--;
                        }
                    }

                    tail = i; // loop end
                    break; // LOOP END

                    NEXT_LOOP:
                    continue;
                }


                lock (runningAndQueueLock)
                {
                    running = false;
                    while (waitQueue.Count != 0)
                    {
                        if (loopItems.Length == tail)
                        {
                            Array.Resize(ref loopItems, checked(tail * 2));
                        }
                        loopItems[tail++] = waitQueue.Dequeue();
                    }
                }
            }
        }
    }
}

