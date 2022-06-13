using SER.Models.SERAudit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SER.Models
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var a = new UserAgent("");
            Console.WriteLine(JsonSerializer.Serialize(a)); 
        }
    }
}
