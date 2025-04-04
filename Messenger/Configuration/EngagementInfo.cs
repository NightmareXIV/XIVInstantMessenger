﻿namespace Messenger.Configuration;
[Serializable]
public class EngagementInfo
{
    public bool Enabled = true;
    public string Name = "";
    public List<Sender> Participants = [];
    public ChannelCustomizationNullable ChannelCustomization = new();
    public bool OpenOnGenericOutgoing = false;
    public long LastUpdated = DateTimeOffset.Now.ToUnixTimeMilliseconds();
    public Sender? DefaultTarget = null;
    public List<Sender> AllowDMs = [];
    public bool PlaySound = false;
    internal bool IsActive => Enabled && Participants.Count > 0;
}
