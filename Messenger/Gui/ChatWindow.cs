using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using ECommons.Automation;
using ECommons.GameFunctions;
using Messenger.FontControl;
using Messenger.FriendListManager;
using System.IO;

namespace Messenger.Gui;

internal unsafe class ChatWindow : Window
{
    static int CascadingPosition = 0;
    internal MessageHistory MessageHistory;
    float fieldHeight = 0;
    float afterInputWidth = 0;
    string msg = "";
    internal bool KeepInCombat = false;
    internal bool Unread = false;
    bool TitleColored = false;
    float Transparency = C.TransMax;
    bool IsTransparent = true;
    internal bool SetPosition = false;
    internal new bool BringToFront = false;

    internal string OwningTab => C.TabWindowAssociations.TryGetValue(MessageHistory.Player.ToString(), out var owner)?owner:null;

    internal ChannelCustomization Cust => this.MessageHistory.Player.GetCustomization();

    public ChatWindow(MessageHistory messageHistory) : 
        base($"Chat with {messageHistory.Player.GetChannelName()}###Messenger - {messageHistory.Player.Name}{messageHistory.Player.HomeWorld}"
            , ImGuiWindowFlags.NoFocusOnAppearing)
    {
        this.MessageHistory = messageHistory;
        this.SizeConstraints = new()
        {
            MinimumSize = new(200, 200),
            MaximumSize = new(9999, 9999)
        };
    }

