using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using ECommons;
using ECommons.Automation;
using ECommons.GameFunctions;
using Messenger.FontControl;
using Messenger.FriendListManager;
using Messenger.Gui.TitleButtons;
using SixLabors.ImageSharp.Metadata;
using System.IO;
using static FFXIVClientStructs.FFXIV.Client.Graphics.Kernel.VertexShader;

namespace Messenger.Gui;

public unsafe class ChatWindow : Window
{
    private static int CascadingPosition = 0;
    internal MessageHistory MessageHistory;
    private float fieldHeight = 0;
    private float afterInputWidth = 0;
    internal bool KeepInCombat = false;
    internal bool Unread = false;
    private bool TitleColored = false;
    private float Transparency = C.TransMax;
    private bool IsTransparent = true;
    internal bool SetPosition = false;
    internal new bool BringToFront = false;
    private PseudoMultilineInput Input = new();
    private bool BlockEmojiSelection = false;

    internal string OwningTab => C.TabWindowAssociations.TryGetValue(MessageHistory.Player.ToString(), out var owner) ? owner : null;

    internal ChannelCustomization Cust => MessageHistory.Player.GetCustomization();

    private InviteToPartyButton InviteToPartyButton;
    private AddFriendButton AddFriendButton;
    private AddToBlacklistButton AddToBlacklistButton;
    private OpenLogButton OpenLogButton;
    private OpenCharaCardButton OpenCharaCardButton;

    public ChatWindow(MessageHistory messageHistory) :
        base($"Chat with {messageHistory.Player.GetChannelName()}###Messenger - {messageHistory.Player.Name}{messageHistory.Player.HomeWorld}"
            , ImGuiWindowFlags.NoFocusOnAppearing)
    {
        MessageHistory = messageHistory;
        SizeConstraints = new()
        {
            MinimumSize = new(200, 200),
            MaximumSize = new(9999, 9999)
        };
        InviteToPartyButton = new(this);
        AddFriendButton = new(this);
        AddToBlacklistButton = new(this);
        OpenLogButton = new(this);
        OpenCharaCardButton = new(this);
    }

    public void UpdateTitleButtons(Window window)
    {
        window.TitleBarButtons.Clear();
        if (InviteToPartyButton.ShouldDisplay()) window.TitleBarButtons.Add(InviteToPartyButton.Button);
        if (AddFriendButton.ShouldDisplay()) window.TitleBarButtons.Add(AddFriendButton.Button);
        if (AddToBlacklistButton.ShouldDisplay()) window.TitleBarButtons.Add(AddToBlacklistButton.Button);
        if (OpenLogButton.ShouldDisplay()) window.TitleBarButtons.Add(OpenLogButton.Button);
        if (OpenCharaCardButton.ShouldDisplay()) window.TitleBarButtons.Add(OpenCharaCardButton.Button);
    }

    internal void SetTransparency(bool isTransparent)
    {
        Transparency = !isTransparent ? C.TransMax : C.TransMin;
    }

    internal bool HideByCombat => Svc.Condition[ConditionFlag.InCombat] && C.AutoHideCombat && !KeepInCombat;

    public override bool DrawConditions()
    {
        var ret = true;
        if (P.Hidden)
        {
            ret = false;
        }
        if (HideByCombat)
        {
            ret = false;
        }
        if (!ret)
        {
            MessageHistory.UnsetFocus();
        }
        return ret;
    }

    public override void OnOpen()
    {
        P.lastHistory = MessageHistory;
        if (C.WindowCascading)
        {
            SetPosition = true;
        }
        if (!C.NoBringWindowToFrontIfTyping || !ImGui.GetIO().WantCaptureKeyboard)
        {
            BringToFront = true;
        }
    }

    public override void OnClose()
    {
        KeepInCombat = false;
        if (P.Chats.All(x => !x.Value.ChatWindow.IsOpen))
        {
            //Notify.Info("Cascading has been reset");
            ChatWindow.CascadingPosition = 0;
        }
    }

