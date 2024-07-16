using Dalamud.Configuration;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.Text;
using Dalamud.Interface.FontIdentifier;
using Dalamud.Interface.GameFonts;
using ECommons.Configuration;
using Messenger.FontControl;

namespace Messenger;

public class Config : IEzConfig
{
    public int ContextMenuPriority = 0;
    public bool ContextMenuEnable = true;

    public HashSet<XivChatType> Channels = [];

    public ChannelCustomization DefaultChannelCustomization = new();
    public Dictionary<XivChatType, ChannelCustomization> SpecificChannelCustomizations = [];

    public bool IRCStyle = true;
    public bool PrintDate = true;
    public string MessageTimestampFormat = "HH:mm:ss";
    public string DateFormat = "D";
    public bool AutoHideCombat = true;
    public bool AutoHideDuty = false;
    public bool AutoReopenAfterCombat = true;
    public bool DisallowTransparencyHovered = false;

    public bool QuickOpenButton = false;
    public int QuickOpenPositionX2 = 0;
    public int QuickOpenPositionY2 = 0;
    public int HistoryAmount = 25;
    public int DisplayedMessages = 50;

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

    public bool UIShowHidden = false;
    public bool UIShowGPose = false;
    public bool UIShowCutscene = false;

    public bool PMLEnable = true;
    public int PMLMaxLines = 5;
    public bool PMLScrollDown = true;

    public int MessageLineSpacing = 0;
    public int MessageSpacing = 2;

    public bool EnableEmoji = true;
    public bool EnableEmojiPicker = true;
    public bool EnableBetterTTV = true;
    public bool DownloadUnknownEmoji = true;
    public long LastStaticBetterTTVUpdate = 0;
    public Dictionary<string, string> StaticBetterTTVEmojiCache = [];
    public Dictionary<string, string> DynamicBetterTTVEmojiCache = [];
    public List<string> FavoriteEmoji = [];
}
