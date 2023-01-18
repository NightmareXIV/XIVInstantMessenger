using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger.Translation
{
    public interface ITranslationProvider : IDisposable
    {
        public string DisplayName { get; }
        public void Initialize();

        /// <summary>
        /// This method will be called IN A DIFFERENT THREAD!
        /// </summary>
        /// <param name="sourceText">Text to translate</param>
        /// <returns>Translated text</returns>
        public string TranslateSynchronous(string sourceText);
        public void DrawSettings();
    }
}
