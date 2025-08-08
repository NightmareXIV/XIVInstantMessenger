using Dalamud.Memory;
using ECommons.Interop;
using ECommons.Throttlers;
using Messenger.Services.EmojiLoaderService;
using System.Text.RegularExpressions;
using TerraFX.Interop.Windows;

namespace Messenger.Gui;
public unsafe partial class PseudoMultilineInput
{
    private string Label = "PML";
    private int MaxLength = 15000;
    private string Text = "";
    private float TextWidth = 0f;
    public float? Width = null;
    private int? EnterPress = null;
    public bool IsInputActive = false;
    public bool IsSelectingEmoji = false;
    private int EmojiStartCursor = 0;
    private int EmojiEndCursor = 0;
    private string EmojiSearch = "";
    private int SetFocusAt = -1;
    private bool EmojiListMode = false;
    private int EmojiSelectorRow = -1;
    private int EmojiKeyboardSelectorRow = 0;
    private bool EmojiKeyboardSelecting = false;
    private int FrameCharsProcessed = 0;
    private bool CloseEmojiPopup = false;
    private bool DoCursorLock = false;
    private int CursorLock = -1;
    private Vector2 MouseEmojiLock = Vector2.Zero;
    private int StoredCursorPos = 0;

    private int MaxLines => C.PMLMaxLines;
    public bool IsMultiline => C.PMLEnable;

    public string SinglelineText
    {
        get => Text.Replace("\r", "").Replace("\n", " ");
        set => Text = value;
    }

    public bool EnterWasPressed()
    {
        return EnterPress == ImGui.GetFrameCount();
    }

    public void OpenEmojiSelector()
    {
        EmojiStartCursor = -1;
        IsSelectingEmoji = true;
        EmojiListMode = false;
        ImGui.OpenPopup("XIMEmojiSelect");
    }

    public void Draw()
    {
        if(IsMultiline)
        {
            DrawMultiline();
        }
        else
        {
            DrawSingleline();
        }
    }

    public void DrawSingleline()
    {
        EnterPress = null;
        if(Text.Contains('\n'))
        {
            Text = Text.Replace("\n", " ");
        }
        var width = Width ?? ImGui.GetContentRegionAvail().X;
        ImGui.SetNextItemWidth(width);
        var ret = ImGui.InputText($"##{Label}", ref Text, MaxLength, ImGuiInputTextFlags.EnterReturnsTrue);
        IsInputActive = ImGui.IsItemActive();
        if(ret)
        {
            EnterPress = ImGui.GetFrameCount();
        }
    }

    public void DrawMultiline()
    {
        FrameCharsProcessed = 0;
        EnterPress = null;
        var width = Width ?? ImGui.GetContentRegionAvail().X;
        TextWidth = width - ImGui.GetStyle().FramePadding.X * 2;
        var newlines = Text.Split("\n").Length;
        var cnt = Math.Max(1, Math.Min(newlines, MaxLines));
        var lheight = ImGui.CalcTextSize("A").Y;
        ImGui.InputTextMultiline($"##{Label}", ref Text, MaxLength, new(width, lheight * cnt + ImGui.GetStyle().FramePadding.X * 2), ImGuiInputTextFlags.NoHorizontalScroll | ImGuiInputTextFlags.CallbackAlways | ImGuiInputTextFlags.CallbackCharFilter | ImGuiInputTextFlags.NoUndoRedo, Callback);
        if(SetFocusAt > -1) ImGui.SetKeyboardFocusHere(-1);
        IsInputActive = ImGui.IsItemActive();
        DrawEmojiPopup();
    }

