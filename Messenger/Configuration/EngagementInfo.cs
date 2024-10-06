namespace Messenger.Configuration;
[Serializable]
public class EngagementInfo
{   
    public bool Enabled = true;
    public string Name = "";
    public List<Sender> Participants = [];
    public ChannelCustomizationNullable ChannelCustomization = new();
    public bool OpenOnGenericOutgoing = false;
    public long LastUpdated = DateTimeOffset.Now.ToUnixTimeMilliseconds();
}
