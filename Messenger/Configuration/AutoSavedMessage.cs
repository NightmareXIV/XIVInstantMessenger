using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger.Configuration;
[Serializable]
public class AutoSavedMessage
{
    public Sender Target;
    public string Message;
    public long Time;
}
