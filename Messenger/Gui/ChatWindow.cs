﻿using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using ECommons.Automation;
using ECommons.GameFunctions;
using Messenger.FontControl;
using Messenger.FriendListManager;
using System.IO;
using static FFXIVClientStructs.FFXIV.Client.Graphics.Kernel.VertexShader;

namespace Messenger.Gui;

internal unsafe class ChatWindow : Window
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

    internal string OwningTab => C.TabWindowAssociations.TryGetValue(MessageHistory.Player.ToString(), out string owner) ? owner : null;

    internal ChannelCustomization Cust => MessageHistory.Player.GetCustomization();

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
    }

    internal void SetTransparency(bool isTransparent)
    {
        Transparency = !isTransparent ? C.TransMax : C.TransMin;
    }

    internal bool HideByCombat => Svc.Condition[ConditionFlag.InCombat] && C.AutoHideCombat && !KeepInCombat;

    public override bool DrawConditions()
    {
        bool ret = true;
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
                int cPos = CascadingPosition % C.WindowCascadingReset;
                int xmult = (int)(CascadingPosition / C.WindowCascadingReset);
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
        if (P.FontManager.FontPushed && !P.FontManager.FontReady)
        {
            ImGuiEx.Text($"Loading font, please wait...");
            return;
        }
        MessageHistory prev = P.GetPreviousMessageHistory(MessageHistory);
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
        string subject = MessageHistory.Player.IsGenericChannel() ? Enum.GetValues<XivChatType>().First(x => x.ToString() == MessageHistory.Player.Name).GetCommand() : MessageHistory.Player.GetPlayerName();
        string subjectNoWorld = MessageHistory.Player.GetPlayerName().Split("@")[0];
        string me = Svc.ClientState.LocalPlayer?.Name.ToString().Split("@")[0] ?? "Me";
        ImGui.BeginChild($"##ChatChild{subject}", new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetContentRegionAvail().Y - fieldHeight));
        if (MessageHistory.Messages.Count == 0)
        {
            Utils.DrawWrappedText($"This is the beginning of your chat with {subject}");
        }
        bool? isIncoming = null;
        if (MessageHistory.LogLoaded && MessageHistory.LoadedMessages.Count > 0)
        {
            foreach (SavedMessage message in MessageHistory.LoadedMessages)
            {
                MessageHistory.Messages.Insert(0, message);
            }
            MessageHistory.Scroll();
            MessageHistory.LoadedMessages.Clear();
        }
        (int year, int day) currentDay = (0, 0);
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, C.MessageLineSpacing));
        foreach (SavedMessage x in MessageHistory.Messages)
        {
            if (x.IsSystem)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, Cust.ColorGeneric);
                x.Draw();
                ImGui.PopStyleColor();
            }
            else
            {
                DateTimeOffset time = DateTimeOffset.FromUnixTimeMilliseconds(x.Time).ToLocalTime();
                if (C.PrintDate)
                {
                    if (!(time.DayOfYear == currentDay.day && time.Year == currentDay.year))
                    {
                        ImGuiEx.Text(Cust.ColorGeneric, $"[{time.ToString(C.DateFormat)}]");
                        if (C.MessageSpacing > 0) ImGui.Dummy(new(C.MessageSpacing));
                        currentDay = (time.Year, time.DayOfYear);
                    }
                }
                string timestamp = time.ToString(C.MessageTimestampFormat);
                if (C.IRCStyle)
                {
                    Vector4 messageColor = x.IsIncoming ? Cust.ColorFromMessage : Cust.ColorToMessage;
                    Vector4 subjectColor = x.IsIncoming ? Cust.ColorFromTitle : Cust.ColorToTitle;
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
        bool isCmd = C.CommandPassthrough && Input.SinglelineText.Trim().StartsWith("/");
        float cursor = ImGui.GetCursorPosY();
        //ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - afterInputWidth);
        Input.Width = ImGui.GetContentRegionAvail().X - afterInputWidth;
        bool inputCol = false;
        if (isCmd)
        {
            inputCol = true;
            ImGui.PushStyleColor(ImGuiCol.FrameBg, ImGui.GetStyle().Colors[(int)ImGuiCol.TitleBgActive] with { W = ImGui.GetStyle().Colors[(int)ImGuiCol.FrameBg].W });
        }
        float cur = ImGui.GetCursorPosY();
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
        Vector2 icur1 = ImGui.GetCursorPos();
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
        if (C.ButtonInvite && !MessageHistory.Player.IsGenericChannel())
        {
            ImGui.SameLine(0, 2);
            if (ImGuiEx.IconButton(FontAwesomeIcon.DoorOpen, "InviteToParty"))
            {
                if (Svc.Objects.Any(c => c is PlayerCharacter pc
                    && pc.HomeWorld.Id == MessageHistory.Player.HomeWorld && pc.Name.ToString() == MessageHistory.Player.Name))
                {
                    string result = P.InviteToParty(MessageHistory.Player, true);
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
                    bool flSuccess = false;
                    foreach (FriendListEntry x in FriendList.Get())
                    {
                        if (flSuccess) break;
                        if (x.Name.ToString() == MessageHistory.Player.Name && x.HomeWorld == MessageHistory.Player.HomeWorld)
                        {
                            flSuccess = true;
                            if (x.IsOnline)
                            {
                                bool sameWorld = Svc.ClientState.LocalPlayer.CurrentWorld.Id == x.CurrentWorld;
                                string result = P.InviteToParty(MessageHistory.Player, sameWorld, x.ContentId);
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
                                string result = P.InviteToParty(MessageHistory.Player, true);
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
                    P.InviteToParty(MessageHistory.Player, true);
                }
                if (ImGui.Selectable("Different world"))
                {
                    if (P.IsFriend(MessageHistory.Player))
                    {
                        P.InviteToParty(MessageHistory.Player, false);
                    }
                    else
                    {
                        Notify.Error("This action is only possible for your friends.");
                    }
                }
                ImGui.EndPopup();
            }
        }

        if (!MessageHistory.Player.IsGenericChannel() && !P.IsFriend(MessageHistory.Player))
        {
            if (C.ButtonFriend)
            {
                ImGui.SameLine(0, 2);
                if (ImGuiEx.IconButton(FontAwesomeIcon.Smile, "AddFriend"))
                {
                    P.GameFunctions.SendFriendRequest(MessageHistory.Player.Name, (ushort)MessageHistory.Player.HomeWorld);
                }
                ImGuiEx.Tooltip("Add to friend list");
            }
            if (C.ButtonBlack)
            {
                ImGui.SameLine(0, 2);
                if (ImGuiEx.IconButton(FontAwesomeIcon.Frown, "AddBlacklist"))
                {
                    P.GameFunctions.AddToBlacklist(MessageHistory.Player.Name, (ushort)MessageHistory.Player.HomeWorld);
                }
                ImGuiEx.Tooltip("Add to blacklist");
            }
        }
        if (C.ButtonLog)
        {
            ImGui.SameLine(0, 2);
            if (ImGuiEx.IconButton(FontAwesomeIcon.Book, "Log"))
            {
                if (File.Exists(MessageHistory.LogFile))
                {
                    ShellStart(MessageHistory.LogFile);
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
                P.OpenCharaCard(MessageHistory.Player);
            }
            ImGuiEx.Tooltip("Open adventurer plate");
        }
        ImGui.SameLine(0, 0);
        afterInputWidth = ImGui.GetCursorPosX() - icur1.X;
        ImGui.Dummy(Vector2.Zero);
        (int current, int max) bytes = Utils.GetLength(subject, Input.SinglelineText);
        float fraction = (float)bytes.current / (float)bytes.max;
        ImGui.PushStyleColor(ImGuiCol.PlotHistogram, fraction > 1f ? ImGuiColors.DalamudRed : Cust.ColorGeneric);
        ImGui.ProgressBar(fraction, new Vector2(ImGui.GetContentRegionAvail().X, 3f), "");
        ImGui.PopStyleColor();
        fieldHeight = ImGui.GetCursorPosY() - cursor;
        ImGui.SetWindowFontScale(1);
    }

    private void PostMessageFunctions(SavedMessage x)
    {
        if (P.Translator.CurrentProvider != null)
        {
            if (x.AwaitingTranslation)
            {
                if (P.Translator.TranslationResults.TryGetValue(x.Message, out string tm))
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
            foreach (string s in x.Message.Split(" "))
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
            string msg = x.Message;
            ImGui.InputText("##copyTextMsg", ref msg, 10000);
            if (ImGui.Selectable($"Copy message to clipboard"))
            {
                ImGui.SetClipboardText(x.Message);
            }
            bool linkN = false;
            foreach (string s in x.Message.Split(" "))
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
            if (P.Translator.CurrentProvider != null)
            {
                if (ImGui.Selectable(x.TranslatedMessage == null ? "Translate" : "Translate again"))
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

    private void SendMessage(string subject, bool generic)
    {
        (int current, int max) bytes = Utils.GetLength(subject, Input.SinglelineText);
        string trimmed = Input.SinglelineText.Trim();
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
            && Svc.Objects.TryGetFirst(x => x is PlayerCharacter pc && pc.GetPlayerName() == subject && x.IsTargetable, out Dalamud.Game.ClientState.Objects.Types.GameObject obj))
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
            string error = P.SendDirectMessage(subject, trimmed, generic);
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
