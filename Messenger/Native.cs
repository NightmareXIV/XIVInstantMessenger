namespace Messenger
{
    internal class Native
    {
        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void igBringWindowToDisplayFront(IntPtr ptr);
        
        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr igGetCurrentWindow();
    }
}
