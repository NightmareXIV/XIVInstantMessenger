using Dalamud.Game.Text.SeStringHandling;
using ECommons.UIHelpers;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger;
public unsafe sealed class ReaderAddonLookingForGroupDetail : AtkReader
{
    public ReaderAddonLookingForGroupDetail(AtkUnitBase* UnitBase, int BeginOffset = 0) : base(UnitBase, BeginOffset)
    {
    }

    public ReaderAddonLookingForGroupDetail(nint UnitBasePtr, int BeginOffset = 0) : base(UnitBasePtr, BeginOffset)
    {
    }

    public SeString Recruiter => this.ReadSeString(11);
    public SeString RecruiterWorld => this.ReadSeString(13);
}