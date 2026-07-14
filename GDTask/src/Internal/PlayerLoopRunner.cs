using System;
using System.Collections.Generic;
using Godot;

namespace GodotTask.Internal;

sealed class PlayerLoopRunner
{
    private const int InitialSize = 16;

#if NET9_0_OR_GREATER
    private readonly System.Threading.Lock _runningAndQueueLock = new();
    private readonly System.Threading.Lock _arrayLock = new();
#else
    private readonly object _runningAndQueueLock = new();
    private readonly object _arrayLock = new();
#endif
    private readonly Action<Exception> _unhandledExceptionCallback = ex => GD.PrintErr(ex);

    private int _tail;
    private bool _running;
    private IPlayerLoopItem[] _loopItems = new IPlayerLoopItem[InitialSize];
    private readonly Queue<IPlayerLoopItem> _waitQueue = new(InitialSize);

    public void AddAction(IPlayerLoopItem item)
    {
        lock (_runningAndQueueLock)
        {
            if (_running)
            {
                _waitQueue.Enqueue(item);
                return;
            }
        }

        lock (_arrayLock)
        {
            // Ensure Capacity
            if (_loopItems.Length == _tail) Array.Resize(ref _loopItems, checked(_tail * 2));
            _loopItems[_tail++] = item;
        }
    }

    public int Clear()
    {
        lock (_arrayLock)
        {
            var rest = 0;

            for (var index = 0; index < _loopItems.Length; index++)
            {
                if (_loopItems[index] != null) rest++;

                _loopItems[index] = null;
            }

            _tail = 0;
            return rest;
        }
    }

    // Delegate entrypoint.
    public void Run(double deltaTime)
    {
        lock (_runningAndQueueLock) { _running = true; }

        lock (_arrayLock)
        {
            var j = _tail - 1;

            var loopItemSpan = _loopItems.AsSpan();

            for (var i = 0; i < loopItemSpan.Length; i++)
            {
                var action = loopItemSpan[i];

                if (action != null)
                    try
                    {
                        if (!action.MoveNext(deltaTime)) loopItemSpan[i] = null;
                        else continue; // next i 
                    }
                    catch (Exception ex)
                    {
                        loopItemSpan[i] = null;

                        try { _unhandledExceptionCallback(ex); }
                        catch { }
                    }

                // find null, loop from tail
                while (i < j)
                {
                    var fromTail = loopItemSpan[j];

                    if (fromTail != null)
                        try
                        {
                            if (!fromTail.MoveNext(deltaTime))
                            {
                                loopItemSpan[j] = null;
                                j--;
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

                            try { _unhandledExceptionCallback(ex); }
                            catch { }
                        }
                    else j--;
                }

                _tail = i; // loop end
                break; // LOOP END

                NEXT_LOOP: ;
            }

            lock (_runningAndQueueLock)
            {
                _running = false;

                while (_waitQueue.Count != 0)
                {
                    if (_loopItems.Length == _tail) Array.Resize(ref _loopItems, checked(_tail * 2));
                    _loopItems[_tail++] = _waitQueue.Dequeue();
                }
            }
        }
    }
}