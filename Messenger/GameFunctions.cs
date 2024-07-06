/*
 This file contains source code:
    - Authored by Anna Clemens from https://git.annaclemens.io/ascclemens/ChatTwo/src/branch/main/ChatTwo which is licensed under EUPL license
    - Authored by Anna Clemens from (https://git.annaclemens.io/ascclemens/XivCommon/src/branch/main) which is distributed under MIT license
    - Authored by Ottermandias from (https://github.com/Ottermandias/GatherBuddy) which is distributed under APACHE license
 */
using Dalamud.Hooking;
using Dalamud.Memory;
using Dalamud.Utility.Signatures;
using ECommons.Automation;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Lumina.Excel.GeneratedSheets;

#pragma warning disable CS8632,CS0649
namespace Messenger;

internal unsafe class GameFunctions : IDisposable
{
    

    private delegate IntPtr ResolveTextCommandPlaceholderDelegate(IntPtr a1, byte* placeholderText, byte a3, byte a4);

    [Signature("E8 ?? ?? ?? ?? 49 8D 4F 18 4C 8B E0", DetourName = nameof(ResolveTextCommandPlaceholderDetour), Fallibility = Fallibility.Infallible)]
    private Hook<ResolveTextCommandPlaceholderDelegate>? ResolveTextCommandPlaceholderHook { get; init; }

    internal GameFunctions()
    {
        SignatureHelper.Initialise(this);
        ResolveTextCommandPlaceholderHook.Enable();
    }

    public void Dispose()
    {
        ResolveTextCommandPlaceholderHook.Disable();
        ResolveTextCommandPlaceholderHook.Dispose();
    }

    internal void PlaySound(Sounds id)
    {
        UIModule.PlaySound((uint)id);
    }

    internal bool IsInInstance()
    {
        return Svc.Condition[ConditionFlag.BoundByDuty56];
    }

    private void ListCommand(string name, ushort world, string commandName)
    {
        var row = Svc.Data.GetExcelSheet<World>()!.GetRow(world);
        if (row == null)
        {
            return;
        }

        var worldName = row.Name.RawString;
        _replacementName = $"{name}@{worldName}";
        Chat.Instance.SendMessage($"/{commandName} add {_placeholder}");
    }

    internal void SendFriendRequest(string name, ushort world)
    {
        ListCommand(name, world, "friendlist");
    }

    internal void AddToMutelist(string name, ushort world)
    {
        ListCommand(name, world, "mutelist");
    }

    private IntPtr _placeholderNamePtr = Marshal.AllocHGlobal(128);
    private string _placeholder = $"<{Guid.NewGuid():N}>";
    private string? _replacementName;

    private IntPtr ResolveTextCommandPlaceholderDetour(IntPtr a1, byte* placeholderText, byte a3, byte a4)
    {
        if (_replacementName == null)
        {
            goto Original;
        }

        var placeholder = MemoryHelper.ReadStringNullTerminated((IntPtr)placeholderText);
        if (placeholder != _placeholder)
        {
            goto Original;
        }

        MemoryHelper.WriteString(_placeholderNamePtr, _replacementName);
        _replacementName = null;

        return _placeholderNamePtr;

    Original:
        return ResolveTextCommandPlaceholderHook!.Original(a1, placeholderText, a3, a4);
    }
}
