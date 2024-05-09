using Dalamud.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger.Gui;
public unsafe class PseudoMultilineInput
{
    private string Label = "PML";
    private uint MaxLength = 1500;
		private string Text = "";
		private float TextWidth = 0f;
    internal float? Width = null;
    private int? EnterPress = null;
    internal bool IsInputActive = false;

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
        IsInputActive = ImGui.IsItemActive();
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
}
