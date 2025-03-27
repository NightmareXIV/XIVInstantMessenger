using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using ECommons.Automation;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.Sheets;
using Messenger.Configuration;
using Messenger.Gui.Settings;
using Messenger.Gui.TitleButtons;

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
    internal PseudoMultilineInput Input = new();
    private bool BlockEmojiSelection = false;
    internal int DisplayCap = C.DisplayedMessages;
    private int FrameCount = 0;
    private int ScrollToMessage = -1;

    internal string OwningTab => C.TabWindowAssociations.TryGetValue(MessageHistory.HistoryPlayer.ToString(), out var owner) ? owner : null;

    internal ChannelCustomization Cust => MessageHistory.HistoryPlayer.GetCustomization();

    private InviteToPartyButton InviteToPartyButton;
    private AddFriendButton AddFriendButton;
    private AddToBlacklistButton AddToBlacklistButton;
    private OpenLogButton OpenLogButton;
    private OpenCharaCardButton OpenCharaCardButton;

    public ChatWindow(MessageHistory messageHistory) :
        base($"Chat with {messageHistory.HistoryPlayer.GetChannelName()}###Messenger - {messageHistory.HistoryPlayer.Name}{messageHistory.HistoryPlayer.HomeWorld}"
            , ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoScrollbar)
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
        if(InviteToPartyButton.ShouldDisplay()) window.TitleBarButtons.Add(InviteToPartyButton.Button);
        if(AddFriendButton.ShouldDisplay()) window.TitleBarButtons.Add(AddFriendButton.Button);
        if(AddToBlacklistButton.ShouldDisplay()) window.TitleBarButtons.Add(AddToBlacklistButton.Button);
        if(OpenLogButton.ShouldDisplay()) window.TitleBarButtons.Add(OpenLogButton.Button);
        if(OpenCharaCardButton.ShouldDisplay()) window.TitleBarButtons.Add(OpenCharaCardButton.Button);
    }

    internal void SetTransparency(bool isTransparent)
    {
        Transparency = !isTransparent ? C.TransMax : C.TransMin;
    }

    internal bool HideByCombat => !KeepInCombat && ((Svc.Condition[ConditionFlag.InCombat] && C.AutoHideCombat) || (Svc.Condition[ConditionFlag.BoundByDuty56] && C.AutoHideDuty));

    public override bool DrawConditions()
    {
        var ret = true;
        if(P.Hidden)
        {
            ret = false;
        }
        if(HideByCombat)
        {
            ret = false;
        }
        if(!ret)
        {
            MessageHistory.UnsetFocus();
        }
        return ret;
    }

    public override void OnOpen()
    {
        P.lastHistory = MessageHistory;
        if(C.WindowCascading)
        {
            SetPosition = true;
        }
        if(!C.NoBringWindowToFrontIfTyping || !ImGui.GetIO().WantCaptureKeyboard)
        {
            BringToFront = true;
        }
    }

    public override void OnClose()
    {
        KeepInCombat = false;
        if(S.MessageProcessor.Chats.All(x => !x.Value.ChatWindow.IsOpen))
        {
            //Notify.Info("Cascading has been reset");
            ChatWindow.CascadingPosition = 0;
        }
    }

    public override void Update()
    {
        UpdateLastFrame();
        TitleBarButtons.Clear();
    }

    public void UpdateLastFrame()
    {
        var f = ImGui.GetFrameCount();
        if(f - FrameCount > 1)
        {
            PluginLog.Verbose($"Window {MessageHistory.HistoryPlayer} just opened");
            DisplayCap = C.DisplayedMessages;
        }
        FrameCount = f;
    }

    public override void PreDraw()
    {
        if(C.NoResize && !ImGui.GetIO().KeyCtrl)
        {
            Flags |= ImGuiWindowFlags.NoResize;
        }
        else
        {
            Flags &= ~ImGuiWindowFlags.NoResize;
        }
        if(C.NoMove && !ImGui.GetIO().KeyCtrl)
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
        if(Unread)
        {
            TitleColored = true;
            ImGui.PushStyleColor(ImGuiCol.TitleBg, ImGuiCol.TitleBg.GetFlashColor(Cust));
            ImGui.PushStyleColor(ImGuiCol.TitleBgCollapsed, ImGuiCol.TitleBgCollapsed.GetFlashColor(Cust));
        }
        if(IsTransparent) ImGui.PushStyleVar(ImGuiStyleVar.Alpha, Transparency);
        if(SetPosition)
        {
            SetPosition = false;
            if(C.WindowCascading)
            {
                var cPos = CascadingPosition % C.WindowCascadingReset;
                var xmult = (int)(CascadingPosition / C.WindowCascadingReset);
                ImGuiHelpers.SetNextWindowPosRelativeMainViewport(ImGuiHelpers.MainViewport.Pos
                    + new Vector2(C.WindowCascadingX, C.WindowCascadingY)
                    + new Vector2(C.WindowCascadingXDelta * cPos + C.WindowCascadingXDelta * xmult, C.WindowCascadingYDelta * cPos));
                CascadingPosition++;
                if(CascadingPosition > C.WindowCascadingReset * C.WindowCascadingMaxColumns)
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
        if(P.FontManager.FontPushed && !P.FontManager.FontReady)
        {
            ImGuiEx.Text($"Loading font, please wait...");
            return;
        }
        var prev = P.GetPreviousMessageHistory(MessageHistory);
        /*ImGuiEx.Text($"{(prev == null ? "null" : prev.Player)}");
        ImGui.SameLine();
        ImGuiEx.TextCopy($"{this.messageHistory.GetLatestMessageTime()}");*/
        if(ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows))
        {
            Unread = false;
            Transparency = Math.Min(C.TransMax, Transparency + C.TransDelta);
        }
        else
        {
            if(!C.DisallowTransparencyHovered && ImGui.IsWindowHovered(ImGuiHoveredFlags.RootAndChildWindows | ImGuiHoveredFlags.AllowWhenBlockedByPopup))
            {
                Transparency = Math.Min(C.TransMax, Transparency + C.TransDelta);
            }
            else
            {
                Transparency = Math.Max(C.TransMin, Transparency - C.TransDelta);
            }
        }
        if(BringToFront)
        {
            BringToFront = false;
            CImGui.igBringWindowToDisplayFront(CImGui.igGetCurrentWindow());
        }
        var subject = MessageHistory.HistoryPlayer.IsGenericChannel() ? Utils.GetGenericCommand(MessageHistory.HistoryPlayer) : MessageHistory.HistoryPlayer.GetPlayerName();
        string tellTarget = null;
        if(MessageHistory.IsEngagement)
        {
            var eng = MessageHistory.HistoryPlayer.GetEngagementInfo();
            if(eng != null && eng.DefaultTarget != null)
            {
                if(eng.DefaultTarget.Value.IsGenericChannel())
                {
                    tellTarget = Utils.GetGenericCommand(eng.DefaultTarget.Value);
                }
                else
                {
                    tellTarget = eng.DefaultTarget.Value.GetPlayerName();
                }
            }
        }
        else
        {
            tellTarget = subject;
        }
        var subjectNoWorld = MessageHistory.HistoryPlayer.GetPlayerName().Split("@")[0];
        var me = Svc.ClientState.LocalPlayer?.Name.ToString().Split("@")[0] ?? "Me";
        ImGui.BeginChild($"##ChatChild{subject}", new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetContentRegionAvail().Y - fieldHeight));
        if(MessageHistory.Messages.Count == 0)
        {
            Utils.DrawWrappedText($"This is the beginning of your chat with {subject}");
        }
        bool? isIncoming = null;
        if(MessageHistory.LogLoaded && MessageHistory.LoadedMessages.Count > 0)
        {
            foreach(var message in MessageHistory.LoadedMessages)
            {
                MessageHistory.Messages.Insert(0, message);
            }
            MessageHistory.Scroll();
            MessageHistory.LoadedMessages.Clear();
        }
        (int year, int day) currentDay = (0, 0);
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, C.MessageLineSpacing));
        var startIndex = Math.Max(0, MessageHistory.Messages.Count - DisplayCap);
        if(startIndex != 0)
        {
            ImGuiEx.TextWrapped(EColor.OrangeBright, $"For performance reasons, {startIndex} messages have been hidden.");
            if(ImGui.Button($"Display {Math.Min(startIndex, C.DisplayedMessages)} more messages"))
            {
                Svc.Framework.RunOnTick(() => ScrollToMessage = startIndex, delayTicks: 1);
                DisplayCap += C.DisplayedMessages;
            }
        }
        for(var n = startIndex; n < MessageHistory.Messages.Count; n++)
        {
            var message = MessageHistory.Messages[n];
            if(message.IsSystem)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, Cust.ColorGeneric);
                message.Draw();
                ImGui.PopStyleColor();
            }
            else
            {
                if(!message.IsIncoming && Cust.NoOutgoing) continue;
                var time = DateTimeOffset.FromUnixTimeMilliseconds(message.Time).ToLocalTime();
                if(C.PrintDate)
                {
                    if(!(time.DayOfYear == currentDay.day && time.Year == currentDay.year))
                    {
                        ImGuiEx.Text(Cust.ColorGeneric, $"[{time.ToString(C.DateFormat)}]");
                        if(C.MessageSpacing > 0) ImGui.Dummy(new(C.MessageSpacing));
                        currentDay = (time.Year, time.DayOfYear);
                    }
                }
                var timestamp = time.ToString(C.MessageTimestampFormat);
                if(C.IRCStyle)
                {
                    var messageColor = message.IsIncoming ? Cust.ColorFromMessage : Cust.ColorToMessage;
                    var subjectColor = message.IsIncoming ? Cust.ColorFromTitle : Cust.ColorToTitle;
                    ImGuiEx.Text(Cust.ColorGeneric, $"{timestamp} ");
                    ImGui.SameLine(0, 0);
                    if(message.XivChatType != XivChatType.CustomEmote)
                    {
                        ImGuiEx.Text(messageColor, $"[");
                        ImGui.SameLine(0, 0);
                    }
                    ImGuiEx.Text(subjectColor, $"{message.OverrideName?.Split("@")[0] ?? (message.IsIncoming ? subjectNoWorld : me)}");
                    ImGui.SameLine(0, 0);
                    if(message.XivChatType != XivChatType.CustomEmote)
                    {
                        ImGuiEx.Text(messageColor, $"] ");
                        ImGui.SameLine(0, 0);
                    }
                    ImGui.PushStyleColor(ImGuiCol.Text, messageColor);
                    message.Draw("", "", () => PostMessageFunctionsShared(message));
                    PostMessageFunctions(message);
                    ImGui.PopStyleColor();
                }
                else
                {
                    if(message.IsIncoming != isIncoming)
                    {
                        isIncoming = message.IsIncoming;
                        if(message.IsIncoming)
                        {
                            ImGui.PushStyleColor(ImGuiCol.Text, Cust.ColorFromTitle);
                            Utils.DrawWrappedText($"From {message.OverrideName?.Split("@")[0] ?? subjectNoWorld}");
                            ImGui.PopStyleColor();
                        }
                        else
                        {
                            ImGui.PushStyleColor(ImGuiCol.Text, Cust.ColorToTitle);
                            Utils.DrawWrappedText($"From {message.OverrideName?.Split("@")[0] ?? me}");
                            ImGui.PopStyleColor();
                        }
                    }
                    ImGuiHelpers.ScaledDummy(new Vector2(20f, 1f));
                    ImGui.SameLine(0, 0);
                    ImGui.PushStyleColor(ImGuiCol.Text, message.IsIncoming ? Cust.ColorFromMessage : Cust.ColorToMessage);
                    message.Draw("[{timestamp}] ", "", () => PostMessageFunctionsShared(message));
                    PostMessageFunctions(message);
                    ImGui.PopStyleColor();
                }
            }

            if(ScrollToMessage == n)
            {
                PluginLog.Verbose($"Set scroll to {n}");
                ImGui.SetScrollHereY();
                ScrollToMessage = -1;
            }
            if(C.MessageSpacing > 0) ImGui.Dummy(new(C.MessageSpacing));
        }
        ImGui.PopStyleVar();
        if(MessageHistory.DoScroll > 0)
        {
            MessageHistory.DoScroll--;
            ImGui.SetScrollHereY();
        }
        if(C.PMLScrollDown && C.PMLEnable && Input.IsInputActive)
        {
            ImGui.SetScrollHereY();
        }
        ImGui.EndChild();
        var isCmd = C.CommandPassthrough && Input.SinglelineText.Trim().StartsWith("/");
        var cursor = ImGui.GetCursorPosY();
        //ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - afterInputWidth);
        Input.Width = ImGui.GetContentRegionAvail().X - afterInputWidth;
        var inputCol = false;
        if(isCmd)
        {
            inputCol = true;
            ImGui.PushStyleColor(ImGuiCol.FrameBg, ImGui.GetStyle().Colors[(int)ImGuiCol.TitleBgActive] with { W = ImGui.GetStyle().Colors[(int)ImGuiCol.FrameBg].W });
        }
        var cur = ImGui.GetCursorPosY();
        Input.Draw();
        if(C.UseAutoSave)
        {
            if(Input.IsInputActive)
            {
                Utils.AutoSaveMessage(this, false);
            }
            if(ImGui.IsItemDeactivated())
            {
                Utils.AutoSaveMessage(this, true);
            }
        }
        string firstMessage = null;
        string remainder = null;
        var split = C.SplitterEnable ? Utils.SplitMessage(Input.SinglelineText, tellTarget, out firstMessage, out remainder) : null;
        var isSplit = split != null && split.Count > 1 && firstMessage != null && remainder != null;
        if(Input.EnterWasPressed() && EzThrottler.Check("SendMessage"))
        {
            if(!C.DoubleEnterSend || !EzThrottler.Check("DoubleEnter"))
            {
                var wasSent = SendMessage(tellTarget);
                if(wasSent && Input.IsMultiline)
                {
                    ImGui.SetWindowFocus(null);
                    ImGui.SetWindowFocus(WindowName);
                }
            }
            else
            {
                EzThrottler.Throttle("DoubleEnter", C.DoubleEnterDelay, true);
            }
        }
        if(inputCol)
        {
            ImGui.PopStyleColor();
        }
        if(MessageHistory.ShouldSetFocus())
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

        if(C.EnableEmoji && C.EnableEmojiPicker)
        {
            ImGui.SameLine(0, 2);
            if(ImGuiEx.IconButton(FontAwesomeIcon.SmileWink, "Insert emoji"))
            {
                if(!BlockEmojiSelection) Input.OpenEmojiSelector();
            }
            if(ImGui.IsItemHovered() && ImGui.IsMouseDown(ImGuiMouseButton.Left) && Input.IsSelectingEmoji)
            {
                BlockEmojiSelection = true;
            }
            if(BlockEmojiSelection)
            {
                if(ImGui.IsItemHovered() && ImGui.IsMouseDown(ImGuiMouseButton.Left))
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

        if(C.ButtonSend)
        {
            ImGui.SameLine(0, 2);
            var col = !EzThrottler.Check("DoubleEnter");
            if(col)
            {
                ImGui.PushStyleColor(ImGuiCol.Button, EColor.Green);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, EColor.Green);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, EColor.Green);
            }
            if(isCmd)
            {
                if(ImGuiEx.IconButton(FontAwesomeIcon.FastForward, "Execute command", enabled: EzThrottler.Check("SendMessage")))
                {
                    SendMessage(tellTarget);
                }
                ImGuiEx.Tooltip("Execute command");
            }
            else
            {

                if(ImGuiEx.IconButton(FontAwesomeIcon.ArrowRight, "Send", enabled: EzThrottler.Check("SendMessage")))
                {
                    SendMessage(tellTarget);
                }
                ImGuiEx.Tooltip("Send message");
            }
            if(col)
            {
                ImGui.PopStyleColor(3);
            }
        }
        ImGui.SameLine(0, 0);
        afterInputWidth = ImGui.GetCursorPosX() - icur1.X;
        ImGui.Dummy(Vector2.Zero);
        if(isSplit)
        {
            var begin = ImGui.GetCursorScreenPos();
            foreach(var x in split)
            {
                var bytes = Utils.GetLength(tellTarget, x);
                var fraction = (float)bytes.current / (float)bytes.max;
                ImGui.PushStyleColor(ImGuiCol.PlotHistogram, fraction > 1f ? ImGuiColors.DalamudRed : Cust.ColorGeneric);
                ImGui.ProgressBar(fraction, new Vector2(ImGui.GetContentRegionAvail().X, 3f), "");
                ImGui.PopStyleColor();
            }
            if(ImGui.IsMouseHoveringRect(begin, new(begin.X + ImGui.GetContentRegionAvail().X, ImGui.GetCursorScreenPos().Y)))
            {
                ImGui.PushFont(UiBuilder.DefaultFont);
                ImGui.BeginTooltip();
                ImGui.PushTextWrapPos(ImGui.GetFontSize() * 45f);
                ImGuiEx.Text($"This message will be split. Keep pressing send button or enter to sequentially send the following messages:\n\n{split.Print("\n\n")}");
                ImGui.Separator();
                ImGuiEx.Text(ImGuiColors.DalamudGrey3, $"Debug:\nFirst message:\n{firstMessage}\n\nRemainder:\n{remainder}");
                ImGui.PopTextWrapPos();
                ImGui.EndTooltip();
                ImGui.PopFont();
            }
        }
        else
        {

            var bytes = Utils.GetLength(tellTarget, Input.SinglelineText);
            var fraction = (float)bytes.current / (float)bytes.max;
            ImGui.PushStyleColor(ImGuiCol.PlotHistogram, fraction > 1f ? ImGuiColors.DalamudRed : Cust.ColorGeneric);
            ImGui.ProgressBar(fraction, new Vector2(ImGui.GetContentRegionAvail().X, 3f), "");
            ImGui.PopStyleColor();
        }
        fieldHeight = ImGui.GetCursorPosY() - cursor;
        ImGui.SetWindowFontScale(1);

        if(MessageHistory.IsEngagement)
        {
            ImGui.PushFont(UiBuilder.DefaultFont);
            try
            {
                var engagementInfo = C.Engagements.FirstOrDefault(x => x.Name == MessageHistory.HistoryPlayer.Name);
                if(engagementInfo != null)
                {
                    ImGuiEx.LineCentered($"Engagement{MessageHistory.HistoryPlayer.GetPlayerName()}", () =>
                    {
                        ImGui.Checkbox("##enableEng", ref engagementInfo.Enabled);
                        ImGuiEx.Tooltip("Enable this engagement. If you disable it, no new messages will be added.");
                        ImGui.SameLine();
                        ImGui.SetNextItemWidth(110f);
                        if(ImGui.BeginCombo("##ctrl", $"{engagementInfo.Participants.Count} participants", ImGuiComboFlags.HeightLarge))
                        {
                            TabEngagement.DrawEngagementEditTable(engagementInfo, false);
                            ImGui.EndCombo();
                        }
                        ImGui.SameLine();
                        if(ImGuiEx.IconButton(FontAwesomeIcon.UserEdit))
                        {
                            S.XIMModalWindow.Open($"Member editing for {engagementInfo.Name}", () => TabEngagement.EditMemberList(engagementInfo));
                        }
                        ImGuiEx.Tooltip("Edit member list");
                        ImGui.SameLine();
                        if(ImGuiEx.IconButton(FontAwesomeIcon.Crosshairs, enabled: Svc.Targets.Target is IPlayerCharacter pc && pc.ObjectIndex != 0 && !engagementInfo.Participants.Contains(new(pc.Name.ToString(), pc.HomeWorld.RowId))))
                        {
                            engagementInfo.Participants.Add(new(Svc.Targets.Target.Name.ToString(), ((IPlayerCharacter)Svc.Targets.Target).HomeWorld.RowId));
                        }
                        ImGuiEx.Tooltip("Add targeted player to this engagement");
                        ImGui.SameLine();
                        ImGui.SetNextItemWidth(120f);
                        string targetName;
                        if(engagementInfo.DefaultTarget == null)
                        {
                            targetName = "No default receiver";
                        }
                        else
                        {
                            targetName = engagementInfo.DefaultTarget.Value.GetChannelName();
                            if(!engagementInfo.DefaultTarget.Value.IsGenericChannel() && !engagementInfo.Participants.Contains(engagementInfo.DefaultTarget.Value))
                            {
                                engagementInfo.DefaultTarget = null;
                            }
                        }
                        if(ImGui.BeginCombo("##engTarget", $"{targetName}", ImGuiComboFlags.HeightRegular))
                        {
                            ImGuiEx.Text($"Select default target for sending messages via enter button.");
                            if(ImGui.Selectable("None", engagementInfo.DefaultTarget == null))
                            {
                                engagementInfo.DefaultTarget = null;
                            }
                            ImGuiEx.EzTabBar("##seldeftar",
                                ("Players", () =>
                                {
                                    foreach(var x in engagementInfo.Participants)
                                    {
                                        if(ImGui.Selectable(x.GetPlayerName(), x == engagementInfo.DefaultTarget)) engagementInfo.DefaultTarget = x;
                                    }
                                }, null, false),
                                ("Generic channels", () =>
                                {
                                    for(var i = 1; i < TabIndividual.Types.Length; i++)
                                    {
                                        var x = new Sender(TabIndividual.Types[i].ToString(), 0);
                                        if(ImGui.Selectable(x.GetChannelName(), x == engagementInfo.DefaultTarget))
                                        {
                                            engagementInfo.DefaultTarget = x;
                                        }
                                    }
                                }, null, false)
                                );
                            ImGui.EndCombo();
                        }
                    });
                }
                if(ImGui.BeginPopup("SelectSendSubject"))
                {
                    if(ImGui.BeginMenu("- Players -"))
                    {
                        foreach(var x in engagementInfo.Participants)
                        {
                            if(ImGui.Selectable(x.GetPlayerName()))
                            {
                                SendMessage(x.GetChannelName(), false);
                            }
                        }
                        ImGui.EndMenu();
                    }
                    if(ImGui.BeginMenu("- Channels -"))
                    {
                        for(var i = 1; i < TabIndividual.Types.Length; i++)
                        {
                            if(TabIndividual.Types[i].EqualsAny(XivChatType.Say, XivChatType.CustomEmote)) continue;
                            var x = new Sender(TabIndividual.Types[i].ToString(), 0);
                            if(ImGui.Selectable(x.GetChannelName()))
                            {
                                SendMessage(Utils.GetGenericCommand(x), true);
                            }
                        }
                        ImGui.EndMenu();
                    }
                    ImGui.Separator();

                    for(var i = 1; i < TabIndividual.Types.Length; i++)
                    {
                        if(!TabIndividual.Types[i].EqualsAny(XivChatType.Say, XivChatType.CustomEmote)) continue;
                        var x = new Sender(TabIndividual.Types[i].ToString(), 0);
                        if(ImGui.Selectable(x.GetChannelName()))
                        {
                            SendMessage(Utils.GetGenericCommand(x), true);
                        }
                    }
                    ImGui.EndPopup();
                }
            }
            catch(Exception e)
            {
                e.Log();
            }

            ImGui.PopFont();
            fieldHeight = ImGui.GetCursorPosY() - cursor;
        }

        bool SendMessage(string subject, bool? generic = null)
        {
            if(!EzThrottler.Check("SendMessage")) return false;
            remainder ??= "";
            var ret = false;
            string trimmed;
            if(isSplit)
            {
                trimmed = firstMessage;
            }
            else
            {
                trimmed = Input.SinglelineText.Trim();
            }
            if(subject == null && !(trimmed.StartsWith('/') && C.CommandPassthrough))
            {
                ImGui.OpenPopup("SelectSendSubject");
                return ret;
            }
            if(MessageHistory.IsEngagement)
            {
                generic ??= MessageHistory.HistoryPlayer.GetEngagementInfo().DefaultTarget?.IsGenericChannel() ?? false;
            }
            else
            {
                generic ??= MessageHistory.HistoryPlayer.IsGenericChannel();
            }
            var bytes = Utils.GetLength(subject, trimmed);
            if(trimmed.Length == 0)
            {
                //Notify.Error("Message is empty!");
            }
            else if(bytes.current > bytes.max)
            {
                Notify.Error("Message is too long!");
            }
            else if(trimmed.StartsWith('/') && C.CommandPassthrough)
            {
                if(!generic.Value && C.AutoTarget &&
                (P.TargetCommands.Any(x => trimmed.Equals(x, StringComparison.OrdinalIgnoreCase) || trimmed.StartsWith($"{x} ", StringComparison.OrdinalIgnoreCase)))
                && Svc.Objects.TryGetFirst(x => x is IPlayerCharacter pc && pc.GetPlayerName() == subject && x.IsTargetable, out var obj))
                {
                    Svc.Targets.SetTarget(obj);
                    //Notify.Info($"Targeting {subject}");
                    new TickScheduler(delegate { Chat.Instance.SendMessage(trimmed); }, 100);
                    ret = true;
                }
                else
                {
                    Chat.Instance.SendMessage(trimmed);
                    ret = true;
                }
                if(C.UseAutoSave) Utils.AutoSaveMessage(this, true);
                Input.SinglelineText = isSplit ? remainder : "";
                if(isSplit) EzThrottler.Throttle("SendMessage", C.IntervalBetweenSends, true);
            }
            else
            {
                PluginLog.Verbose($"Begin send message to {subject} {generic}: {trimmed}");
                var error = P.SendDirectMessage(subject, trimmed, generic.Value);
                if(error == null)
                {
                    ret = true;
                    if(C.UseAutoSave) Utils.AutoSaveMessage(this, true);
                    Input.SinglelineText = isSplit ? remainder : "";
                    if(isSplit) EzThrottler.Throttle("SendMessage", C.IntervalBetweenSends, true);
                }
                else
                {
                    Notify.Error(Input.SinglelineText);
                }
            }
            if(C.RefocusInputAfterSending)
            {
                MessageHistory.SetFocusAtNextFrame();
            }
            return ret;
        }
    }

    private void PostMessageFunctionsShared(SavedMessage x)
    {
        if(C.ClickToOpenLink && ImGui.IsItemHovered())
        {
            foreach(var s in x.Message.Split(" "))
            {
                if(s.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                    || s.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, Cust.ColorGeneric);
                    ImGuiEx.SetTooltip($"Link found:\n{s}\nClick to open");
                    ImGui.PopStyleColor();
                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                    if(ImGui.IsItemClicked(ImGuiMouseButton.Left))
                    {
                        new OpenLinkWindow(s);
                    }
                    break;
                }
            }
        }
        if(ImGui.IsItemClicked(ImGuiMouseButton.Right))
        {
            ImGui.OpenPopup($"MessageDetail{x.GUID}");
        }
    }

    private void PostMessageFunctions(SavedMessage x)
    {
        if(ImGui.BeginPopup($"MessageDetail{x.GUID}"))
        {
            ImGui.PushStyleColor(ImGuiCol.Text, Cust.ColorGeneric);
            //this.SetTransparency(false);
            ImGui.SetNextItemWidth(400f);
            var msg = x.Message;
            ImGui.InputText("##copyTextMsg", ref msg, 10000);
            if(ImGui.Selectable($"Copy message to clipboard"))
            {
                ImGui.SetClipboardText(x.Message);
            }
            var linkN = false;
            foreach(var s in x.Message.Split(" "))
            {
                if(s.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                    || s.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    if(!linkN)
                    {
                        linkN = true;
                        ImGui.Separator();
                        ImGuiEx.Text("Found links in message:");
                    }
                    if(ImGui.Selectable($"{s}"))
                    {
                        new OpenLinkWindow(s);
                    }
                }
            }
            if(x.MapPayload != null)
            {
                if(ImGui.Selectable("Open map link"))
                {
                    Safe(delegate
                    {
                        Svc.GameGui.OpenMapWithMapLink(x.MapPayload);
                    });
                }
            }
            if(x.Item != null)
            {
                if(ImGui.Selectable("Print item details"))
                {
                    Safe(delegate
                    {
                        Svc.Chat.Print(new SeStringBuilder().Add(Utils.GetItemPayload(Svc.Data.GetExcelSheet<Item>().GetRowOrDefault(x.Item.Item.RowId), x.Item.IsHQ)).BuiltString);
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
        if(IsTransparent) ImGui.PopStyleVar();
        if(TitleColored)
        {
            ImGui.PopStyleColor(2);
        }
    }
}
