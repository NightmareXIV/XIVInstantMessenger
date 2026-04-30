using Dalamud.Game.Chat;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using System;
using System.Collections.Generic;
using System.Text;

namespace Messenger.Miscellaneous;

/// <summary>
/// This struct represents an intercepted chat message.
/// </summary>
internal class MockChatMessage : IHandleableChatMessage
{
    /// <inheritdoc />
    public XivChatType LogKind { get; private set; }

    /// <inheritdoc />
    public XivChatRelationKind SourceKind { get; private set; }

    /// <inheritdoc />
    public XivChatRelationKind TargetKind { get; private set; }

    /// <inheritdoc />
    public SeString Sender
    {
        get;
        set
        {
            var newValue = value ?? new SeString();

            if(field == null || !field.Encode().SequenceEqual(newValue.Encode()))
                this.SenderModified = true;

            field = newValue;
        }
    }

    /// <inheritdoc />
    public SeString Message
    {
        get;
        set
        {
            var newValue = value ?? new SeString();

            if(field == null || !field.Encode().SequenceEqual(newValue.Encode()))
                this.MessageModified = true;

            field = newValue;
        }
    }

    /// <inheritdoc />
    public bool SenderModified { get; private set; }

    /// <inheritdoc />
    public bool MessageModified { get; private set; }

    /// <inheritdoc />
    public int Timestamp { get; private set; }

    /// <inheritdoc />
    public bool IsHandled { get; private set; }

    /// <inheritdoc />
    public void PreventOriginal() => this.IsHandled = true;

    /// <summary>
    /// Sets data for a new chat message, allowing the object to be reused.
    /// </summary>
    /// <param name="logKind">The type of chat.</param>
    /// <param name="sourceKind">The relationship of the entity sending the message or performing the action.</param>
    /// <param name="targetKind">The relationship of the entity receiving the message or being targeted by the action.</param>
    /// <param name="lSender">The sender name.</param>
    /// <param name="lMessage">The message sent.</param>
    /// <param name="timestamp">The timestamp of when the message was sent.</param>
    internal void SetData(XivChatType logKind, XivChatRelationKind sourceKind, XivChatRelationKind targetKind, SeString lSender, SeString lMessage, int timestamp)
    {
        this.LogKind = logKind;
        this.SourceKind = sourceKind;
        this.TargetKind = targetKind;
        this.Sender = lSender;
        this.Message = lMessage;
        this.SenderModified = false;
        this.MessageModified = false;
        this.Timestamp = timestamp;
        this.IsHandled = false;
    }

    /// <summary>
    /// Clears all data of this object.
    /// </summary>
    internal void Clear()
    {
        this.LogKind = 0;
        this.SourceKind = 0;
        this.TargetKind = 0;
        this.Sender = null;
        this.Message = null;
        this.SenderModified = false;
        this.MessageModified = false;
        this.Timestamp = 0;
        this.IsHandled = false;
    }
}
