using System.Collections.Concurrent;
using System.IO;
using System.Threading;

namespace Messenger;

internal class Logger : IDisposable
{
    private BlockingCollection<LogTask> Tasks = [];
    internal Logger()
    {
        new Thread(() =>
        {
            try
            {
                while(!Tasks.IsCompleted)
                {
                    var task = Tasks.Take();
                    while(!task.History.LogLoaded)
                    {
                        PluginLog.Verbose("Waiting for log to be loaded first...");
                        Thread.Sleep(200);
                    }
                    Safe(delegate
                    {
                        File.AppendAllLines(task.History.LogFile, new string[] { task.Line });
                    });
                }
            }
            catch(InvalidOperationException)
            {
            }
            catch(Exception e)
            {
                e.Log();
            }
        }).Start();
    }

    internal void Log(LogTask task)
    {
        if(!Tasks.TryAdd(task))
        {
            S.ThreadPool.Run(() => Tasks.Add(task));
        }
    }

    public void Dispose()
    {
        Tasks.CompleteAdding();
    }
}
