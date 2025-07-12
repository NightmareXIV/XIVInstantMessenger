using ECommons.Networking;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ThreadPool = ECommons.Schedulers.ThreadPool;

namespace Messenger.Services.Translation;
public unsafe sealed class LibreTranslateRunner : IDisposable
{
    private Process TranslatorProcess;
    public ThreadPool LibreTranslateThreadPool = new(1);

    private Job Job
    {
        get
        {
            field ??= new();
            return field;
        }
    }

    private HttpClient TranslationClient
    {
        get
        {
            field ??= new HttpClient()
            {
                Timeout = TimeSpan.FromSeconds(30)
            }.ApplyProxySettings(C.ProxySettings);
            return field;
        }
    }

    public void Dispose()
    {
        LibreTranslateThreadPool.Dispose();
        TranslationClient?.Dispose();
        try
        {
            TranslatorProcess?.Kill();
        }
        catch(Exception e)
        {
            e.LogWarning();
        }
        try
        {
            Job?.Dispose();
        }
        catch(Exception e)
        {
            e.LogWarning();
        }
    }

    public void EnqueueTask(Guid guid, string message)
    {
        LibreTranslateThreadPool.Run(() => 
        {
            if(TranslatorProcess == null || TranslatorProcess.HasExited)
            {
                var newProc = LibreTranslationUtils.StartLibreTranslateServer(port: 17785);
                if(newProc != null)
                {
                    TranslatorProcess = newProc;
                    Job.AddProcess(TranslatorProcess);
                }
                else
                {
                    PluginLog.Error($"Could not start LibreTranslate. Ensure it is installed and ran manually at least once.");
                    return;
                }
                Thread.Sleep(1000);
                for(int i = 0; i < 20; i++)
                {
                    try
                    {
                        if(IsHttpPortResponsive("http://127.0.0.1:17785/"))
                        {
                            break;
                        }
                    }
                    catch(Exception e)
                    {
                        e.LogInternal();
                    }
                }
            }
            TranslateSync(guid, message);
        });
    }

    public bool IsHttpPortResponsive(string url)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, url);
            using var response = TranslationClient.Send(request);

            return response.IsSuccessStatusCode || (int)response.StatusCode < 500;
        }
        catch
        {
            return false;
        }
    }

    public void TranslateSync(Guid guid, string message)
    {
        try
        {
            using var stringContent = new StringContent(JsonConvert.SerializeObject(new TranslationRequest(message)), Encoding.UTF8, "application/json");
            using var result = TranslationClient.PostAsync("http://127.0.0.1:17785/translate", stringContent).Result;
            var content = result.Content.ReadAsStringAsync().Result;
            InternalLog.Information($"Content: {content}");
            var response = JsonConvert.DeserializeObject<TranslationResponse>(content);
            if(response.DetectedLanguage.Language != C.LibreTarget)
            {
                S.LocalLibretranslateTranslator.DeliverTranslatedMessage(guid, response.TranslatedText);
                return;
            }
        }
        catch(Exception e)
        {
            e.Log();
        }
    }
}