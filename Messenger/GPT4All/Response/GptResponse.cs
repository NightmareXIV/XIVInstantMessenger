using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger.GPT4All.Response;
public class GptResponse
{
    public List<GptChoice> choices;

    public class GptChoice
    {
        public string finish_reason;
        public int index;
        public GptMessage message;

        public class GptMessage
        {
            public string content;
            public string role;
        }
    }
}
