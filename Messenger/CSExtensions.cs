using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace Messenger;
public static unsafe class CSExtensions
{
    /*extension(ref AcquaintanceModule.Acquaintance obj)
    {
        public byte Reason
        {
            get
            {
                fixed(void* ptr = &obj)
                {
                    return *(byte*)((nint)ptr + 210);
                }
            }
        }
    }*/

    public static byte GetReason(this ref AcquaintanceModule.Acquaintance obj)
    {
        fixed(void* ptr = &obj)
        {
            return *(byte*)((nint)ptr + 210);
        }
    }
}