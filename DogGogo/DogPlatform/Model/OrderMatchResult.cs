using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DogPlatform.Model
{
    public class OrderMatchResult
    {
        public string id { get; set; }
        // 如 gntusdt
        public string symbol { get; set; }
        public string type { get; set; }
        public decimal price { get; set; }
        public string source { get; set; }
    }
}
