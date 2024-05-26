using Lumina.Excel.GeneratedSheets2;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Messenger.Services.EmojiLoaderService;
public sealed class EmojiLoader : IDisposable
{
    public readonly Dictionary<string, ImageFile> Emoji = [];
    private HttpClient Client;

    private EmojiLoader()
    {
				LoadDefaultEmoji();
				if (DateTimeOffset.Now.ToUnixTimeMilliseconds() > C.LastBetterTTVUpdate + TimeSpan.FromDays(7).TotalMilliseconds)
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
				C.BetterTTVEmojiCache.Clear();
				Task.Run(() =>
				{
						try
						{
								Client ??= new();
								var result = Client.GetAsync("https://api.betterttv.net/3/emotes/shared/top?limit=100").Result;
								result.EnsureSuccessStatusCode();
								var data = JsonConvert.DeserializeObject<List<BetterTTWEmoji>>(result.Content.ReadAsStringAsync().Result);
								PluginLog.Debug($"Emote info received: \n{data.Print("\n")}");
								var tempPath = Path.Combine(Svc.PluginInterface.ConfigDirectory.FullName, "BetterTTVCache");
								Directory.CreateDirectory(tempPath);
								foreach (var e in data)
								{
										var url = $"https://cdn.betterttv.net/emote/{e.emote.id}/1x.{e.emote.imageType}";
										PluginLog.Debug($" Downloading {url}");
										var file = Client.GetByteArrayAsync(url).Result;
										var fname = $"{e.emote.id}.{e.emote.imageType}";
										File.WriteAllBytes(Path.Combine(tempPath, fname), file);
										Svc.Framework.RunOnFrameworkThread(() =>
										{
												var key = e.emote.code;
												var i = 1;
												while (C.BetterTTVEmojiCache.ContainsKey(key))
												{
														key = $"{e.emote.code}{i}";
														i++;
												}
												C.BetterTTVEmojiCache[key] = fname;
										});
								}
								Svc.Framework.RunOnFrameworkThread(() =>
								{
										LoadEmojiCache();
										C.LastBetterTTVUpdate = DateTimeOffset.Now.ToUnixTimeMilliseconds();
								});
						}
						catch (Exception e)
						{
								e.Log();
						}
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
				foreach (var x in C.BetterTTVEmojiCache)
        {
            Emoji[x.Key] = new(Path.Combine(Svc.PluginInterface.ConfigDirectory.FullName, "BetterTTVCache", x.Value));
				}
    }
}