    public override void Update()
    {
        TitleBarButtons.Clear();
    }

    public override void PreDraw()
    {
        if (C.NoResize && !ImGui.GetIO().KeyCtrl)
        {
            Flags |= ImGuiWindowFlags.NoResize;
        }
        else
        {
            Flags &= ~ImGuiWindowFlags.NoResize;
        }
        if (C.NoMove && !ImGui.GetIO().KeyCtrl)
        {
            Flags |= ImGuiWindowFlags.NoMove;
        }
        else
        {
            Flags &= ~ImGuiWindowFlags.NoMove;
        }
        Size = C.DefaultSize;
        SizeCondition = C.ResetSizeOnAppearing ? ImGuiCond.Appearing : ImGuiCond.FirstUseEver;
        IsTransparent = Transparency < 1f;
        TitleColored = false;
        if (Unread)
        {
            TitleColored = true;
            ImGui.PushStyleColor(ImGuiCol.TitleBg, ImGuiCol.TitleBg.GetFlashColor(Cust));
            ImGui.PushStyleColor(ImGuiCol.TitleBgCollapsed, ImGuiCol.TitleBgCollapsed.GetFlashColor(Cust));
        }
        if (IsTransparent) ImGui.PushStyleVar(ImGuiStyleVar.Alpha, Transparency);
        if (SetPosition)
        {
            SetPosition = false;
            if (C.WindowCascading)
            {
                var cPos = CascadingPosition % C.WindowCascadingReset;
                var xmult = (int)(CascadingPosition / C.WindowCascadingReset);
                ImGuiHelpers.SetNextWindowPosRelativeMainViewport(ImGuiHelpers.MainViewport.Pos
                    + new Vector2(C.WindowCascadingX, C.WindowCascadingY)
                    + new Vector2(C.WindowCascadingXDelta * cPos + C.WindowCascadingXDelta * xmult, C.WindowCascadingYDelta * cPos));
                CascadingPosition++;
                if (CascadingPosition > C.WindowCascadingReset * C.WindowCascadingMaxColumns)
                {
                    CascadingPosition = 0;
                }
            }
        }
        P.FontManager.PushFont();
    }

