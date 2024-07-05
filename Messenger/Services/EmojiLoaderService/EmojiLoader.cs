using FFXIVClientStructs.Interop;
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
            if (Emoji.TryGetValue(id, out var image))
            {
                return image;
            }
        }
        {
            if (Emoji.TryGetFirst(x => x.Key.EqualsIgnoreCase(id), out var image))
            {
                return image.Value;
            }
        }
        var lowerId = id.ToLower();
        if (!PastEmojiSearchRequests.Contains(lowerId) && C.DownloadUnknownEmoji)
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
            PluginLog.Verbose($"Begin emoji downloader thread");
            try
            {
                var idle = 0;
                while (idle < 100)
                {
                    if (EmojiSearchRequests.TryDequeue(out var request))
                    {
                        try
                        {
                            PluginLog.Verbose($"  Request dequeued: {request}");
                            idle = 0;
                            Client ??= new();
                            var result = Client.GetStringAsync("https://api.betterttv.net/3/emotes/shared/search?query=" + request).Result;
                            PluginLog.Verbose($"  Result: {result}");
                            var emoji = JsonConvert.DeserializeObject<List<BetterTTWEmoji.EmoteData>>(result);
                            foreach (var e in emoji)
                            {
                                PluginLog.Verbose($"    Emoji: {e.code}");
                                DownloadEmojiToCache(e.id, e.imageType, e.code, true, C.DynamicBetterTTVEmojiCache, request);
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
        Initialize();
    }

    public void Initialize()
    {
        foreach (var fname in C.StaticBetterTTVEmojiCache.Keys.ToArray())
        {
            if (!File.Exists(Path.Combine(CachePath, C.StaticBetterTTVEmojiCache[fname])))
            {
                PluginLog.Warning($"Deleting corrupted emoji {fname} from static bttv cache");
                C.StaticBetterTTVEmojiCache.Remove(fname);
            }
        }
        foreach (var fname in C.DynamicBetterTTVEmojiCache.Keys.ToArray())
        {
            if (!File.Exists(Path.Combine(CachePath, C.DynamicBetterTTVEmojiCache[fname])))
            {
                PluginLog.Warning($"Deleting corrupted emoji {fname} from dynamic bttv cache");
                C.DynamicBetterTTVEmojiCache.Remove(fname);
            }
        }
        if (C.EnableEmoji)
        {
            LoadDefaultEmoji();
            if (C.EnableBetterTTV)
            {
                if (DateTimeOffset.Now.ToUnixTimeMilliseconds() > C.LastStaticBetterTTVUpdate + TimeSpan.FromDays(7).TotalMilliseconds)
                {
                    BuildCache();
                }
                else
                {
                    LoadEmojiCache();
                }
            }
        }
    }

    private volatile bool BuildingCache = false;
    public void BuildCache()
    {
        if (BuildingCache) return;
        BuildingCache = true;
        C.StaticBetterTTVEmojiCache.Clear();
        Task.Run(() =>
        {
            try
            {
                Client ??= new();
                var result = Client.GetAsync("https://api.betterttv.net/3/emotes/shared/top?limit=100").Result;
                result.EnsureSuccessStatusCode();
                var data = JsonConvert.DeserializeObject<List<BetterTTWEmoji>>(result.Content.ReadAsStringAsync().Result);
                PluginLog.Verbose($"Emote info received: \n{data.Print("\n")}");
                foreach (var e in data)
                {
                    DownloadEmojiToCache(e.emote.id, e.emote.imageType, e.emote.code, false, C.StaticBetterTTVEmojiCache, null);
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
            BuildingCache = false;
        });
    }

    private void DownloadEmojiToCache(string id, string imageType, string code, bool skipExisting, Dictionary<string, string> cache, string overwrite)
    {
        var url = $"https://cdn.betterttv.net/emote/{id}/3x.{imageType}";
        PluginLog.Verbose($" Downloading {url}");
        var file = Client.GetByteArrayAsync(url).Result;
        var fname = $"{id}.{imageType}";
        Directory.CreateDirectory(CachePath);
        File.WriteAllBytes(Path.Combine(CachePath, fname), file);
        Svc.Framework.RunOnFrameworkThread(() =>
        {
            var key = code;
            if (skipExisting && cache.ContainsKey(key))
            {
                if(key == overwrite)
                {
                    cache.Remove(key);
                }
                else
                {
                    return;
                }
            }
            var i = 1;
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
        foreach (var x in Emoji)
        {
            x.Value.Dispose();
        }
    }

    private void LoadDefaultEmoji()
    {
        var defaultEmojiFolder = Path.Combine(Svc.PluginInterface.AssemblyLocation.Directory.FullName, "images", "emoji");
        foreach (var f in Directory.GetFiles(defaultEmojiFolder))
        {
            Emoji[Path.GetFileNameWithoutExtension(f)] = new(f);
        }
    }

    private void LoadEmojiCache()
    {
        Emoji.Clear();
        LoadDefaultEmoji();
        foreach (var x in C.StaticBetterTTVEmojiCache)
        {
            Emoji[x.Key] = new(Path.Combine(Svc.PluginInterface.ConfigDirectory.FullName, "BetterTTVCache", x.Value));
        }
        foreach (var x in C.DynamicBetterTTVEmojiCache)
        {
            Emoji[x.Key] = new(Path.Combine(Svc.PluginInterface.ConfigDirectory.FullName, "BetterTTVCache", x.Value));
        }
    }
}
