using Dalamud.Configuration;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.Text;
using Dalamud.Interface.FontIdentifier;
using Dalamud.Interface.GameFonts;
using GTranslatorAPI;
using Messenger.FontControl;

namespace Messenger;

public class Config : IPluginConfiguration
{
    public int Version { get; set; } = 1;
    public int ContextMenuIndex = 1;
    public bool ContextMenuEnable = true;

    [Obsolete("", true)] public bool AutoOpenTellIncoming = true;
    [Obsolete("", true)] public bool AutoOpenTellOutgoing = true;
    [Obsolete("", true)] public bool AutoFocusTellOutgoing = true;
    [Obsolete("", true)] public Vector4 ColorToTitle = new(0.77f, 0.7f, 0.965f, 1f);
    [Obsolete("", true)] public Vector4 ColorToMessage = new(0.86f, 0.52f, 0.98f, 1f);
    [Obsolete("", true)] public Vector4 ColorFromTitle = new(0.47f, 0.30f, 0.96f, 1f);
    [Obsolete("", true)] public Vector4 ColorFromMessage = new(0.77f, 0.69f, 1f, 1f);
    [Obsolete("", true)] public Vector4 ColorGeneric = new(1f, 1f, 1f, 1f);
    [Obsolete("", true)] public Vector4 ColorTitleFlash = new(0.91f, 1f, 0f, 1f);
    [Obsolete("", true)] public bool SuppressDMs = false;
    public string TranslationProvider = "Do not use translation";
    public bool TranslatorLowercase = false;
    public bool TranslateSelf = false;
    public Languages GTranslateSourceLang = Languages.auto;
    public Languages GTranslateTargetLang = Languages.en;

    public HashSet<XivChatType> Channels = [];

    public ChannelCustomization DefaultChannelCustomization = null;
    public Dictionary<XivChatType, ChannelCustomization> SpecificChannelCustomizations = [];

    public bool IRCStyle = true;
    public bool PrintDate = true;
    public string MessageTimestampFormat = "HH:mm:ss";
    public string DateFormat = "D";
    public bool AutoHideCombat = true;
    public bool AutoReopenAfterCombat = true;

    public bool QuickOpenButton = false;
    public int QuickOpenPositionX2 = 0;
    public int QuickOpenPositionY2 = 0;
    public int HistoryAmount = 50;

    public bool EnableKey = true;
    public ModifierKey ModifierKey = ModifierKey.Alt;
    public VirtualKey Key = VirtualKey.R;
    public bool CommandPassthrough = true;

    public bool WindowShift = true;
    public int WindowShiftX = 50;
    public int WindowShiftY = 50;
    public float TransMin = 0.5f;
    public float TransMax = 1f;
    public float TransDelta = 0.02f;
    public bool WindowCascading = false;
    public int WindowCascadingX = 100;
    public int WindowCascadingY = 100;
    public int WindowCascadingXDelta = 50;
    public int WindowCascadingYDelta = 50;
    public int WindowCascadingReset = 10;
    public int WindowCascadingMaxColumns = 3;
    public bool ClickToOpenLink = true;
    public bool NoBringWindowToFrontIfTyping = true;
    public bool AutoTarget = true;
    public Sounds IncomingTellSound = Sounds.Sound01;
    public bool UseCustomFont = false;
    public bool IncreaseSpacing = false;
    public string AddonName = "_NaviMap";
    public bool CycleChatHotkey = false;
    public bool QuickOpenButtonOnTop = true;
    public bool Tabs = false;
    public bool ColorTitleFlashTab = true;

    public bool ButtonSend = true;
    public bool ButtonInvite = true;
    public bool ButtonFriend = true;
    public bool ButtonBlack = true;
    public bool ButtonLog = true;
    public bool ButtonCharaCard = true;

    public bool LockWindowSize = true;
    public Vector2 DefaultSize = new(300, 200);
    public bool ResetSizeOnAppearing = false;
    public bool NoResize = false;
    public bool NoMove = false;
    public bool CloseLogout = false;
    public bool RefocusInputAfterSending = true;
    public bool NoWarningWhenOpenLinks = false;
    public bool NoFlashing = false;
    public string LogStorageFolder = "";

    public HashSet<string> TabWindows = [];
    public Dictionary<string, string> TabWindowAssociations = [];

    public bool FontNoTabs = false;
    public bool TabsNoWorld = false;
    public bool SplitLogging = false;
    public bool SplitAutoUnload = false;
    public HashSet<string> SplitBlacklist = [];
}
