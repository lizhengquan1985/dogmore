using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DogApi.DTO
{
    public class TodayTradeDTO
    {
        public string Name { get; internal set; }
        public decimal BQuantity { get; internal set; }
        public decimal BTradeP { get; internal set; }
        public decimal STradeP { get; internal set; }
        public decimal SQuantity { get; internal set; }
        public DateTime SDate { get; internal set; }
        public DateTime BDate { get; internal set; }
    }
}
