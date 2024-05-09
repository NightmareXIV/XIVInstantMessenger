using Dalamud.Interface.FontIdentifier;
using Dalamud.Interface.GameFonts;
using Dalamud.Interface.ManagedFontAtlas;
using ECommons.Configuration;
using ImGuiNET;

namespace Messenger.FontControl;

public unsafe class FontManager
{
    public FontConfiguration FontConfiguration;
    private IFontHandle Handle = null;
    public FontManager()
    {
        P.WhitespaceMap.Clear();
        try
        {
            FontConfiguration = EzConfig.LoadConfiguration<FontConfiguration>("FontConfiguration.json");
        }
        catch(Exception e)
        {
            FontConfiguration = new();
            Notify.Error($"Failed to load font configuration.\nFont settings have been reset.");
            e.Log();
        }
        if (C.UseCustomFont)
        {
            try
            {
                Handle = FontConfiguration.Font.CreateFontHandle(Svc.PluginInterface.UiBuilder.FontAtlas);
            }
            catch (Exception e)
            {
                e.Log();
            }
        }
    }

    public void Save()
    {
        EzConfig.SaveConfiguration(FontConfiguration, "FontConfiguration.json");
    }

    public void Dispose()
    {
        Handle?.Dispose();
        Save();
    }

    public bool FontPushed = false;
    public bool FontReady => Handle.Available;

    public void PushFont()
    {
        if(FontPushed)
        {
            DuoLog.Error($"A critical error occurred. Please send logs to developer.");
            throw new InvalidOperationException("Font is already pushed.");
        }
        if (C.UseCustomFont)
        {
            if(Handle != null && Handle.Available)
            {
                Handle.Push();
                FontPushed = true;
            }
        }
    }

    public void PopFont()
    {
        if (FontPushed)
        {
            Handle.Pop();
            FontPushed = false;
        }
    }
}