    private int Callback(ref ImGuiInputTextCallbackData dataPtr)
    {
        fixed(ImGuiInputTextCallbackData* data = &dataPtr)
        {
            if(C.EnableEmojiPicker)
            {
                if(ImGui.IsKeyDown(ImGuiKey.UpArrow) || ImGui.IsKeyDown(ImGuiKey.DownArrow))
                {
                    data->CursorPos = StoredCursorPos;
                    EmojiKeyboardSelecting = true;
                    POINT point;
                    Utils.GetCursorPos(&point);
                    MouseEmojiLock = point.AsVector2();
                    if(ImGui.IsKeyPressed(ImGuiKey.UpArrow))
                    {
                        EmojiKeyboardSelectorRow--;
                    }
                    if(ImGui.IsKeyPressed(ImGuiKey.DownArrow))
                    {
                        EmojiKeyboardSelectorRow++;
                    }
                }
                else
                {
                    EzThrottler.Reset("EmojiSel");
                    StoredCursorPos = data->CursorPos;
                }
            }
            if(data->EventFlag == ImGuiInputTextFlags.CallbackCharFilter)
            {
                FrameCharsProcessed++;
                if(FrameCharsProcessed > 1) EnterPress = null;
                if(data->EventChar == '\n')
                {
                    if(FrameCharsProcessed == 1)
                    {
                        EnterPress = ImGui.GetFrameCount();
                        return 1;
                    }
                }
                return 0;
            }
            if(data->EventFlag == ImGuiInputTextFlags.CallbackAlways)
            {
                if(SetFocusAt > -1)
                {
                    if(SetFocusAt <= data->BufTextLen)
                    {
                        data->CursorPos = SetFocusAt;
                    }
                    SetFocusAt = -1;
                }
                if(DoCursorLock)
                {
                    if(CursorLock != -1 && GenericHelpers.IsAnyKeyPressed([LimitedKeys.Up, LimitedKeys.Down]))
                    {
                        data->CursorPos = CursorLock;
                    }
                    else
                    {
                        CursorLock = data->CursorPos;
                    }
                }
                else
                {
                    if(CursorLock != -1)
                    {
                        CursorLock = -1;
                    }
                }
                EmojiEndCursor = data->CursorPos;
                IsSelectingEmoji = false;
                if(C.EnableEmoji && C.EnableEmojiPicker)
                {
                    for(var i = 0; i < EmojiEndCursor; i++)
                    {
                        if(data->Buf[i] == ':')
                        {
                            IsSelectingEmoji = !IsSelectingEmoji;
                            EmojiStartCursor = i;
                        }
                        else if(!EmojiWhitelistedSymbols().IsMatch(((char)data->Buf[i]).ToString()))
                        {
                            IsSelectingEmoji = false;
                        }
                    }
                    if(IsSelectingEmoji)
                    {
                        if(EmojiEndCursor - EmojiStartCursor < 3) IsSelectingEmoji = false;
                    }
                    if(IsSelectingEmoji)
                    {
                        for(var i = EmojiEndCursor; i < data->BufTextLen; i++)
                        {
                            if(data->Buf[i] == ':')
                            {
                                IsSelectingEmoji = false;
                            }
                            if(!EmojiWhitelistedSymbols().IsMatch(((char)data->Buf[i]).ToString())) break;
                        }
                    }
                    if(IsSelectingEmoji)
                    {
                        EmojiSearch = MemoryHelper.ReadString(((nint)data->Buf) + EmojiStartCursor, EmojiEndCursor - EmojiStartCursor)[1..];
                        EmojiListMode = true;
                    }
                }


                var space = ImGui.CalcTextSize(" ").X;
                var numNewlines = 0;
                void Process(ImGuiInputTextCallbackData* data)
                {
                    for(var i = 0; i < data->BufTextLen; i++)
                    {
                        var symbol = data->Buf[i];
                        if(symbol == '\n')
                        {
                            data->Buf[i] = (byte)' ';
                        }
                    }
                    var cursor = 0;
                    var width = TextWidth;
                    var prevSpaceIndex = -1;
                    while(cursor < data->BufTextLen)
                    {
                        var word = ReadUntilSpace(data->Buf, data->BufTextLen, cursor, out var spaceIndex);
                        var wordWidth = ImGui.CalcTextSize(word).X;
                        {
                            width -= wordWidth + (spaceIndex == -1 ? 0 : space);
                            if(width < 0 && prevSpaceIndex > -1)
                            {
                                data->Buf[prevSpaceIndex] = (byte)'\n';
                                numNewlines++;
                                width = TextWidth - wordWidth - (spaceIndex == -1 ? 0 : space);
                            }
                        }
                        cursor = spaceIndex == -1 ? data->BufTextLen : spaceIndex + 1;
                        prevSpaceIndex = spaceIndex;
                    }
                }
                var old = MemoryHelper.ReadRaw((nint)data->Buf, data->BufTextLen);
                Process(data);
                if(numNewlines >= MaxLines)
                {
                    TextWidth -= ImGui.GetStyle().ScrollbarSize;
                    Process(data);
                }
                if(!MemoryHelper.ReadRaw((nint)data->Buf, data->BufTextLen).SequenceEqual(old))
                {
                    data->BufDirty = 1;
                }
                return 0;
            }
            return 0;
        }
    }

