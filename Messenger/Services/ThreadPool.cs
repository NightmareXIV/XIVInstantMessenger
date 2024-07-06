using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Messenger.Services;
public class ThreadPool : IDisposable
{
    private readonly int MaxThreads = 4;
    private ConcurrentQueue<Action> TaskQueue = [];
    private volatile uint ThreadNum = 0;
    private volatile bool Disposed = false;
    public ThreadPool()
    {
    }

    public ThreadPool(int maxThreads)
    {
        MaxThreads = maxThreads;
    }

    public void Dispose()
    {
        Disposed = true;
    }

    public void Run(Action task)
    {
        TaskQueue.Enqueue(task);
        var num = Math.Max(1, Math.Min(MaxThreads, TaskQueue.Count));
        if (ThreadNum < num)
        {
            PluginLog.Verbose($"{ThreadNum} threads running, Creating new thread to deal with tasks...");
            ThreadNum++;
            new Thread(ThreadRun).Start();
        }
        else
        {
            //PluginLog.Verbose($"{num} threads already running, no new!");
        }
    }

    private void ThreadRun()
    {
        var uniqueID = $"{Random.Shared.Next():X8}";
        PluginLog.Verbose($"Thread {uniqueID} begins!");
        int idleTicks = 0;
        while (!Disposed)
        {
            if (TaskQueue.TryDequeue(out var result))
            {
                idleTicks = 0;
                try
                {
                    result();
                }
                catch (Exception ex)
                {
                    ex.Log();
                }
            }
            else
            {
                idleTicks++;
                Thread.Sleep(100);
                if (idleTicks > 100 || Disposed)
                {
                    ThreadNum--;
                    break;
                }
            }
        }
        PluginLog.Verbose($"Thread {uniqueID} ends!");
    }
}
