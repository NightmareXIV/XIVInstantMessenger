using Dalamud.Memory;
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
    internal float? Width = null;
    private int? EnterPress = null;
    internal bool IsInputActive = false;
    private bool IsSelectingEmoji = false;
    private int EmojiStartCursor = 0;
    private int EmojiEndCursor = 0;
    private string EmojiSearch = "";
    private int SetFocusAt = -1;

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
				if(data->EventFlag == ImGuiInputTextFlags.CallbackCharFilter)
        {
            if(data->EventChar == '\n')
            {
								EnterPress = ImGui.GetFrameCount();
								return 1;
            }
            return 0;
        }
        if(SetFocusAt > -1)
        {
            if(SetFocusAt <= data->BufTextLen)
            {
                data->CursorPos = SetFocusAt;
            }
            SetFocusAt = -1;
        }
        EmojiEndCursor = data->CursorPos;
				IsSelectingEmoji = false;
        for (int i = 0; i < EmojiEndCursor; i++)
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
						for (int i = EmojiEndCursor; i < data->BufTextLen; i++)
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
				}


				var space = ImGui.CalcTextSize(" ").X;
				int numNewlines = 0;
				void Process()
        {
            for (int i = 0; i < data->BufTextLen; i++)
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
        if(numNewlines >= MaxLines)
        {
            TextWidth -= ImGui.GetStyle().ScrollbarSize;
            Process();
				}
        return 0;
    }

    private void DrawEmojiPopup()
    {
        if (IsInputActive)
        {
            if (IsSelectingEmoji && !ImGui.IsPopupOpen("XIMEmojiSelect"))
            {
                ImGui.OpenPopup("XIMEmojiSelect");
            }
				}
				if (ImGui.IsPopupOpen("XIMEmojiSelect"))
				{
						ImGuiHelpers.SetNextWindowPosRelativeMainViewport(ImGui.GetCursorScreenPos());
						ImGui.SetNextWindowSizeConstraints(new(100, 100), new(500, 300));
				}
				if (ImGui.BeginPopup("XIMEmojiSelect", ImGuiWindowFlags.NoFocusOnAppearing))
        {
            if (ImGui.IsWindowAppearing())
            {
                CImGui.igBringWindowToDisplayFront(CImGui.igGetCurrentWindow());
            }
            if (!IsSelectingEmoji) ImGui.CloseCurrentPopup();
            var cnt = 0;
            ImGuiEx.InputWithRightButtonsArea(() =>
            {
                ImGui.InputTextWithHint("##emjfltr", "Search...", ref EmojiSearch, 50);
            }, () =>
            {
                if (ImGuiEx.IconButton(FontAwesomeIcon.SearchPlus, enabled:EmojiSearch != ""))
                {
                    
                }
            });
            foreach(var em in S.EmojiLoader.Emoji)
            {
                if (EmojiSearch != "" && !em.Key.Contains(EmojiSearch, StringComparison.OrdinalIgnoreCase)) continue;
                cnt++;
                if(em.Value.GetTextureWrap() != null)
                {
                    ImGui.Image(em.Value.GetTextureWrap().ImGuiHandle, new(32));
                    ImGuiEx.Tooltip($":{em.Key}:");
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                        if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                        {
                            SinglelineText = SinglelineText[0..EmojiStartCursor] + $":{em.Key}: " + SinglelineText[EmojiEndCursor..];
                            SetFocusAt = EmojiStartCursor + em.Key.Length + 3;
                        }
                    }
								}
                else
                {
                    ImGui.Dummy(new(32));
                }
                ImGui.SameLine();
                if(cnt % 12 == 0)
                {
                    ImGui.NewLine();
                }
            }
            if(cnt == 0)
            {
                ImGuiEx.Text($"No emoji found...");
            }
            ImGui.EndPopup();
        }
    }
    
    private static string ReadUntilSpace(byte* array, int length, int start, out int spaceIndex)
    {
        for (int i = start; i < length; i++)
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
