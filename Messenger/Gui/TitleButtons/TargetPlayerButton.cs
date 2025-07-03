using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons.GameHelpers;

namespace Messenger.Gui.TitleButtons;
public unsafe class TargetPlayerButton : ChatWindowTitleButton
{
    public TargetPlayerButton(ChatWindow chatWindow) : base(chatWindow)
    {
    }

    public override FontAwesomeIcon Icon { get; } = FontAwesomeIcon.Bullseye;
    public override Vector2 Offset { get; } = new(2, 1);

    public override void DrawTooltip()
    {
        ImGuiEx.SetTooltip($"Target {MessageHistory.HistoryPlayer}. Ctrl+click to focus target them.");
    }

    public override void OnLeftClick()
    {
        if(Svc.Objects.OfType<IPlayerCharacter>().TryGetFirst(x => x.GetNameWithWorld() == MessageHistory.HistoryPlayer.ToString(), out var pl) && pl.IsTargetable)
        {
            Svc.Targets.Target = pl;
        }
    }

    public override bool ShouldDisplay()
    {
        return !MessageHistory.IsEngagement && C.ButtonCharaCard;
    }
}
