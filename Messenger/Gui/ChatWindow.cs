using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using ECommons.GameFunctions;
using Messenger.FontControl;
using Messenger.FriendListManager;
using System.IO;

namespace Messenger.Gui
{
    internal unsafe class ChatWindow : Window
    {
        static int CascadingPosition = 0;
        internal MessageHistory messageHistory;
        float fieldHeight = 0;
        float afterInputWidth = 0;
        string msg = "";
        internal bool KeepInCombat = false;
        internal bool Unread = false;
        bool TitleColored = false;
        float Transparency = P.config.TransMax;
        bool IsTransparent = true;
        internal bool SetPosition = false;
        internal bool BringToFront = false;
        bool fontPushed;

        public ChatWindow(MessageHistory messageHistory) : 
            base($"Chat with {messageHistory.Player.GetPlayerName()}###Messenger - {messageHistory.Player.Name}{messageHistory.Player.HomeWorld}"
                , ImGuiWindowFlags.NoFocusOnAppearing)
        {
            this.messageHistory = messageHistory;
            this.SizeConstraints = new()
            {
                MinimumSize = new(200, 200),
                MaximumSize = new(9999, 9999)
            };
        }

        internal void SetTransparency(bool isTransparent)
        {
            this.Transparency = !isTransparent ? P.config.TransMax : P.config.TransMin;
        }

        public override bool DrawConditions()
        {
            var ret = true;
            if (P.Hidden)
            {
                ret = false;
            }
            if (Svc.Condition[ConditionFlag.InCombat] && P.config.AutoHideCombat && !KeepInCombat)
            {
                ret = false;
            }
            if (!ret)
            {
                this.messageHistory.SetFocus = false;
            }
            return ret;
        }

        public override void OnOpen()
        {
            P.lastHistory = this.messageHistory;
            if (P.config.WindowCascading)
            {
                SetPosition = true;
            }
            if (!P.config.NoBringWindowToFrontIfTyping || !ImGui.GetIO().WantCaptureKeyboard)
            {
                BringToFront = true;
            }
        }

        public override void OnClose()
        {
            KeepInCombat = false;
            if(P.Chats.All(x => !x.Value.chatWindow.IsOpen))
            {
                //Notify.Info("Cascading has been reset");
                ChatWindow.CascadingPosition = 0;
            }
        }

        public override void PreDraw()
        {
            if(P.config.NoResize && !ImGui.GetIO().KeyCtrl)
            {
                this.Flags |= ImGuiWindowFlags.NoResize;
            }
            else
            {
                this.Flags &= ~ImGuiWindowFlags.NoResize;
            }
            this.Size = P.config.DefaultSize;
            this.SizeCondition = P.config.ResetSizeOnAppearing ? ImGuiCond.Appearing : ImGuiCond.FirstUseEver;
            IsTransparent = Transparency < 1f;
            TitleColored = false;
            if (Unread && Environment.TickCount % 1000 > 500)
            {
                TitleColored = true;
                ImGui.PushStyleColor(ImGuiCol.TitleBg, P.config.ColorTitleFlash);
                ImGui.PushStyleColor(ImGuiCol.TitleBgCollapsed, P.config.ColorTitleFlash);
            }
            if(IsTransparent) ImGui.PushStyleVar(ImGuiStyleVar.Alpha, Transparency);
            if (SetPosition)
            {
                SetPosition = false;
                if (P.config.WindowCascading)
                {
                    var cPos = CascadingPosition % P.config.WindowCascadingReset;
                    var xmult = (int)(CascadingPosition / P.config.WindowCascadingReset);
                    ImGuiHelpers.SetNextWindowPosRelativeMainViewport(ImGuiHelpers.MainViewport.Pos
                        + new Vector2(P.config.WindowCascadingX, P.config.WindowCascadingY)
                        + new Vector2(P.config.WindowCascadingXDelta * cPos + P.config.WindowCascadingXDelta * xmult, P.config.WindowCascadingYDelta * cPos));
                    CascadingPosition++;
                    if (CascadingPosition > P.config.WindowCascadingReset * P.config.WindowCascadingMaxColumns)
                    {
                        CascadingPosition = 0;
                    }
                }
            }
            fontPushed = FontPusher.PushConfiguredFont();
        }

