using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DogPlatform.Model
{
    public class HBResponse<T> where T : new()
    {
        public string Status { get; set; }
        //public string Ch { get; set; }
        //public long Ts { get; set; }
        public T Data { get; set; }
    }
}
