using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger.Gui
{
    internal class TabSystem : Window
    {
        public TabSystem() : base("XIV Instant Messenger")
        {
            this.RespectCloseHotkey = false;
            this.IsOpen = true;
        }

        public override bool DrawConditions()
        {
            return P.config.Tabs && P.wsChats.Windows.Any(x => x.IsOpen);
        }

        public override void OnClose()
        {
            foreach(var w in P.wsChats.Windows)
            {
                w.IsOpen = false;
            }
            this.IsOpen = true;
        }

        public override void Draw()
        {
            if (ImGui.BeginTabBar("##MessengerTabs", ImGuiTabBarFlags.FittingPolicyScroll))
            {
                foreach (var window in P.wsChats.Windows)
                {
                    var isOpen = window.IsOpen;
                    if (window is ChatWindow w && isOpen && ImGui.BeginTabItem(w.messageHistory.Player.Name+$"###{w.WindowName}", ref isOpen))
                    {
                        w.SetPosition = false;
                        w.Draw();
                        ImGui.EndTabItem();
                    }
                    window.IsOpen = isOpen;
                }
                ImGui.EndTabBar();
            }
        }
    }
}
