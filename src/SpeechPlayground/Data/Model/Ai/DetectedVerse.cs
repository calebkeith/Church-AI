using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeechPlayground.Data.Model.Ai
{
    public class DetectedVerse
    {
        public string Book { get; set; }

        public int? Chapter { get; set; }

        public List<int?> Verses { get; set; } = new List<int?>();

        public decimal Confidence { get; set; }

        public decimal Relevance { get; set; }

        public string Paraphrase { get; set; }
    }
}
