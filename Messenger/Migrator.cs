using ECommons.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger
{
    internal static class Migrator
    {
        internal static void MigrateConfiguration()
        {
            Safe(MigrateConfiguration1);
        }

        private static void MigrateConfiguration1()
        {
            if(C.DefaultChannelCustomization == null)
            {
                C.DefaultChannelCustomization = new ChannelCustomization();
                foreach(var x in  C.DefaultChannelCustomization.GetType().GetFields())
                {
                    PluginLog.Information($"Setting {x.Name} to {C.GetFoP(x.Name)}");
                    C.DefaultChannelCustomization.SetFoP(x.Name, C.GetFoP(x.Name));
                }
            }
        }
    }
}
