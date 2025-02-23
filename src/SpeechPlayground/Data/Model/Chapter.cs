using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SpeechPlayground.Data.Model
{
    public class Chapter
    {
        [JsonPropertyName("chapter")]
        public int Number { get; set; }
        public string Name { get; set; }
        public List<Verse> Verses { get; set; }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;

            var other = (Chapter)obj;

            return other.Number == this.Number
                && other.Name == this.Name;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Number);
        }
    }
}
