using SharpDapper.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DogService.DateTypes
{
    [Table("t_dog_empty_sell")]
    public class DogEmptySell
    {
        [Key]
        public long Id { get; set; }
        public string SymbolName { get; set; }
        /// <summary>
        /// 基础分区，btc，eth，usdt，ht
        /// </summary>
        public string QuoteCurrency { get; set; }
        public string AccountId { get; set; }
        public string UserName { get; set; }
        public long SellOrderId { get; set; }
        public string SellOrderResult { get; set; }
        public decimal SellQuantity { get; set; }
        public decimal SellOrderPrice { get; set; }
        public decimal SellTradePrice { get; set; }
        public string SellState { get; set; }
        public string SellOrderDetail { get; set; }
        public string SellOrderMatchResults { get; set; }
        public string SellMemo { get; set; }
        public DateTime SellDate { get; set; }
        public bool IsFinished { get; set; }
    }

    [Table("t_dog_empty_buy")]
    public class DogEmptyBuy
    {
        [Key]
        public long Id { get; set; }
        public string SymbolName { get; set; }
        /// <summary>
        /// 基础分区，btc，eth，usdt，ht
        /// </summary>
        public string QuoteCurrency { get; set; }
        public string AccountId { get; set; }
        public string UserName { get; set; }
        public long SellOrderId { get; set; }
        public long BuyOrderId { get; set; }
        public string BuyOrderResult { get; set; }
        public decimal BuyQuantity { get; set; }
        public decimal BuyOrderPrice { get; set; }
        public decimal BuyTradePrice { get; set; }
        public string BuyState { get; set; }
        public string BuyMemo { get; set; }
        public string BuyOrderDetail { get; set; }
        public string BuyOrderMatchResults { get; set; }
        public DateTime BuyDate { get; set; }
    }
}
