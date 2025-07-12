using ECommons.EzIpcManager;
using ECommons.Networking;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Messenger.Services.Translation;
/// <summary>
/// This class serves as an example on how to register as a translation service for XIVInstantMessenger. Built-in translation is intentionally done via IPC for it to be an example. 
/// </summary>
public unsafe sealed class LocalLibretranslateTranslator : IDisposable
{
    private HttpClient TranslationClient
    {
        get
        {
            field ??= new HttpClient()
            {
                Timeout = TimeSpan.FromSeconds(10)
            }.ApplyProxySettings(C.ProxySettings);
            return field;
        }
    }

    private LocalLibretranslateTranslator()
    {
        EzIPC.Init(this, "Messenger");
    }

    public void Dispose()
    {
        TranslationClient?.Dispose();
    }

    private static string Name = "Built-In - Self-Hosted LibreTranslate";

    /// <summary>
    /// Step 1. Subscribe to this event. <i>You don't have to use EzIPC, you can use standard Dalamud-provided IPC methods with same name and signature. EzIPC available at Nuget as a library, if you prefer to use attributes instead. </i><br></br>
    /// Simply add your plugin's name into <paramref name="values"/>. 
    /// </summary>
    /// <param name="values"></param>
    [EzIPCEvent("OnAvailableTranslatorsRequest")]
    private void OnAvailableTranslatorsRequest(HashSet<string> values)
    {
        values.Add(Name);
    }

    /// <summary>
    /// Step 2. Translate any messages that are passed to this event if <paramref name="pluginName"/> equals to the same plugin name you added in <see cref="OnAvailableTranslatorsRequest"/> method.
    /// </summary>
    /// <param name="pluginName"></param>
    /// <param name="guid"></param>
    /// <param name="message"></param>
    [EzIPCEvent("OnMessageTranslationRequest")]
    private void OnMessageTranslationRequest(string pluginName, Guid guid, string message)
    {
        if(pluginName != Name) return;
        S.ThreadPool.Run(() =>
        {
            try
            {
                var stringContent = new StringContent(JsonConvert.SerializeObject(new TranslationRequest(message)), Encoding.UTF8, "application/json");
                var result = TranslationClient.PostAsync("http://127.0.0.1:5000/translate", stringContent).Result;
                var content = result.Content.ReadAsStringAsync().Result;
                InternalLog.Information($"Content: {content}");
                var response = JsonConvert.DeserializeObject<TranslationResponse>(content);
                if(response.DetectedLanguage.Language != C.LibreTarget)
                {
                    DeliverTranslatedMessage(guid, response.TranslatedText);
                    return;
                }
            }
            catch(Exception e)
            {
                e.Log();
            }
        });
    }

    /// <summary>
    /// Step 3. When the translation is ready, simply call this IPC method with the same GUID and translated text. It is thread-safe.
    /// </summary>
    [EzIPC("DeliverTranslatedMessage")]
    public Action<Guid, string> DeliverTranslatedMessage;

    /// <summary>
    /// (optional step) This method will be executed in XIM's ImGui Window in "Translation" tab when your translation engine is selected. You can draw some settings or information here. Call ImGui functions inside this method if <paramref name="pluginName"/> equals to the same plugin name you added in <see cref="OnAvailableTranslatorsRequest"/> method.
    /// </summary>
    /// <param name="pluginName"></param>
    [EzIPCEvent("OnTranslatorSettingsDraw")]
    private void OnTranslatorSettingsDraw(string pluginName)
    {
        if(pluginName != Name) return;
        ImGui.SetNextItemWidth(150f.Scale());
        if(ImGui.BeginCombo("Select language to translate to", LibreTranslationUtils.Languages.FindKeysByValue(C.LibreTarget).FirstOrDefault() ?? "- Not Selected -"))
        {
            foreach(var x in LibreTranslationUtils.Languages)
            {
                if(ImGui.Selectable(x.Key, x.Value == C.LibreTarget))
                {
                    C.LibreTarget = x.Value;
                }
            }
            ImGui.EndCombo();
        }
    }
}