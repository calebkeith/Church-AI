using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeechPlayground.Data.Model
{
    public class Book
    {
        public string Name { get; set; }
        public List<Chapter> Chapters { get; set; }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;

            var other = (Book)obj;

            return other.Name == this.Name;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
    }
}
