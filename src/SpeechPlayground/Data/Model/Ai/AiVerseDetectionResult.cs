using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeechPlayground.Data.Model.Ai
{
    public class AiVerseDetectionResult
    {
        public List<DetectedVerse> MainVerses { get; set; }

        public List<DetectedVerse> ReferenceVerses { get; set; }
    }
}
