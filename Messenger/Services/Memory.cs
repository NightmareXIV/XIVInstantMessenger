using Dalamud.Utility.Signatures;
using ECommons.EzHookManager;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger.Services;
public unsafe class Memory
{
    private Memory()
    {
        SignatureHelper.Initialise(this);
        EzSignatureHelper.Initialize(this);
    }
}
