using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger.Gui.Settings;
public class TabRecent
{
    public static void Draw()
    {
        ImGuiEx.TextWrapped($"Any time XIM sends a message, it gets saved to this tab. In case your message was not delivered for some reason, you can copy it from here. Last 100 messages are displayed and they are cleared upon game restart.");
        var i = 0;
        foreach(var x in P.MessageCache)
        {
            if(ImGui.Selectable($"{x}##{i}"))
            {
#pragma warning disable CS0618 // Type or member is obsolete
                Copy(x);
#pragma warning restore CS0618 // Type or member is obsolete
            }
            ImGuiEx.Tooltip($"Click to copy:\n\n{x}");
        }
    }
}