    private void DrawEmojiPopup()
    {
        DoCursorLock = false;
        var emojiSize = 32f;
        if(IsInputActive)
        {
            if(IsSelectingEmoji && !ImGui.IsPopupOpen("XIMEmojiSelect"))
            {
                ImGui.OpenPopup("XIMEmojiSelect");
            }
        }
        if(ImGui.IsPopupOpen("XIMEmojiSelect"))
        {
            ImGui.SetNextWindowPos(new(ImGui.GetWindowPos().X, ImGui.GetCursorScreenPos().Y));
            ImGui.SetNextWindowSizeConstraints(new(ImGui.GetWindowSize().X, 100), new(ImGui.GetWindowSize().X, 300));
        }
        else
        {
            IsSelectingEmoji = false;
        }
        if(ImGui.BeginPopup("XIMEmojiSelect", ImGuiWindowFlags.NoFocusOnAppearing))
        {
            if(ImGui.IsWindowAppearing())
            {
                CImGui.igBringWindowToDisplayFront(CImGui.igGetCurrentWindow());
            }
            if(CloseEmojiPopup)
            {
                CloseEmojiPopup = false;
                ImGui.SetWindowFocus(ImU8String.Empty);
            }
            if(!IsSelectingEmoji) ImGui.CloseCurrentPopup();
            if(!EmojiListMode)
            {
                EmojiKeyboardSelecting = false;
                var cnt = 0;
                ImGuiEx.InputWithRightButtonsArea(() =>
                {
                    ImGui.InputTextWithHint("##emjfltr", "Search...", ref EmojiSearch, 50);
                }, () =>
                {
                    if(ImGuiEx.IconButton(FontAwesomeIcon.SearchPlus, enabled: EmojiSearch != "" && !S.EmojiLoader.DownloaderTaskRunning))
                    {
                        S.EmojiLoader.Search(EmojiSearch.ToLower());
                    }
                    ImGuiEx.Tooltip($"Search for this emoji on BetterTTV");
                });
                var fav = S.EmojiLoader.Emoji.Where(x => C.FavoriteEmoji.Contains(x.Key));
                var all = S.EmojiLoader.Emoji.Where(x => !C.FavoriteEmoji.Contains(x.Key));
                if(fav.Any())
                {
                    DrawEmojiSet(fav);
                    ImGui.SameLine();
                    ImGui.NewLine();
                    ImGui.Separator();
                }
                DrawEmojiSet(all);
                void DrawEmojiSet(IEnumerable<KeyValuePair<string, ImageFile>> emojiSet)
                {
                    var internalCnt = 0;
                    foreach(var em in emojiSet)
                    {
                        if(EmojiSearch != "" && !em.Key.Contains(EmojiSearch, StringComparison.OrdinalIgnoreCase)) continue;
                        cnt++;
                        internalCnt++;
                        if(em.Value.GetTextureWrap() != null)
                        {
                            ImGui.Image(em.Value.GetTextureWrap().Handle, new(emojiSize));
                            ImGuiEx.Tooltip($":{em.Key}:");
                            HandleEmojiRightClick(em, -1);
                        }
                        else
                        {
                            ImGui.Dummy(new(emojiSize));
                        }
                        ImGui.SameLine();
                        if(ImGui.GetContentRegionAvail().X < emojiSize)
                        {
                            ImGui.NewLine();
                        }
                    }
                }
                if(cnt == 0)
                {
                    ImGuiEx.Text($"No emoji found...");
                }
            }
            else
            {
                if(ImGui.IsWindowAppearing())
                {
                    EmojiKeyboardSelectorRow = -1;
                    EmojiKeyboardSelecting = false;
                }
                POINT point;
                Utils.GetCursorPos(&point);
                if(MouseEmojiLock != point.AsVector2() && ImGui.IsWindowHovered())
                {
                    MouseEmojiLock = point.AsVector2();
                    EmojiKeyboardSelectorRow = -1;
                    EmojiKeyboardSelecting = false;
                }
                DoCursorLock = true;
                var index = 0;
                var fav = S.EmojiLoader.Emoji.Where(x => C.FavoriteEmoji.Contains(x.Key));
                var all = S.EmojiLoader.Emoji.Where(x => !C.FavoriteEmoji.Contains(x.Key));
                if(fav.Any())
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.ParsedGold);
                    var num = DrawEmojiSet(fav, "fav");
                    ImGui.PopStyleColor();
                    if(num > 0)
                    {
                        ImGui.SameLine();
                        ImGui.NewLine();
                    }
                }
                DrawEmojiSet(all, "all");
                int DrawEmojiSet(IEnumerable<KeyValuePair<string, ImageFile>> emojiSet, string id)
                {
                    var num = 0;
                    List<Action> PostDrawAction = [];
                    if(ImGui.BeginTable($"EmojiTable{id}", 2, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.BordersInnerH))
                    {
                        ImGui.TableSetupColumn("image");
                        ImGui.TableSetupColumn("emojiText", ImGuiTableColumnFlags.WidthStretch);
                        foreach(var em in emojiSet)
                        {
                            if(EmojiSearch != "" && !em.Key.Contains(EmojiSearch, StringComparison.OrdinalIgnoreCase)) continue;
                            num++;
                            ImGui.TableNextRow();
                            if(index == EmojiSelectorRow)
                            {
                                ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGuiEx.Vector4FromRGBA(0xffffff33).ToUint());
                                ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg1, ImGuiEx.Vector4FromRGBA(0xffffff33).ToUint());
                                EmojiSelectorRow = -1;
                            }
                            ImGui.TableNextColumn();

                            var cur = ImGui.GetCursorPos();
                            var index2 = index;
                            PostDrawAction.Add(() =>
                            {
                                ImGui.SetCursorPos(cur);
                                ImGui.Dummy(new(ImGuiEx.GetWindowContentRegionWidth(), emojiSize));
                                HandleEmojiRightClick(em, index2);
                            });

                            if(em.Value.GetTextureWrap() != null)
                            {
                                ImGui.Image(em.Value.GetTextureWrap().Handle, new(emojiSize));
                            }
                            else
                            {
                                ImGui.Dummy(new(emojiSize));
                            }
                            ImGui.TableNextColumn();
                            var pad = (emojiSize - ImGui.CalcTextSize($":{em.Key}:").Y) / 2f;
                            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + pad);
                            ImGuiEx.Text($":{em.Key}:");
                            index++;
                        }
                        ImGui.EndTable();
                        PostDrawAction.Each(x => x());
                    }
                    return num;
                }
                if(EmojiKeyboardSelecting)
                {
                    if(EmojiKeyboardSelectorRow < 0) EmojiKeyboardSelectorRow = index - 1;
                    if(EmojiKeyboardSelectorRow >= index) EmojiKeyboardSelectorRow = 0;
                }
            }
            ImGui.EndPopup();
        }
    }

    private void HandleEmojiRightClick(KeyValuePair<string, ImageFile> em, int index)
    {
        if(EmojiKeyboardSelecting)
        {
            if(EmojiKeyboardSelectorRow == index)
            {
                EmojiSelectorRow = index;
                ImGui.SetScrollHereY();
                if(IsKeyPressed(LimitedKeys.Tab))
                {
                    ImGui.SetWindowFocus(ImU8String.Empty);
                    Insert(em.Key);
                }
            }
        }
        else
        {
            if(ImGui.IsItemHovered())
            {
                EmojiSelectorRow = index;
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                if(ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                {
                    Insert(em.Key);
                }
                if(ImGui.IsMouseReleased(ImGuiMouseButton.Right))
                {
                    ImGui.OpenPopup($"EmojiContext{em.Key}");
                }
            }
        }
        if(ImGui.BeginPopup($"EmojiContext{em.Key}"))
        {
            if(ImGui.Selectable("Insert as sticker"))
            {
                Insert("s-" + em.Key);
            }
            if(ImGui.Selectable(!C.FavoriteEmoji.Contains(em.Key) ? "Add to Favorites" : "Remove from Favorites", false, ImGuiSelectableFlags.DontClosePopups))
            {
                C.FavoriteEmoji.Toggle(em.Key);
                CloseEmojiPopup = true;
                CImGui.igClosePopupToLevel(1, false);
            }
            ImGui.EndPopup();
        }
    }

    private void Insert(string emText)
    {
        if(EmojiStartCursor != -1)
        {
            SinglelineText = SinglelineText[0..EmojiStartCursor] + $":{emText}: " + SinglelineText[EmojiEndCursor..];
            SetFocusAt = EmojiStartCursor + emText.Length + 3;
        }
        else
        {
            SinglelineText += $":{emText}: ";
            SetFocusAt = Text.Length;
        }
    }

    private static string ReadUntilSpace(byte* array, int length, int start, out int spaceIndex)
    {
        for(var i = start; i < length; i++)
        {
            if(array[i] == ' ')
            {
                spaceIndex = i;
                return MemoryHelper.ReadString(((nint)array) + start, i - start);
            }
        }
        spaceIndex = -1;
        return MemoryHelper.ReadString(((nint)array) + start, length - start);
    }

    [GeneratedRegex(@"^[a-z0-9_]{1}$", RegexOptions.IgnoreCase)]
    private static partial Regex EmojiWhitelistedSymbols();
}
