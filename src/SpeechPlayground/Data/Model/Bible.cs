using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SpeechPlayground.Data.Model
{
    public class Bible
    {
        private static Bible _instance;
        public static Bible Instance
        {
            get
            {
                if (_instance == null)
                {
                    var assembly = Assembly.GetExecutingAssembly();
                    using (Stream stream = assembly.GetManifestResourceStream("SpeechPlayground.KJV.json"))
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        _instance = JsonSerializer.Deserialize<Bible>(reader.ReadToEnd(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    }
                }

                return _instance;
            }
        }
        public List<Book> Books { get; set; }
    }
}
