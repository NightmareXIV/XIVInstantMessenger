using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger.GPT4All.Request;
public class GptRequest
{
    public string model;
    public List<GptMessage> messages;
    public int max_tokens = 2048;
    public float temperature = 0.7f;
}
