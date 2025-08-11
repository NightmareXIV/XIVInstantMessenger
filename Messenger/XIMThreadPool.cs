using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger;
public class XIMThreadPool : ThreadPool
{
    public static int NumThreads => Math.Clamp(Environment.ProcessorCount / 4, 1, 4);
    public XIMThreadPool() : base(NumThreads)
    {
        PluginLog.Debug($"Using no more than {NumThreads} threads for thread pool");
    }
}