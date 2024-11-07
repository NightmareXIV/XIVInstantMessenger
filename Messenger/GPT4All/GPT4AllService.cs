using Messenger.GPT4All.Request;
using Messenger.GPT4All.Response;
using Messenger.Gui;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Messenger.GPT4All;
public class GPT4AllService : IDisposable
{
    private bool Disposed = false;
    public WindowGptAssist Window = new();
    HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(20),
    };

    private ConcurrentDictionary<string, string> Responses = [];

    private GPT4AllService()
    {
        P.WindowSystemMain.AddWindow(Window);
        new Thread(Run).Start();
    }

    public void ResendRequest(string request)
    {
        Responses[request] = null;
    }

    public string GetResponse(string request)
    {
        if(Responses.TryGetValue(request, out var response))
        {
            return response;
        }
        else
        {
            ResendRequest(request);
        }
        return null;
    }

    private void Run()
    {
        while(!Disposed)
        {
            try
            {
                while(Responses.TryGetFirst(x => x.Value == null, out var result))
                {
                    Responses[result.Key] = MakeRequest(result.Key);
                }
            }
            catch(Exception e)
            {
                e.Log();
            }
            Thread.Sleep(100);
        }
    }

    public void Dispose()
    {
        Disposed = true;
        HttpClient.Dispose();
    }

    private string MakeRequest(string message)
    {
        try
        {
            var request = new GptRequest()
            {
                model = "Mistral Instruct",
                messages = [new() {
                role = "user",
                content = message,
            }]
            };
            var ser = JsonConvert.SerializeObject(request);
            PluginLog.Information($"Requesting: {ser}");
            var result = HttpClient.PostAsync("http://localhost:4891/v1/chat/completions", new StringContent(ser)).Result;
            var data = result.Content.ReadAsStringAsync().Result;
            PluginLog.Information($"For prompt:\n{message} \nResult: \n {data}");
            result.EnsureSuccessStatusCode();
            var decode = JsonConvert.DeserializeObject<GptResponse>(data);
            PluginLog.Information($"Result:\n\n{decode.choices[0].message.content}");
            return decode.choices[0].message.content ?? "< returned null >";
        }
        catch(Exception ex)
        {
            ex.Log();
            return ex.Message;
        }
    }
}
