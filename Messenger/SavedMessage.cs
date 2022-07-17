namespace Messenger
{
    internal class SavedMessage
    {
        public string Message;
        public long Time = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        public bool IsIncoming;
        public string OverrideName = null;
        public bool IsSystem = false;
        public string GUID { get; } = Guid.NewGuid().ToString();
    }
}