    internal void SetTransparency(bool isTransparent)
    {
        this.Transparency = !isTransparent ? C.TransMax : C.TransMin;
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
            this.MessageHistory.SetFocus = false;
        }
        return ret;
    }

    public override void OnOpen()
    {
        P.lastHistory = this.MessageHistory;
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
        if(P.Chats.All(x => !x.Value.ChatWindow.IsOpen))
        {
            //Notify.Info("Cascading has been reset");
            ChatWindow.CascadingPosition = 0;
        }
    }

    public override void PreDraw()
    {
        if (C.NoResize && !ImGui.GetIO().KeyCtrl)
        {
            this.Flags |= ImGuiWindowFlags.NoResize;
        }
        else
        {
            this.Flags &= ~ImGuiWindowFlags.NoResize;
        }
        if (C.NoMove && !ImGui.GetIO().KeyCtrl)
        {
            this.Flags |= ImGuiWindowFlags.NoMove;
        }
        else
        {
            this.Flags &= ~ImGuiWindowFlags.NoMove;
        }
        this.Size = C.DefaultSize;
        this.SizeCondition = C.ResetSizeOnAppearing ? ImGuiCond.Appearing : ImGuiCond.FirstUseEver;
        IsTransparent = Transparency < 1f;
        TitleColored = false;
        if (Unread)
        {
            TitleColored = true;
            ImGui.PushStyleColor(ImGuiCol.TitleBg, ImGuiCol.TitleBg.GetFlashColor(Cust));
            ImGui.PushStyleColor(ImGuiCol.TitleBgCollapsed, ImGuiCol.TitleBgCollapsed.GetFlashColor(Cust));
        }
        if(IsTransparent) ImGui.PushStyleVar(ImGuiStyleVar.Alpha, Transparency);
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
        var prev = P.GetPreviousMessageHistory(this.MessageHistory);
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
        var subject = MessageHistory.Player.IsGenericChannel()? Enum.GetValues<XivChatType>().First(x => x.ToString() == MessageHistory.Player.Name).GetCommand() : MessageHistory.Player.GetPlayerName();
        var subjectNoWorld = MessageHistory.Player.GetPlayerName().Split("@")[0];
        var me = Svc.ClientState.LocalPlayer?.Name.ToString().Split("@")[0] ?? "Me";
        ImGui.BeginChild($"##ChatChild{subject}", new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetContentRegionAvail().Y - fieldHeight));
        if(MessageHistory.Messages.Count == 0)
        {
            ImGuiEx.TextWrapped($"This is the beginning of your chat with {subject}");
        }
        bool? isIncoming = null;
        if (MessageHistory.LogLoaded && MessageHistory.LoadedMessages.Count > 0)
        {
            foreach(var message in MessageHistory.LoadedMessages)
            {
                MessageHistory.Messages.Insert(0, message);
            }
            MessageHistory.Scroll();
            MessageHistory.LoadedMessages.Clear();
        }
        (int year, int day) currentDay = (0, 0);
        foreach (var x in MessageHistory.Messages)
        {
            if (x.IsSystem)
            {
                ImGuiEx.TextWrapped(Cust.ColorGeneric, $"{x.Message}");
            }
            else
            {
                var time = DateTimeOffset.FromUnixTimeMilliseconds(x.Time).ToLocalTime();
                if (C.PrintDate)
                {
                    if (!(time.DayOfYear == currentDay.day && time.Year == currentDay.year))
                    {
                        ImGuiEx.Text(Cust.ColorGeneric, $"[{time.ToString(C.DateFormat)}]");
                        currentDay = (time.Year, time.DayOfYear);
                    }
                }
                var timestamp = time.ToString(C.MessageTimestampFormat);
                if (C.IRCStyle)
                {
                    var messageColor = x.IsIncoming ? Cust.ColorFromMessage : Cust.ColorToMessage;
                    var subjectColor = x.IsIncoming ? Cust.ColorFromTitle : Cust.ColorToTitle;
                    var cur1 = ImGui.GetCursorPos();
                    var wdt = ImGuiEx.Measure(delegate
                    {
                        ImGuiEx.Text(Cust.ColorGeneric, $"{timestamp} ");
                        ImGui.SameLine(0, 0);
                        ImGuiEx.Text(messageColor, $"[");
                        ImGui.SameLine(0, 0);
                        ImGuiEx.Text(subjectColor, $"{x.OverrideName?.Split("@")[0] ?? (x.IsIncoming ? subjectNoWorld : me)}");
                        ImGui.SameLine(0, 0);
                        ImGuiEx.Text(messageColor, $"] ");
                    }, false);
                    
                    var spaces = P.GetWhitespacesForLen(wdt);
                    ImGui.PushStyleColor(ImGuiCol.Text, messageColor);
                    ImGui.SetCursorPos(cur1);
                    ImGuiEx.TextWrapped($"{spaces} {x.TranslatedMessage ?? x.Message}");
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
                            ImGuiEx.TextWrapped(Cust.ColorFromTitle, $"From {x.OverrideName?.Split("@")[0] ?? subjectNoWorld}");
                        }
                        else
                        {
                            ImGuiEx.TextWrapped(Cust.ColorToTitle, $"From {x.OverrideName?.Split("@")[0] ?? me}");
                        }
                    }
                    ImGuiHelpers.ScaledDummy(new Vector2(20f, 1f));
                    ImGui.SameLine(0, 0);
                    ImGuiEx.TextWrapped(x.IsIncoming ? Cust.ColorFromMessage : Cust.ColorToMessage, $"[{timestamp}] {x.TranslatedMessage ?? x.Message}");
                    PostMessageFunctions(x);
                }
            }
        }
        if (MessageHistory.DoScroll > 0)
        {
            MessageHistory.DoScroll--;
            ImGui.SetScrollHereY();
        } 
        ImGui.EndChild();
        var isCmd = C.CommandPassthrough && msg.Trim().StartsWith("/");
        var cursor = ImGui.GetCursorPosY();
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - afterInputWidth);
        var inputCol = false;
        if (isCmd)
        {
            inputCol = true;
            ImGui.PushStyleColor(ImGuiCol.FrameBg, ImGui.GetStyle().Colors[(int)ImGuiCol.TitleBgActive] with { W = ImGui.GetStyle().Colors[(int)ImGuiCol.FrameBg].W });
        }
        var cur = ImGui.GetCursorPosY();
        if (ImGui.InputText("##outgoing", ref msg, 500, ImGuiInputTextFlags.EnterReturnsTrue))
        {
            SendMessage(subject, MessageHistory.Player.IsGenericChannel());
        }
        if (inputCol)
        {
            ImGui.PopStyleColor();
        }
        if (MessageHistory.SetFocus)
        {
            this.SetTransparency(false);
            ImGui.SetWindowFocus();
            ImGui.SetKeyboardFocusHere(-1);
            MessageHistory.SetFocus = false;
        }
        ImGui.SetWindowFontScale(ImGui.CalcTextSize(" ").Y / ImGuiEx.CalcIconSize(FontAwesomeIcon.ArrowRight).Y);
        ImGui.SameLine(0, 0);
        var icur1 = ImGui.GetCursorPos();
        ImGui.Dummy(Vector2.Zero);
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
        if (C.ButtonInvite && !this.MessageHistory.Player.IsGenericChannel())
        {
            ImGui.SameLine(0, 2);
            if (ImGuiEx.IconButton(FontAwesomeIcon.DoorOpen, "InviteToParty"))
            {
                if (Svc.Objects.Any(c => c is PlayerCharacter pc
                    && pc.HomeWorld.Id == this.MessageHistory.Player.HomeWorld && pc.Name.ToString() == this.MessageHistory.Player.Name))
                {
                    var result = P.InviteToParty(this.MessageHistory.Player, true);
                    if (result != null)
                    {
                        Notify.Error(result);
                    }
                    else
                    {
                        Notify.Info($"Inviting through World");
                    }
                }
                else
                {
                    var flSuccess = false;
                    foreach (var x in FriendList.Get())
                    {
                        if (flSuccess) break;
                        if (x.Name.ToString() == this.MessageHistory.Player.Name && x.HomeWorld == this.MessageHistory.Player.HomeWorld)
                        {
                            flSuccess = true;
                            if (x.IsOnline)
                            {
                                var sameWorld = Svc.ClientState.LocalPlayer.CurrentWorld.Id == x.CurrentWorld;
                                var result = P.InviteToParty(this.MessageHistory.Player, sameWorld, x.ContentId);
                                if (result != null)
                                {
                                    Notify.Error(result);
                                }
                                else
                                {
                                    Notify.Info($"Inviting through FrieldList ({(sameWorld ? "same world" : "different world")})");
                                }
                            }
                            else if (P.CIDlist.ContainsValue(x.ContentId))
                            {
                                var result = P.InviteToParty(this.MessageHistory.Player, true);
                                if (result != null)
                                {
                                    Notify.Error(result);
                                }
                                else
                                {
                                    Notify.Info($"Inviting through Chat History");
                                }
                            }
                            else
                            {
                                Notify.Error("Target appears to be offline.");
                            }
                        }
                    }
                    if (!flSuccess)
                    {
                        {
                            ImGui.OpenPopup("Invite");
                        }
                    }
                }
            }
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                ImGui.OpenPopup("Invite");
            }
            ImGuiEx.Tooltip("Invite to party (right click for extra options)");
            if (ImGui.BeginPopup("Invite"))
            {
                ImGuiEx.Text("Unable to determine player's current world.");
                if (ImGui.Selectable("Same world"))
                {
                    P.InviteToParty(this.MessageHistory.Player, true);
                }
                if (ImGui.Selectable("Different world"))
                {
                    if (P.IsFriend(this.MessageHistory.Player))
                    {
                        P.InviteToParty(this.MessageHistory.Player, false);
                    }
                    else
                    {
                        Notify.Error("This action is only possible for your friends.");
                    }
                }
                ImGui.EndPopup();
            }
        }
        
        if (!this.MessageHistory.Player.IsGenericChannel() && !P.IsFriend(this.MessageHistory.Player))
        {
            if (C.ButtonFriend)
            {
                ImGui.SameLine(0, 2);
                if (ImGuiEx.IconButton(FontAwesomeIcon.Smile, "AddFriend"))
                {
                    P.GameFunctions.SendFriendRequest(this.MessageHistory.Player.Name, (ushort)this.MessageHistory.Player.HomeWorld);
                }
                ImGuiEx.Tooltip("Add to friend list");
            }
            if (C.ButtonBlack)
            {
                ImGui.SameLine(0, 2);
                if (ImGuiEx.IconButton(FontAwesomeIcon.Frown, "AddBlacklist"))
                {
                    P.GameFunctions.AddToBlacklist(this.MessageHistory.Player.Name, (ushort)this.MessageHistory.Player.HomeWorld);
                }
                ImGuiEx.Tooltip("Add to blacklist");
            }
        }
        if (C.ButtonLog)
        {
            ImGui.SameLine(0, 2);
            if (ImGuiEx.IconButton(FontAwesomeIcon.Book, "Log"))
            {
                if (File.Exists(this.MessageHistory.LogFile))
                {
                    ShellStart(this.MessageHistory.LogFile);
                }
                else
                {
                    Notify.Error("No log exist yet");
                }
            }
            ImGuiEx.Tooltip("Open text log");
        }
        if (C.ButtonCharaCard)
        {
            ImGui.SameLine(0, 2);
            if (ImGuiEx.IconButton(FontAwesomeIcon.IdCard, "OpenCharaCard"))
            {
                P.OpenCharaCard(this.MessageHistory.Player);
            }
            ImGuiEx.Tooltip("Open adventurer plate");
        }
        ImGui.SameLine(0, 0);
        afterInputWidth = ImGui.GetCursorPosX() - icur1.X;
        ImGui.Dummy(Vector2.Zero);
        var bytes = Utils.GetLength(subject, msg);
        var fraction = (float)bytes.current / (float)bytes.max;
        ImGui.PushStyleColor(ImGuiCol.PlotHistogram, fraction > 1f?ImGuiColors.DalamudRed: Cust.ColorGeneric);
        ImGui.ProgressBar(fraction, new Vector2(ImGui.GetContentRegionAvail().X, 3f), "");
        ImGui.PopStyleColor();
        fieldHeight = ImGui.GetCursorPosY() - cursor;
        ImGui.SetWindowFontScale(1);
    }

    void PostMessageFunctions(SavedMessage x)
    {
        if(P.Translator.CurrentProvider != null)
        {
            if (x.AwaitingTranslation)
            {
                if (P.Translator.TranslationResults.TryGetValue(x.Message, out var tm))
                {
                    x.TranslatedMessage = tm;
                    x.AwaitingTranslation = false;
                    PluginLog.Verbose($"Message {x.Message} translation found {x.TranslatedMessage}");
                }
            }
            if (!x.IgnoreTranslation)
            {
                x.AwaitingTranslation = true;
                x.IgnoreTranslation = true;
                P.Translator.EnqueueTranslation(x.Message);
                PluginLog.Verbose($"Message {x.Message} translation enqueued");
            }
        }
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
            if(P.Translator.CurrentProvider != null)
            {
                if(ImGui.Selectable(x.TranslatedMessage == null? "Translate" : "Translate again"))
                {
                    P.Translator.TranslationResults.Remove(x.Message, out _);
                    x.AwaitingTranslation = false;
                    x.IgnoreTranslation = false;
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

    void SendMessage(string subject, bool generic)
    {
        var bytes = Utils.GetLength(subject, msg);
        var trimmed = msg.Trim();
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
            (P.TargetCommands.Any(x => msg.Equals(x, StringComparison.OrdinalIgnoreCase) || msg.StartsWith($"{x} ", StringComparison.OrdinalIgnoreCase)))
            && Svc.Objects.TryGetFirst(x => x is PlayerCharacter pc && pc.GetPlayerName() == subject && x.Struct()->GetIsTargetable(), out var obj))
            {
                Svc.Targets.SetTarget(obj);
                Notify.Info($"Targeting {subject}");
                new TickScheduler(delegate { Chat.Instance.SendMessage(trimmed); }, 100);
            }
            else
            {
                Chat.Instance.SendMessage(trimmed);
            }
            this.msg = "";
        }
        else
        {
            PluginLog.Verbose($"Begin send message to {subject} {generic}: {trimmed}");
            var error = P.SendDirectMessage(subject, trimmed, generic);
            if (error == null)
            {
                this.msg = "";
            }
            else
            {
                Notify.Error(msg);
            }
        }
        if (C.RefocusInputAfterSending)
        {
            MessageHistory.SetFocus = true;
        }
    }
}
