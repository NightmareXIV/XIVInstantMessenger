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
            if(P.config.DefaultChannelCustomization == null)
            {
                P.config.DefaultChannelCustomization = new ChannelCustomization();
                foreach(var x in  P.config.DefaultChannelCustomization.GetType().GetFields())
                {
                    PluginLog.Information($"Setting {x.Name} to {P.config.GetFoP(x.Name)}");
                    P.config.DefaultChannelCustomization.SetFoP(x.Name, P.config.GetFoP(x.Name));
                }
            }
        }
    }
}
