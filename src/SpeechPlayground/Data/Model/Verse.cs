using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SpeechPlayground.Data.Model
{
    public class Verse
    {
        [JsonPropertyName("verse")]
        public int Number { get; set; }
        public int Chapter { get; set; }
        public string Name { get; set; }
        public string Text { get; set; }
        public string Book { get; set; }

        public override string ToString()
        {
            return $"{Name} - {Text}";
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;

            var other = (Verse)obj;

            return other.Chapter == this.Chapter
                && other.Number == this.Number
                && other.Book == this.Book;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Chapter, Number, Book);
        }
    }
}
