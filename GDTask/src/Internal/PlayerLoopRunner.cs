using Godot;
using System;
using System.Threading;

namespace GodotTask.Internal
{
    internal sealed class PlayerLoopRunner
    {
        private const int InitialSize = 16;

#if NET9_0_OR_GREATER
        private readonly Lock runningAndQueueLock = new();
        private readonly Lock arrayLock = new();
#else
        private readonly object runningAndQueueLock = new();
        private readonly object arrayLock = new();
#endif
        private readonly Action<Exception> unhandledExceptionCallback = ex => GD.PrintErr(ex);

        private int tail = 0;
        private bool running = false;
        private IPlayerLoopItem[] loopItems = new IPlayerLoopItem[InitialSize];
        private readonly MinimumQueue<IPlayerLoopItem> waitQueue = new(InitialSize);

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

        // Delegate entrypoint.
        public void Run(double deltaTime)
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
                            if (!action.MoveNext(deltaTime))
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
                                if (!fromTail.MoveNext(deltaTime))
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