        public override void Draw()
        {
            var prev = P.GetPreviousMessageHistory(this.messageHistory);
            /*ImGuiEx.Text($"{(prev == null ? "null" : prev.Player)}");
            ImGui.SameLine();
            ImGuiEx.TextCopy($"{this.messageHistory.GetLatestMessageTime()}");*/
            if (ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows))
            {
                Unread = false;
                Transparency = Math.Min(P.config.TransMax, Transparency + P.config.TransDelta);
            }
            else
            {
                if (ImGui.IsWindowHovered(ImGuiHoveredFlags.RootAndChildWindows | ImGuiHoveredFlags.AllowWhenBlockedByPopup))
                {
                    Transparency = Math.Min(P.config.TransMax, Transparency + P.config.TransDelta);
                }
                else
                {
                    Transparency = Math.Max(P.config.TransMin, Transparency - P.config.TransDelta);
                }
            }
            if (BringToFront)
            {
                BringToFront = false;
                Native.igBringWindowToDisplayFront(Native.igGetCurrentWindow());
            }
            var subject = messageHistory.Player.GetPlayerName();
            var subjectNoWorld = messageHistory.Player.GetPlayerName().Split("@")[0];
            var me = Svc.ClientState.LocalPlayer?.Name.ToString().Split("@")[0] ?? "Me";
            ImGui.BeginChild($"##ChatChild{subject}", new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetContentRegionAvail().Y - fieldHeight));
            if(messageHistory.Messages.Count == 0)
            {
                ImGuiEx.TextWrapped($"This is the beginning of your chat with {subject}");
            }
            bool? isIncoming = null;
            if (messageHistory.LogLoaded && messageHistory.LoadedMessages.Count > 0)
            {
                foreach(var message in messageHistory.LoadedMessages)
                {
                    messageHistory.Messages.Insert(0, message);
                }
                messageHistory.Scroll();
                messageHistory.LoadedMessages.Clear();
            }
            (int year, int day) currentDay = (0, 0);
            foreach (var x in messageHistory.Messages)
            {
                if (x.IsSystem)
                {
                    ImGuiEx.TextWrapped(P.config.ColorGeneric, $"{x.Message}");
                }
                else
                {
                    var time = DateTimeOffset.FromUnixTimeMilliseconds(x.Time).ToLocalTime();
                    if (P.config.PrintDate)
                    {
                        if (!(time.DayOfYear == currentDay.day && time.Year == currentDay.year))
                        {
                            ImGuiEx.Text(P.config.ColorGeneric, $"[{time.ToString(P.config.DateFormat)}]");
                            currentDay = (time.Year, time.DayOfYear);
                        }
                    }
                    var timestamp = time.ToString(P.config.MessageTimestampFormat);
                    if (P.config.IRCStyle)
                    {
                        var messageColor = x.IsIncoming ? P.config.ColorFromMessage : P.config.ColorToMessage;
                        var subjectColor = x.IsIncoming ? P.config.ColorFromTitle : P.config.ColorToTitle;
                        var cur1 = ImGui.GetCursorPos();
                        var wdt = ImGuiEx.Measure(delegate
                        {
                            ImGuiEx.Text(P.config.ColorGeneric, $"{timestamp} ");
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
                        ImGuiEx.TextWrapped($"{spaces} {x.Message}");
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
                                ImGuiEx.TextWrapped(P.config.ColorFromTitle, $"From {x.OverrideName?.Split("@")[0] ?? subjectNoWorld}");
                            }
                            else
                            {
                                ImGuiEx.TextWrapped(P.config.ColorToTitle, $"From {x.OverrideName?.Split("@")[0] ?? me}");
                            }
                        }
                        ImGuiHelpers.ScaledDummy(new Vector2(20f, 1f));
                        ImGui.SameLine(0, 0);
                        ImGuiEx.TextWrapped(x.IsIncoming ? P.config.ColorFromMessage : P.config.ColorToMessage, $"[{timestamp}] {x.Message}");
                        PostMessageFunctions(x);
                    }
                }
            }
            if (messageHistory.DoScroll > 0)
            {
                messageHistory.DoScroll--;
                ImGui.SetScrollHereY();
            } 
            ImGui.EndChild();
            var isCmd = P.config.CommandPassthrough && msg.Trim().StartsWith("/");
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
                SendMessage(subject);
            }
            if (inputCol)
            {
                ImGui.PopStyleColor();
            }
            if (messageHistory.SetFocus)
            {
                this.SetTransparency(false);
                ImGui.SetWindowFocus();
                ImGui.SetKeyboardFocusHere(-1);
                messageHistory.SetFocus = false;
            }
            ImGui.SetWindowFontScale(ImGui.CalcTextSize(" ").Y / ImGuiEx.CalcIconSize(FontAwesomeIcon.ArrowRight).Y);
            ImGui.SameLine(0, 0);
            var icur1 = ImGui.GetCursorPos();
            ImGui.Dummy(Vector2.Zero);
            if (P.config.ButtonSend)
            {
                ImGui.SameLine(0, 2);
                if (isCmd)
                {
                    if (ImGuiEx.IconButton(FontAwesomeIcon.FastForward, "Execute command"))
                    {
                        SendMessage(subject);
                    }
                    ImGuiEx.Tooltip("Execute command");
                }
                else
                {
                    if (ImGuiEx.IconButton(FontAwesomeIcon.ArrowRight, "Send"))
                    {
                        SendMessage(subject);
                    }
                    ImGuiEx.Tooltip("Send message");
                }
            }
            if (P.config.ButtonInvite)
            {
                ImGui.SameLine(0, 2);
                if (ImGuiEx.IconButton(FontAwesomeIcon.DoorOpen, "InviteToParty"))
                {
                    if (Svc.Objects.Any(c => c is PlayerCharacter pc
                        && pc.HomeWorld.Id == this.messageHistory.Player.HomeWorld && pc.Name.ToString() == this.messageHistory.Player.Name))
                    {
                        var result = P.InviteToParty(this.messageHistory.Player, true);
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
                            if (x->Name.ToString() == this.messageHistory.Player.Name && x->HomeWorld == this.messageHistory.Player.HomeWorld)
                            {
                                flSuccess = true;
                                if (x->IsOnline)
                                {
                                    var sameWorld = Svc.ClientState.LocalPlayer.CurrentWorld.Id == x->CurrentWorld;
                                    var result = P.InviteToParty(this.messageHistory.Player, sameWorld, x->ContentId);
                                    if (result != null)
                                    {
                                        Notify.Error(result);
                                    }
                                    else
                                    {
                                        Notify.Info($"Inviting through FrieldList ({(sameWorld ? "same world" : "different world")})");
                                    }
                                }
                                else if (P.CIDlist.ContainsValue(x->ContentId))
                                {
                                    var result = P.InviteToParty(this.messageHistory.Player, true);
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
                        P.InviteToParty(this.messageHistory.Player, true);
                    }
                    if (ImGui.Selectable("Different world"))
                    {
                        if (P.IsFriend(this.messageHistory.Player))
                        {
                            P.InviteToParty(this.messageHistory.Player, false);
                        }
                        else
                        {
                            Notify.Error("This action is only possible for your friends.");
                        }
                    }
                    ImGui.EndPopup();
                }
            }
            
            if (!P.IsFriend(this.messageHistory.Player))
            {
                if (P.config.ButtonFriend)
                {
                    ImGui.SameLine(0, 2);
                    if (ImGuiEx.IconButton(FontAwesomeIcon.Smile, "AddFriend"))
                    {
                        P.gameFunctions.SendFriendRequest(this.messageHistory.Player.Name, (ushort)this.messageHistory.Player.HomeWorld);
                    }
                    ImGuiEx.Tooltip("Add to friend list");
                }
                if (P.config.ButtonBlack)
                {
                    ImGui.SameLine(0, 2);
                    if (ImGuiEx.IconButton(FontAwesomeIcon.Frown, "AddBlacklist"))
                    {
                        P.gameFunctions.AddToBlacklist(this.messageHistory.Player.Name, (ushort)this.messageHistory.Player.HomeWorld);
                    }
                    ImGuiEx.Tooltip("Add to blacklist");
                }
            }
            if (P.config.ButtonLog)
            {
                ImGui.SameLine(0, 2);
                if (ImGuiEx.IconButton(FontAwesomeIcon.Book, "Log"))
                {
                    if (File.Exists(this.messageHistory.LogFile))
                    {
                        ShellStart(this.messageHistory.LogFile);
                    }
                    else
                    {
                        Notify.Error("No log exist yet");
                    }
                }
                ImGuiEx.Tooltip("Open text log");
            }
            ImGui.SameLine(0, 0);
            afterInputWidth = ImGui.GetCursorPosX() - icur1.X;
            ImGui.Dummy(Vector2.Zero);
            var bytes = P.GetLength(subject, msg);
            var fraction = (float)bytes.current / (float)bytes.max;
            ImGui.PushStyleColor(ImGuiCol.PlotHistogram, fraction > 1f?ImGuiColors.DalamudRed:P.config.ColorGeneric);
            ImGui.ProgressBar(fraction, new Vector2(ImGui.GetContentRegionAvail().X, 3f), "");
            ImGui.PopStyleColor();
            fieldHeight = ImGui.GetCursorPosY() - cursor;
            ImGui.SetWindowFontScale(1);
        }

        void PostMessageFunctions(SavedMessage x)
        {
            if (P.config.ClickToOpenLink && ImGui.IsItemHovered())
            {
                foreach (var s in x.Message.Split(" "))
                {
                    if (s.StartsWith("http://", StringComparison.OrdinalIgnoreCase) 
                        || s.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    {
                        ImGui.PushStyleColor(ImGuiCol.Text, P.config.ColorGeneric);
                        ImGuiEx.SetTooltip($"Link found:\n{s}\nClick to open");
                        ImGui.PopStyleColor();
                        ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                        if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
                        {
                            ShellStart(s);
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
                ImGui.PushStyleColor(ImGuiCol.Text, P.config.ColorGeneric);
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
                            ShellStart(s);
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
                            Svc.Chat.Print(new SeStringBuilder().Add(Extensions.GetItemPayload(x.Item.Item, x.Item.IsHQ)).BuiltString);
                        });
                    }
                }
                ImGui.EndPopup();
                ImGui.PopStyleColor();
            }
        }

        public override void PostDraw()
        {
            if (fontPushed)
            {
                ImGui.PopFont();
            }
            if (IsTransparent) ImGui.PopStyleVar();
            if (TitleColored)
            {
                ImGui.PopStyleColor(2);
            }
        }

        void SendMessage(string subject)
        {
            var bytes = P.GetLength(subject, msg);
            var trimmed = msg.Trim();
            if (trimmed.Length == 0)
            {
                Notify.Error("Message is empty!");
            }
            else if (bytes.current > bytes.max)
            {
                Notify.Error("Message is too long!");
            }
            else if (trimmed.StartsWith("/") && P.config.CommandPassthrough)
            {
                if (P.config.AutoTarget &&
                (P.TargetCommands.Any(x => msg.Equals(x, StringComparison.OrdinalIgnoreCase) || msg.StartsWith($"{x} ", StringComparison.OrdinalIgnoreCase)))
                && Svc.Objects.TryGetFirst(x => x is PlayerCharacter pc && pc.GetPlayerName() == subject && x.Struct()->GetIsTargetable(), out var obj))
                {
                    Svc.Targets.SetTarget(obj);
                    Notify.Info($"Targeting {subject}");
                    new TickScheduler(delegate { P.chat.SendMessage(trimmed); }, 100);
                }
                else
                {
                    P.chat.SendMessage(trimmed);
                }
                this.msg = "";
            }
            else
            {
                var error = P.SendDirectMessage(subject, trimmed);
                if (error == null)
                {
                    this.msg = "";
                }
                else
                {
                    Notify.Error(msg);
                }
            }
            if (P.config.RefocusInputAfterSending)
            {
                messageHistory.SetFocus = true;
            }
            else
            {

            }
        }
    }
}
