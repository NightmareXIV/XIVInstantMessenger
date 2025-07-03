using Dalamud.Game.Text.SeStringHandling;
using ECommons.UIHelpers;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Messenger;
public sealed unsafe class ReaderAddonLookingForGroupDetail : AtkReader
{
    public ReaderAddonLookingForGroupDetail(AtkUnitBase* UnitBase, int BeginOffset = 0) : base(UnitBase, BeginOffset)
    {
    }

    public ReaderAddonLookingForGroupDetail(nint UnitBasePtr, int BeginOffset = 0) : base(UnitBasePtr, BeginOffset)
    {
    }

    public SeString Recruiter => ReadSeString(11);
    public SeString RecruiterWorld => ReadSeString(13);
}