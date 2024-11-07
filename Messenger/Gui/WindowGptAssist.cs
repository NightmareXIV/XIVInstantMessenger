using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger.Gui;
public class WindowGptAssist : Window
{
    public string[] Prompts = [];
    public WindowGptAssist() : base("GPT4All Assistant")
    {
    }

    public List<string> Parts = [];
    public string SuggestedMessage = null;
    public string CurrentMessage = null;

    public override void Draw()
    {
        if(ImGui.Button("Analyze clipboard"))
        {
            try
            {
                var text = Paste();
                if(text != null)
                {
                    CurrentMessage = text.Replace("\n", " ");
                }
            }
            catch(Exception ex)
            {
                ex.Log();
            }
        }
        ImGui.SameLine();
        ImGuiEx.SetNextItemFullWidth();
        if(ImGui.BeginCombo("##sel", "Select part", ImGuiComboFlags.HeightLarge))
        {
            foreach(var item in Parts)
            {
                if(ImGui.Selectable(item))
                {
                    CurrentMessage = item;
                }
            }
            ImGui.EndCombo();
        }
        if(SuggestedMessage != null)
        {
            ImGuiEx.TextWrapped($"Suggested message:\n{SuggestedMessage}");
            if(ImGui.Button("Analyze it")) CurrentMessage = SuggestedMessage;
            ImGui.Separator();
        }
        if(CurrentMessage != null)
        {
            ImGuiEx.Text($"Current message:\n{CurrentMessage}");
            ImGui.Separator();
            foreach(var x in Prompts)
            {
                ImGuiEx.Text(ImGuiColors.DalamudGrey, x);
                var res = S.GPT4All.GetResponse($"{x}\n{CurrentMessage}");
                if(res != null)
                {
                    ImGuiEx.TextWrapped($"{res}");
                }
                else
                {
                    ImGuiEx.Text(EColor.Yellow, "Loading...");
                }
                ImGui.Separator();
            }
        }
        else
        {
            ImGuiEx.Text("No current message");
        }
    }
}
