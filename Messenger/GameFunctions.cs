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
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Lumina.Excel.GeneratedSheets;

#pragma warning disable CS8632,CS0649
namespace Messenger;

internal unsafe class GameFunctions : IDisposable
{
    /*[Signature("44 8B 89 ?? ?? ?? ?? 4C 8B C1 45 85 C9")]
    delegate* unmanaged<void*, int, IntPtr> _getTellHistory;*/

    [Signature("E8 ?? ?? ?? ?? 8B FD 8B CD", Fallibility = Fallibility.Infallible)]
    private delegate* unmanaged<IntPtr, uint, IntPtr> _getInfoProxyByIndex;

    [Signature("4C 8B 81 ?? ?? ?? ?? 4D 85 C0 74 17", Fallibility = Fallibility.Infallible)]
    private delegate* unmanaged<RaptureLogModule*, uint, ulong> _getContentIdForChatEntry;

    [Signature("8B 77 ?? 8D 46 01 89 47 14 81 FE ?? ?? ?? ?? 72 03 FF 47", Offset = 2, Fallibility = Fallibility.Infallible)]
    private byte? _currentChatEntryOffset;

    private delegate IntPtr ResolveTextCommandPlaceholderDelegate(IntPtr a1, byte* placeholderText, byte a3, byte a4);

    [Signature("E8 ?? ?? ?? ?? 49 8D 4F 18 4C 8B E0", DetourName = nameof(ResolveTextCommandPlaceholderDetour), Fallibility = Fallibility.Infallible)]
    private Hook<ResolveTextCommandPlaceholderDelegate>? ResolveTextCommandPlaceholderHook { get; init; }

    private delegate ulong PlaySoundDelegate(int id, ulong unk1, ulong unk2);
    [Signature("E8 ?? ?? ?? ?? 4D 39 BE", Fallibility = Fallibility.Infallible)]
    private PlaySoundDelegate PlaySoundFunction;

    [Signature("FF 90 ?? ?? ?? ?? 48 8B C8 BA ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 8B F0 48 85 C0 0F 84 ?? ?? ?? ?? 48 8B 10 33 ED", Offset = 2, Fallibility = Fallibility.Infallible)]
    private readonly int? _infoModuleVfunc;

    [Signature("E8 ?? ?? ?? ?? 41 8D 4F 08 84 C0", Fallibility = Fallibility.Infallible)]
    private readonly delegate* unmanaged<byte> InInstance = null;

    internal GameFunctions()
    {
        SignatureHelper.Initialise(this);
        ResolveTextCommandPlaceholderHook.Enable();
        //SetChatLogTellTargetHook.Enable();
    }

    public void Dispose()
    {
        ResolveTextCommandPlaceholderHook.Disable();
        ResolveTextCommandPlaceholderHook.Dispose();
        //SendTellHook?.Disable();
        //SendTellHook?.Dispose();
    }

    internal void InstallHooks()
    {
        //if(!SendTellHook.IsEnabled) SendTellHook.Enable();
    }

    /*internal byte SendTellDetour(IntPtr a1, ulong cid, ushort world, Utf8String* name, Utf8String* message, byte reason, ulong world2)
    {
        DuoLog.Information($"a1:{a1}, cid:{cid:X16}, world:{world}, name:{name->ToString()}, msg:{message->ToString()}, reason:{reason}, world2:{world2}");
        return SendTellHook.Original(a1, cid, world, name, message, reason, world2);
    }*/

    /*internal void SendTell(byte reason, ulong contentId, string name, ushort homeWorld, string message)
    {
        var uName = Utf8String.FromString(name);
        var uMessage = Utf8String.FromString(message);

        var networkModule = this._getNetworkModule(Framework.Instance());
        var a1 = *(IntPtr*)(networkModule + 8);
        var logModule = Framework.Instance()->GetUiModule()->GetRaptureLogModule();

        this.SendTellHook.Original(a1, contentId, homeWorld, uName, uMessage, (byte)reason, homeWorld);

        uName->Dtor();
        IMemorySpace.Free(uName);

        uMessage->Dtor();
        IMemorySpace.Free(uMessage);
    }*/

    /*internal (Sender sender, ulong CID)? GetTellHistoryInfo(int index)
    {
        var acquaintanceModule = Framework.Instance()->GetUiModule()->GetAcquaintanceModule();
        if (acquaintanceModule == null)
        {
            return null;
        }

        var ptr = this._getTellHistory(acquaintanceModule, index);
        if (ptr == IntPtr.Zero)
        {
            return null;
        }

        var name = MemoryHelper.ReadStringNullTerminated(*(IntPtr*)ptr);
        var world = *(ushort*)(ptr + 0xD0);
        var contentId = *(ulong*)(ptr + 0xD8);

        return (new Sender(name, world), contentId);
    }*/

    internal void PlaySound(Sounds id)
    {
        PlaySoundFunction((int)id, 0ul, 0ul);
    }

    internal bool IsInInstance()
    {
        return InInstance() != 0;
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
        if (Svc.ClientState.LocalPlayer?.CurrentWorld.GameData.DataCenter.Value.RowId !=
            Svc.Data.GetExcelSheet<World>().GetRow(world).DataCenter.Value.RowId)
        {
            Notify.Error("Target is located in different data center");
        }
        else
        {
            ListCommand(name, world, "friendlist");
        }
    }

    internal void AddToBlacklist(string name, ushort world)
    {
        if (Svc.ClientState.LocalPlayer?.CurrentWorld.GameData.DataCenter.Value.RowId !=
            Svc.Data.GetExcelSheet<World>().GetRow(world).DataCenter.Value.RowId)
        {
            Notify.Error("Target is located in different data center");
        }
        else
        {
            ListCommand(name, world, "blist");
        }
    }

    internal ulong? GetContentIdForEntry(uint index)
    {
        if (_getContentIdForChatEntry == null)
        {
            return null;
        }

        return _getContentIdForChatEntry(Framework.Instance()->GetUiModule()->GetRaptureLogModule(), index);
    }


    internal uint? GetCurrentChatLogEntryIndex()
    {
        if (_currentChatEntryOffset == null)
        {
            return null;
        }

        var log = (IntPtr)Framework.Instance()->GetUiModule()->GetRaptureLogModule();
        return *(uint*)(log + _currentChatEntryOffset.Value);
    }

    private IntPtr GetInfoModule()
    {
        if (_infoModuleVfunc is not { } vfunc)
        {
            return IntPtr.Zero;
        }

        var uiModule = Framework.Instance()->GetUiModule();
        var getInfoModule = (delegate* unmanaged<UIModule*, IntPtr>)uiModule->vfunc[vfunc / 8];
        return getInfoModule(uiModule);
    }


    internal IntPtr GetInfoProxyByIndex(uint idx)
    {
        var infoModule = GetInfoModule();
        return infoModule == IntPtr.Zero ? IntPtr.Zero : _getInfoProxyByIndex(infoModule, idx);
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
