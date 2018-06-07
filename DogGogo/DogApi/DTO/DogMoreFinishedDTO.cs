using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DogApi.DTO
{
    public class DogMoreFinishedDTO
    {
        public long BuyOrderId { get; set; }
        public string SymbolName { get; set; }
        public decimal BuyAmount { get; set; }
        public decimal SellQuantity { get; set; }
        public decimal Usdt { get; set; }
        public decimal BuyQuantity { get; set; }
        public decimal BuyFees { get; set; }
        public decimal SellAmount { get; set; }
        public decimal SellFees { get; set; }
        public decimal BaseSymbol { get; set; }
        public string UserName { get; set; }
        public decimal BuyTradePrice { get; set; }
    }
}
