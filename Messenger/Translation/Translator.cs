using Messenger.Translation.Providers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Messenger.Translation
{
    internal class Translator : IDisposable
    {
        internal List<ITranslationProvider> RegisteredProviders;
        internal ITranslationProvider CurrentProvider;

        volatile bool IsRunning = false;
        volatile bool Terminate = false;
        internal ConcurrentDictionary<string, string> TranslationResults = new();
        ConcurrentQueue<string> TranslationQueue = new();
        
        internal Translator()
        {
            RegisteredProviders = new()
            {
                new DummyProvider()
            };
            CurrentProvider = RegisteredProviders.FirstOrDefault(x => x.DisplayName == P.config.TranslationProvider);
        }

        internal void EnqueueTranslation(string s)
        {
            if (TranslationResults.ContainsKey(s))
            {
                PluginLog.Verbose($"Message {s} already translated, ignoring");
                return;
            }
            TranslationQueue.Enqueue(s);
            if (!IsRunning)
            {
                StartThread();
            }
        }

        public void Dispose()
        {
            Terminate = true;
            CurrentProvider?.Dispose();
        }

        void StartThread()
        {
            IsRunning = true;
            new Thread(() =>
            {
                int Idle = 0;
                try
                {
                    while (!Terminate && Idle < 1000)
                    {
                        if(TranslationQueue.TryDequeue(out var msg))
                        {
                            Idle = 0;
                            try
                            {
                                TranslationResults[msg] = CurrentProvider.TranslateSynchronous(msg);
                            }
                            catch(Exception e)
                            {
                                e.Log();
                            }
                        }
                        else
                        {
                            Idle++;
                            Thread.Sleep(250);
                        }
                    }
                }
                catch(Exception e)
                {
                    e.Log();
                }
                IsRunning = false;
            }).Start();
        }
    }
}
