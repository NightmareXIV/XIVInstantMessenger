using Dalamud.Memory;
using Messenger.Services.EmojiLoaderService;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Messenger.Gui;
public unsafe partial class PseudoMultilineInput
{
    private string Label = "PML";
    private uint MaxLength = 1500;
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
    private int FrameCharsProcessed = 0;
    private bool CloseEmojiPopup = false;

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
        this.EmojiListMode = false;
        ImGui.OpenPopup("XIMEmojiSelect");
    }

    public void Draw()
    {
        if (IsMultiline)
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
        if (Text.Contains('\n'))
        {
            Text = Text.Replace("\n", " ");
        }
        var width = Width ?? ImGui.GetContentRegionAvail().X;
        ImGui.SetNextItemWidth(width);
        var ret = ImGui.InputText($"##{Label}", ref Text, MaxLength, ImGuiInputTextFlags.EnterReturnsTrue);
        IsInputActive = ImGui.IsItemActive();
        if (ret)
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
        if (SetFocusAt > -1) ImGui.SetKeyboardFocusHere(-1);
        IsInputActive = ImGui.IsItemActive();
        DrawEmojiPopup();
    }

    private int Callback(ImGuiInputTextCallbackData* data)
    {
        if (data->EventFlag == ImGuiInputTextFlags.CallbackCharFilter)
        {
            FrameCharsProcessed++;
            if (FrameCharsProcessed > 1) EnterPress = null;
            if (data->EventChar == '\n')
            {
                if (FrameCharsProcessed == 1)
                {
                    EnterPress = ImGui.GetFrameCount();
                    return 1;
                }
            }
            return 0;
        }
        if (data->EventFlag == ImGuiInputTextFlags.CallbackAlways)
        {
            if (SetFocusAt > -1)
            {
                if (SetFocusAt <= data->BufTextLen)
                {
                    data->CursorPos = SetFocusAt;
                }
                SetFocusAt = -1;
            }
            EmojiEndCursor = data->CursorPos;
            IsSelectingEmoji = false;
            if (C.EnableEmoji && C.EnableEmojiPicker)
            {
                for (var i = 0; i < EmojiEndCursor; i++)
                {
                    if (data->Buf[i] == ':')
                    {
                        IsSelectingEmoji = !IsSelectingEmoji;
                        EmojiStartCursor = i;
                    }
                    else if (!EmojiWhitelistedSymbols().IsMatch(((char)data->Buf[i]).ToString()))
                    {
                        IsSelectingEmoji = false;
                    }
                }
                if (IsSelectingEmoji)
                {
                    if (EmojiEndCursor - EmojiStartCursor < 3) IsSelectingEmoji = false;
                }
                if (IsSelectingEmoji)
                {
                    for (var i = EmojiEndCursor; i < data->BufTextLen; i++)
                    {
                        if (data->Buf[i] == ':')
                        {
                            IsSelectingEmoji = false;
                        }
                        if (!EmojiWhitelistedSymbols().IsMatch(((char)data->Buf[i]).ToString())) break;
                    }
                }
                if (IsSelectingEmoji)
                {
                    EmojiSearch = MemoryHelper.ReadString(((nint)data->Buf) + EmojiStartCursor, EmojiEndCursor - EmojiStartCursor)[1..];
                    this.EmojiListMode = true;
                }
            }


            var space = ImGui.CalcTextSize(" ").X;
            var numNewlines = 0;
            void Process()
            {
                for (var i = 0; i < data->BufTextLen; i++)
                {
                    var symbol = data->Buf[i];
                    if (symbol == '\n')
                    {
                        data->Buf[i] = (byte)' ';
                    }
                }
                var cursor = 0;
                var width = TextWidth;
                var prevSpaceIndex = -1;
                while (cursor < data->BufTextLen)
                {
                    var word = ReadUntilSpace(data->Buf, data->BufTextLen, cursor, out var spaceIndex);
                    var wordWidth = ImGui.CalcTextSize(word).X;
                    {
                        width -= wordWidth + (spaceIndex == -1 ? 0 : space);
                        if (width < 0 && prevSpaceIndex > -1)
                        {
                            data->Buf[prevSpaceIndex] = (byte)'\n';
                            numNewlines++;
                            width = TextWidth - wordWidth - (spaceIndex == -1 ? 0 : space);
                        }
                    }
                    cursor = spaceIndex == -1 ? data->BufTextLen : spaceIndex + 1;
                    prevSpaceIndex = spaceIndex;
                }
                data->BufDirty = 1;
            }
            Process();
            if (numNewlines >= MaxLines)
            {
                TextWidth -= ImGui.GetStyle().ScrollbarSize;
                Process();
            }
            return 0;
        }
        return 0;
    }

    private void DrawEmojiPopup()
    {
        var emojiSize = 32f;
        if (IsInputActive)
        {
            if (IsSelectingEmoji && !ImGui.IsPopupOpen("XIMEmojiSelect"))
            {
                ImGui.OpenPopup("XIMEmojiSelect");
            }
        }
        if (ImGui.IsPopupOpen("XIMEmojiSelect"))
        {
            ImGui.SetNextWindowPos(new(ImGui.GetWindowPos().X, ImGui.GetCursorScreenPos().Y));
            ImGui.SetNextWindowSizeConstraints(new(ImGui.GetWindowSize().X, 100), new(ImGui.GetWindowSize().X, 300));
        }
        else
        {
            IsSelectingEmoji = false;
        }
        if (ImGui.BeginPopup("XIMEmojiSelect", ImGuiWindowFlags.NoFocusOnAppearing))
        {
            if (ImGui.IsWindowAppearing())
            {
                CImGui.igBringWindowToDisplayFront(CImGui.igGetCurrentWindow());
            }
            if (CloseEmojiPopup)
            {
                CloseEmojiPopup= false;
                ImGui.SetWindowFocus();
            }
            if (!IsSelectingEmoji) ImGui.CloseCurrentPopup();
            if (!EmojiListMode)
            {
                var cnt = 0;
                ImGuiEx.InputWithRightButtonsArea(() =>
                {
                    ImGui.InputTextWithHint("##emjfltr", "Search...", ref EmojiSearch, 50);
                }, () =>
                {
                    if (ImGuiEx.IconButton(FontAwesomeIcon.SearchPlus, enabled: EmojiSearch != "" && !S.EmojiLoader.DownloaderTaskRunning))
                    {
                        S.EmojiLoader.Search(EmojiSearch.ToLower());
                    }
                    ImGuiEx.Tooltip($"Search for this emoji on BetterTTV");
                });
                var fav = S.EmojiLoader.Emoji.Where(x => C.FavoriteEmoji.Contains(x.Key));
                var all = S.EmojiLoader.Emoji.Where(x => !C.FavoriteEmoji.Contains(x.Key));
                if (fav.Any())
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
                    foreach (var em in emojiSet)
                    {
                        if (EmojiSearch != "" && !em.Key.Contains(EmojiSearch, StringComparison.OrdinalIgnoreCase)) continue;
                        cnt++;
                        internalCnt++;
                        if (em.Value.GetTextureWrap() != null)
                        {
                            ImGui.Image(em.Value.GetTextureWrap().ImGuiHandle, new(emojiSize));
                            ImGuiEx.Tooltip($":{em.Key}:");
                            HandleEmojiRightClick(em, -1);
                        }
                        else
                        {
                            ImGui.Dummy(new(emojiSize));
                        }
                        ImGui.SameLine();
                        if (ImGui.GetContentRegionAvail().X < emojiSize)
                        {
                            ImGui.NewLine();
                        }
                    }
                }
                if (cnt == 0)
                {
                    ImGuiEx.Text($"No emoji found...");
                }
            }
            else
            {
                var index = 0;
                var fav = S.EmojiLoader.Emoji.Where(x => C.FavoriteEmoji.Contains(x.Key));
                var all = S.EmojiLoader.Emoji.Where(x => !C.FavoriteEmoji.Contains(x.Key));
                if (fav.Any())
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.ParsedGold);
                    DrawEmojiSet(fav, "fav");
                    ImGui.PopStyleColor();
                    ImGui.SameLine();
                    ImGui.NewLine();
                }
                DrawEmojiSet(all, "all");
                void DrawEmojiSet(IEnumerable<KeyValuePair<string, ImageFile>> emojiSet, string id)
                {
                    List<Action> PostDrawAction = [];
                    if(ImGui.BeginTable($"EmojiTable{id}", 2, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.BordersInnerH))
                    {
                        ImGui.TableSetupColumn("image");
                        ImGui.TableSetupColumn("emojiText", ImGuiTableColumnFlags.WidthStretch);
                        foreach (var em in emojiSet)
                        {
                            if (EmojiSearch != "" && !em.Key.Contains(EmojiSearch, StringComparison.OrdinalIgnoreCase)) continue;
                            ImGui.TableNextRow();
                            if(index == this.EmojiSelectorRow)
                            {
                                ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGuiEx.Vector4FromRGBA(0xffffff33).ToUint());
                                ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg1, ImGuiEx.Vector4FromRGBA(0xffffff33).ToUint());
                                this.EmojiSelectorRow = -1;
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

                            if (em.Value.GetTextureWrap() != null)
                            {
                                ImGui.Image(em.Value.GetTextureWrap().ImGuiHandle, new(emojiSize));
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
                }
            }
            ImGui.EndPopup();
        }
    }

    void HandleEmojiRightClick(KeyValuePair<string, ImageFile> em, int index)
    {
        if (ImGui.IsItemHovered())
        {
            EmojiSelectorRow = index;
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
            {
                Insert(em.Key);
            }
            if (ImGui.IsMouseReleased(ImGuiMouseButton.Right))
            {
                ImGui.OpenPopup($"EmojiContext{em.Key}");
            }
        }
        if (ImGui.BeginPopup($"EmojiContext{em.Key}"))
        {
            if (ImGui.Selectable("Insert as sticker"))
            {
                Insert("s-" + em.Key);
            }
            if (ImGui.Selectable(!C.FavoriteEmoji.Contains(em.Key) ? "Add to Favorites" : "Remove from Favorites", false, ImGuiSelectableFlags.DontClosePopups))
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
        if (EmojiStartCursor != -1)
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
        for (var i = start; i < length; i++)
        {
            if (array[i] == ' ')
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