    public override void Draw()
    {
        InviteToPartyButton.DrawPopup();
        UpdateTitleButtons(this);
        if (P.FontManager.FontPushed && !P.FontManager.FontReady)
        {
            ImGuiEx.Text($"Loading font, please wait...");
            return;
        }
        var prev = P.GetPreviousMessageHistory(MessageHistory);
        /*ImGuiEx.Text($"{(prev == null ? "null" : prev.Player)}");
        ImGui.SameLine();
        ImGuiEx.TextCopy($"{this.messageHistory.GetLatestMessageTime()}");*/
        if (ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows))
        {
            Unread = false;
            Transparency = Math.Min(C.TransMax, Transparency + C.TransDelta);
        }
        else
        {
            if (ImGui.IsWindowHovered(ImGuiHoveredFlags.RootAndChildWindows | ImGuiHoveredFlags.AllowWhenBlockedByPopup))
            {
                Transparency = Math.Min(C.TransMax, Transparency + C.TransDelta);
            }
            else
            {
                Transparency = Math.Max(C.TransMin, Transparency - C.TransDelta);
            }
        }
        if (BringToFront)
        {
            BringToFront = false;
            CImGui.igBringWindowToDisplayFront(CImGui.igGetCurrentWindow());
        }
        var subject = MessageHistory.Player.IsGenericChannel() ? Enum.GetValues<XivChatType>().First(x => x.ToString() == MessageHistory.Player.Name).GetCommand() : MessageHistory.Player.GetPlayerName();
        var subjectNoWorld = MessageHistory.Player.GetPlayerName().Split("@")[0];
        var me = Svc.ClientState.LocalPlayer?.Name.ToString().Split("@")[0] ?? "Me";
        ImGui.BeginChild($"##ChatChild{subject}", new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetContentRegionAvail().Y - fieldHeight));
        if (MessageHistory.Messages.Count == 0)
        {
            Utils.DrawWrappedText($"This is the beginning of your chat with {subject}");
        }
        bool? isIncoming = null;
        if (MessageHistory.LogLoaded && MessageHistory.LoadedMessages.Count > 0)
        {
            foreach (var message in MessageHistory.LoadedMessages)
            {
                MessageHistory.Messages.Insert(0, message);
            }
            MessageHistory.Scroll();
            MessageHistory.LoadedMessages.Clear();
        }
        (int year, int day) currentDay = (0, 0);
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, C.MessageLineSpacing));
        foreach (var x in MessageHistory.Messages)
        {
            if (x.IsSystem)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, Cust.ColorGeneric);
                x.Draw();
                ImGui.PopStyleColor();
            }
            else
            {
                var time = DateTimeOffset.FromUnixTimeMilliseconds(x.Time).ToLocalTime();
                if (C.PrintDate)
                {
                    if (!(time.DayOfYear == currentDay.day && time.Year == currentDay.year))
                    {
                        ImGuiEx.Text(Cust.ColorGeneric, $"[{time.ToString(C.DateFormat)}]");
                        if (C.MessageSpacing > 0) ImGui.Dummy(new(C.MessageSpacing));
                        currentDay = (time.Year, time.DayOfYear);
                    }
                }
                var timestamp = time.ToString(C.MessageTimestampFormat);
                if (C.IRCStyle)
                {
                    var messageColor = x.IsIncoming ? Cust.ColorFromMessage : Cust.ColorToMessage;
                    var subjectColor = x.IsIncoming ? Cust.ColorFromTitle : Cust.ColorToTitle;
                    ImGuiEx.Text(Cust.ColorGeneric, $"{timestamp} ");
                    ImGui.SameLine(0, 0);
                    ImGuiEx.Text(messageColor, $"[");
                    ImGui.SameLine(0, 0);
                    ImGuiEx.Text(subjectColor, $"{x.OverrideName?.Split("@")[0] ?? (x.IsIncoming ? subjectNoWorld : me)}");
                    ImGui.SameLine(0, 0);
                    ImGuiEx.Text(messageColor, $"] ");
                    ImGui.SameLine(0, 0);
                    ImGui.PushStyleColor(ImGuiCol.Text, messageColor);
                    x.Draw();
                    PostMessageFunctions(x);
                    ImGui.PopStyleColor();
                }
                else
                {
                    if (x.IsIncoming != isIncoming)
                    {
                        isIncoming = x.IsIncoming;
                        if (x.IsIncoming)
                        {
                            ImGui.PushStyleColor(ImGuiCol.Text, Cust.ColorFromTitle);
                            Utils.DrawWrappedText($"From {x.OverrideName?.Split("@")[0] ?? subjectNoWorld}");
                            ImGui.PopStyleColor();
                        }
                        else
                        {
                            ImGui.PushStyleColor(ImGuiCol.Text, Cust.ColorToTitle);
                            Utils.DrawWrappedText($"From {x.OverrideName?.Split("@")[0] ?? me}");
                            ImGui.PopStyleColor();
                        }
                    }
                    ImGuiHelpers.ScaledDummy(new Vector2(20f, 1f));
                    ImGui.SameLine(0, 0);
                    ImGui.PushStyleColor(ImGuiCol.Text, x.IsIncoming ? Cust.ColorFromMessage : Cust.ColorToMessage);
                    x.Draw("[{timestamp}] ");
                    ImGui.PopStyleColor();
                    PostMessageFunctions(x);
                }
            }
            if (C.MessageSpacing > 0) ImGui.Dummy(new(C.MessageSpacing));
        }
        ImGui.PopStyleVar();
        if (MessageHistory.DoScroll > 0)
        {
            MessageHistory.DoScroll--;
            ImGui.SetScrollHereY();
        }
        if (C.PMLScrollDown && C.PMLEnable && Input.IsInputActive)
        {
            ImGui.SetScrollHereY();
        }
        ImGui.EndChild();
        var isCmd = C.CommandPassthrough && Input.SinglelineText.Trim().StartsWith("/");
        var cursor = ImGui.GetCursorPosY();
        //ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - afterInputWidth);
        Input.Width = ImGui.GetContentRegionAvail().X - afterInputWidth;
        var inputCol = false;
        if (isCmd)
        {
            inputCol = true;
            ImGui.PushStyleColor(ImGuiCol.FrameBg, ImGui.GetStyle().Colors[(int)ImGuiCol.TitleBgActive] with { W = ImGui.GetStyle().Colors[(int)ImGuiCol.FrameBg].W });
        }
        var cur = ImGui.GetCursorPosY();
        Input.Draw();
        if (Input.EnterWasPressed())
        {
            SendMessage(subject, MessageHistory.Player.IsGenericChannel());
            if (Input.IsMultiline)
            {
                ImGui.SetWindowFocus(null);
                ImGui.SetWindowFocus(WindowName);
            }
        }
        if (inputCol)
        {
            ImGui.PopStyleColor();
        }
        if (MessageHistory.ShouldSetFocus())
        {
            SetTransparency(false);
            ImGui.SetWindowFocus();
            ImGui.SetKeyboardFocusHere(-1);
            MessageHistory.UnsetFocus();
        }
        ImGui.SetWindowFontScale(ImGui.CalcTextSize(" ").Y / ImGuiEx.CalcIconSize(FontAwesomeIcon.ArrowRight).Y);
        ImGui.SameLine(0, 0);
        var icur1 = ImGui.GetCursorPos();
        ImGui.Dummy(Vector2.Zero);

        if (C.EnableEmoji && C.EnableEmojiPicker)
        {
            ImGui.SameLine(0, 2);
            if (ImGuiEx.IconButton(FontAwesomeIcon.SmileWink, "Insert emoji"))
            {
                if (!BlockEmojiSelection) Input.OpenEmojiSelector();
            }
            if (ImGui.IsItemHovered() && ImGui.IsMouseDown(ImGuiMouseButton.Left) && Input.IsSelectingEmoji)
            {
                BlockEmojiSelection = true;
            }
            if (BlockEmojiSelection)
            {
                if (ImGui.IsItemHovered() && ImGui.IsMouseDown(ImGuiMouseButton.Left))
                {
                    //
                }
                else
                {
                    BlockEmojiSelection = false;
                }
            }
            ImGuiEx.Tooltip("Open Emoji selector");
        }

        if (C.ButtonSend)
        {
            ImGui.SameLine(0, 2);
            if (isCmd)
            {
                if (ImGuiEx.IconButton(FontAwesomeIcon.FastForward, "Execute command"))
                {
                    SendMessage(subject, MessageHistory.Player.IsGenericChannel());
                }
                ImGuiEx.Tooltip("Execute command");
            }
            else
            {
                if (ImGuiEx.IconButton(FontAwesomeIcon.ArrowRight, "Send"))
                {
                    SendMessage(subject, MessageHistory.Player.IsGenericChannel());
                }
                ImGuiEx.Tooltip("Send message");
            }
        }
        ImGui.SameLine(0, 0);
        afterInputWidth = ImGui.GetCursorPosX() - icur1.X;
        ImGui.Dummy(Vector2.Zero);
        var bytes = Utils.GetLength(subject, Input.SinglelineText);
        var fraction = (float)bytes.current / (float)bytes.max;
        ImGui.PushStyleColor(ImGuiCol.PlotHistogram, fraction > 1f ? ImGuiColors.DalamudRed : Cust.ColorGeneric);
        ImGui.ProgressBar(fraction, new Vector2(ImGui.GetContentRegionAvail().X, 3f), "");
        ImGui.PopStyleColor();
        fieldHeight = ImGui.GetCursorPosY() - cursor;
        ImGui.SetWindowFontScale(1);
    }

    private void PostMessageFunctions(SavedMessage x)
    {
        if (C.ClickToOpenLink && ImGui.IsItemHovered())
        {
            foreach (var s in x.Message.Split(" "))
            {
                if (s.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                    || s.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, Cust.ColorGeneric);
                    ImGuiEx.SetTooltip($"Link found:\n{s}\nClick to open");
                    ImGui.PopStyleColor();
                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                    if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
                    {
                        new OpenLinkWindow(s);
                    }
                    break;
                }
            }
        }
        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
        {
            ImGui.OpenPopup($"MessageDetail{x.GUID}");
        }
        if (ImGui.BeginPopup($"MessageDetail{x.GUID}"))
        {
            ImGui.PushStyleColor(ImGuiCol.Text, Cust.ColorGeneric);
            //this.SetTransparency(false);
            ImGui.SetNextItemWidth(400f);
            var msg = x.Message;
            ImGui.InputText("##copyTextMsg", ref msg, 10000);
            if (ImGui.Selectable($"Copy message to clipboard"))
            {
                ImGui.SetClipboardText(x.Message);
            }
            var linkN = false;
            foreach (var s in x.Message.Split(" "))
            {
                if (s.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                    || s.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    if (!linkN)
                    {
                        linkN = true;
                        ImGui.Separator();
                        ImGuiEx.Text("Found links in message:");
                    }
                    if (ImGui.Selectable($"{s}"))
                    {
                        new OpenLinkWindow(s);
                    }
                }
            }
            if (x.MapPayload != null)
            {
                if (ImGui.Selectable("Open map link"))
                {
                    Safe(delegate
                    {
                        Svc.GameGui.OpenMapWithMapLink(x.MapPayload);
                    });
                }
            }
            if (x.Item != null)
            {
                if (ImGui.Selectable("Print item details"))
                {
                    Safe(delegate
                    {
                        Svc.Chat.Print(new SeStringBuilder().Add(Utils.GetItemPayload(x.Item.Item, x.Item.IsHQ)).BuiltString);
                    });
                }
            }
            ImGui.EndPopup();
            ImGui.PopStyleColor();
        }
    }


    public override void PostDraw()
    {
        P.FontManager.PopFont();
        if (IsTransparent) ImGui.PopStyleVar();
        if (TitleColored)
        {
            ImGui.PopStyleColor(2);
        }
    }

    private void SendMessage(string subject, bool generic)
    {
        var bytes = Utils.GetLength(subject, Input.SinglelineText);
        var trimmed = Input.SinglelineText.Trim();
        if (trimmed.Length == 0)
        {
            //Notify.Error("Message is empty!");
        }
        else if (bytes.current > bytes.max)
        {
            Notify.Error("Message is too long!");
        }
        else if (trimmed.StartsWith("/") && C.CommandPassthrough)
        {
            if (!generic && C.AutoTarget &&
            (P.TargetCommands.Any(x => Input.SinglelineText.Equals(x, StringComparison.OrdinalIgnoreCase) || Input.SinglelineText.StartsWith($"{x} ", StringComparison.OrdinalIgnoreCase)))
            && Svc.Objects.TryGetFirst(x => x is PlayerCharacter pc && pc.GetPlayerName() == subject && x.IsTargetable, out var obj))
            {
                Svc.Targets.SetTarget(obj);
                //Notify.Info($"Targeting {subject}");
                new TickScheduler(delegate { Chat.Instance.SendMessage(trimmed); }, 100);
            }
            else
            {
                Chat.Instance.SendMessage(trimmed);
            }
            Input.SinglelineText = "";
        }
        else
        {
            PluginLog.Verbose($"Begin send message to {subject} {generic}: {trimmed}");
            var error = P.SendDirectMessage(subject, trimmed, generic);
            if (error == null)
            {
                Input.SinglelineText = "";
            }
            else
            {
                Notify.Error(Input.SinglelineText);
            }
        }
        if (C.RefocusInputAfterSending)
        {
            MessageHistory.SetFocusAtNextFrame();
        }
    }
}
