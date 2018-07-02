using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DogService.DTO
{
    public class DogMoreBuyNotFinishedStatistics
    {
        public string SymbolName { get; set; }
        public decimal MinPrice { get; set; }
        public decimal MaxPrice { get; set; }
        public decimal TotalQuantity { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal NowTotalAmount { get; set; }
        public decimal NowPrice { get; set; }
        public int Count { get; set; }
    }
}
