using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger.Services.MessageParsingService.Segments;
public interface ISegment
{
    public void Draw(Action? postMessageFunctions);
}
