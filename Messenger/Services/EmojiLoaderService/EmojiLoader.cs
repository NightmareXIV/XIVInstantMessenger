using Lumina.Excel.GeneratedSheets2;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Messenger.Services.EmojiLoaderService;
public sealed class EmojiLoader : IDisposable
{
    public readonly Dictionary<string, ImageFile> Emoji = [];
    private HttpClient Client = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };
    private HashSet<string> PastEmojiSearchRequests = [];
    private ConcurrentQueue<string> EmojiSearchRequests = [];
    public volatile bool DownloaderTaskRunning = false;
    private readonly string CachePath = Path.Combine(Svc.PluginInterface.ConfigDirectory.FullName, "BetterTTVCache");

    public ImageFile Loading = new(Path.Combine(Svc.PluginInterface.AssemblyLocation.DirectoryName, "images", "loading.gif"));
    public ImageFile Error = new(Path.Combine(Svc.PluginInterface.AssemblyLocation.DirectoryName, "images", "error.png"));

    public void Search(string lowerId)
    {
        PastEmojiSearchRequests.Add(lowerId);
        EmojiSearchRequests.Enqueue(lowerId);
        if (!DownloaderTaskRunning) BeginDownloaderTask();
    }

    public ImageFile GetEmoji(string id)
    {
        {
            if (Emoji.TryGetValue(id, out ImageFile image))
            {
                return image;
            }
        }
        {
            if (Emoji.TryGetFirst(x => x.Key.EqualsIgnoreCase(id), out KeyValuePair<string, ImageFile> image))
            {
                return image.Value;
            }
        }
        string lowerId = id.ToLower();
        if (!PastEmojiSearchRequests.Contains(lowerId))
        {
            Search(lowerId);
        }
        return null;
    }

    private void BeginDownloaderTask()
    {
        DownloaderTaskRunning = true;
        new Thread(() =>
        {
            PluginLog.Debug($"Begin emoji downloader thread");
            try
            {
                int idle = 0;
                while (idle < 100)
                {
                    if (EmojiSearchRequests.TryDequeue(out string request))
                    {
                        try
                        {
                            PluginLog.Debug($"  Request dequeued: {request}");
                            idle = 0;
                            Client ??= new();
                            string result = Client.GetStringAsync("https://api.betterttv.net/3/emotes/shared/search?query=" + request).Result;
                            PluginLog.Debug($"  Result: {result}");
                            List<BetterTTWEmoji.EmoteData> emoji = JsonConvert.DeserializeObject<List<BetterTTWEmoji.EmoteData>>(result);
                            foreach (BetterTTWEmoji.EmoteData e in emoji)
                            {
                                PluginLog.Debug($"    Emoji: {e.code}");
                                DownloadEmojiToCache(e.id, e.imageType, e.code, true, C.DynamicBetterTTVEmojiCache);
                            }
                        }
                        catch (Exception e)
                        {
                            e.Log();
                        }
                    }
                    else
                    {
                        idle++;
                        Thread.Sleep(100);
                    }
                }
            }
            catch (Exception e)
            {
                e.Log();
            }
            DownloaderTaskRunning = false;
        }).Start();
    }

    private EmojiLoader()
    {
        LoadDefaultEmoji();
        if (DateTimeOffset.Now.ToUnixTimeMilliseconds() > C.LastStaticBetterTTVUpdate + TimeSpan.FromDays(7).TotalMilliseconds)
        {
            BuildCache();
        }
        else
        {
            LoadEmojiCache();
        }
    }

    public void BuildCache()
    {
        C.StaticBetterTTVEmojiCache.Clear();
        Task.Run(() =>
        {
            try
            {
                Client ??= new();
                HttpResponseMessage result = Client.GetAsync("https://api.betterttv.net/3/emotes/shared/top?limit=100").Result;
                result.EnsureSuccessStatusCode();
                List<BetterTTWEmoji> data = JsonConvert.DeserializeObject<List<BetterTTWEmoji>>(result.Content.ReadAsStringAsync().Result);
                PluginLog.Debug($"Emote info received: \n{data.Print("\n")}");
                foreach (BetterTTWEmoji e in data)
                {
                    DownloadEmojiToCache(e.emote.id, e.emote.imageType, e.emote.code, false, C.StaticBetterTTVEmojiCache);
                }
                Svc.Framework.RunOnFrameworkThread(() =>
                {
                    LoadEmojiCache();
                    C.LastStaticBetterTTVUpdate = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                });
            }
            catch (Exception e)
            {
                e.Log();
            }
        });
    }

    private void DownloadEmojiToCache(string id, string imageType, string code, bool skipExisting, Dictionary<string, string> cache)
    {
        string url = $"https://cdn.betterttv.net/emote/{id}/3x.{imageType}";
        PluginLog.Debug($" Downloading {url}");
        byte[] file = Client.GetByteArrayAsync(url).Result;
        string fname = $"{id}.{imageType}";
        File.WriteAllBytes(Path.Combine(CachePath, fname), file);
        Svc.Framework.RunOnFrameworkThread(() =>
        {
            string key = code;
            if (skipExisting && cache.ContainsKey(key)) return;
            int i = 1;
            while (cache.ContainsKey(key))
            {
                key = $"{code}{i}";
                i++;
            }
            cache[key] = fname;
            Emoji[key] = new(Path.Combine(CachePath, fname));
        });
    }

    public void Dispose()
    {
        Client?.Dispose();
        foreach (KeyValuePair<string, ImageFile> x in Emoji)
        {
            x.Value.Dispose();
        }
    }

    private void LoadDefaultEmoji()
    {
        string defaultEmojiFolder = Path.Combine(Svc.PluginInterface.AssemblyLocation.Directory.FullName, "images", "emoji");
        foreach (string f in Directory.GetFiles(defaultEmojiFolder))
        {
            Emoji[Path.GetFileNameWithoutExtension(f)] = new(f);
        }
    }

    private void LoadEmojiCache()
    {
        Emoji.Clear();
        LoadDefaultEmoji();
        foreach (KeyValuePair<string, string> x in C.StaticBetterTTVEmojiCache)
        {
            Emoji[x.Key] = new(Path.Combine(Svc.PluginInterface.ConfigDirectory.FullName, "BetterTTVCache", x.Value));
        }
        foreach (KeyValuePair<string, string> x in C.DynamicBetterTTVEmojiCache)
        {
            Emoji[x.Key] = new(Path.Combine(Svc.PluginInterface.ConfigDirectory.FullName, "BetterTTVCache", x.Value));
        }
    }
}
