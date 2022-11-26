/*
 * This file contains source code authored by Anna Clemens from (https://git.annaclemens.io/ascclemens/XivCommon/src/branch/main) which is distributed under MIT license
 * 
 * */
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Memory;

namespace Messenger.FriendListManager;

/// <summary>
/// An entry in a player's friend list.
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = Size)]
public unsafe struct FriendListEntry
{
    internal const int Size = 96;
    /// <summary>
    /// The content ID of the friend.
    /// </summary>
    [FieldOffset(0)]
    public readonly ulong ContentId;
    [FieldOffset(13)]
    public readonly byte OnlineStatus;
    /// <summary>
    /// The current world of the friend.
    /// </summary>
    [FieldOffset(22)]
    public readonly ushort CurrentWorld;
    /// <summary>
    /// The home world of the friend.
    /// </summary>
    [FieldOffset(24)]
    public readonly ushort HomeWorld;
    /// <summary>
    /// The job the friend is currently on.
    /// </summary>
    [FieldOffset(33)]
    public readonly byte Job;
    /// <summary>
    /// The friend's raw SeString name. See <see cref="Name"/>.
    /// </summary>
    [FieldOffset(34)]
    public fixed byte RawName[32];
    /// <summary>
    /// The friend's raw SeString free company tag. See <see cref="FreeCompany"/>.
    /// </summary>
    [FieldOffset(66)]
    public fixed byte RawFreeCompany[5];
    /// <summary>
    /// The friend's name.
    /// </summary>
    public SeString Name
    {
        get
        {
            fixed (byte* ptr = this.RawName)
            {
                return MemoryHelper.ReadSeStringNullTerminated((IntPtr)ptr);
            }
        }
    }
    /// <summary>
    /// The friend's free company tag.
    /// </summary>
    public SeString FreeCompany
    {
        get
        {
            fixed (byte* ptr = this.RawFreeCompany)
            {
                return MemoryHelper.ReadSeStringNullTerminated((IntPtr)ptr);
            }
        }
    }

    public bool IsOnline
    {
        get
        {
            return OnlineStatus == 0x80;
        }
    }
}
