using Dalamud.Game.Text.SeStringHandling;
using ECommons.ExcelServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger.Services.MessageProcessorService;
public record struct LogMessage
{
    public SeString Sender;
    public SeString? Message;
    public int World;
    public ulong CID;
    public ulong AID;

    public LogMessage(SeString sender, int world, ulong cID, ulong aID) : this()
    {
        Sender = sender ?? throw new ArgumentNullException(nameof(sender));
        World = world;
        CID = cID;
        AID = aID;
    }

    public LogMessage(SeString sender, SeString message, int world, ulong cID, ulong aID)
    {
        Sender = sender ?? throw new ArgumentNullException(nameof(sender));
        Message = message;
        World = world;
        CID = cID;
        AID = aID;
    }

    public override string ToString()
    {
        return $"""
            Sender: {Sender}
            Message: {Message}
            World: {ExcelWorldHelper.GetName(World)}
            CID: {CID:X16}
            AID: {AID:X16}
            """;
    }
}
