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

    public string Recruiter => this.ReadString(11);
    public string RecruiterWorld => this.ReadString(13);
}