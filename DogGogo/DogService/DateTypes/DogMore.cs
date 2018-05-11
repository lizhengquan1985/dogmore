using SharpDapper.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DogService.DateTypes
{
    [Table("t_dog_more_buy")]
    public class DogMoreBuy
    {
        [Key]
        public long Id { get; set; }
        public string SymbolName { get; set; }
        public string AccountId { get; set; }
        public string UserName { get; set; }
        public long BuyOrderId { get; set; }
        public string BuyOrderResult { get; set; }
        public decimal BuyQuantity { get; set; }
        public decimal BuyOrderPrice { get; set; }
        public decimal BuyTradePrice { get; set; }
        public string BuyState { get; set; }
        public string BuyFlex { get; set; }
        public string BuyMemo { get; set; }
        public string BuyOrderDetail { get; set; }
        public string BuyOrderMatchResults { get; set; }
        public DateTime BuyDate { get; set; }
        public decimal FlexPercent { get; set; }
        public bool IsFinished { get; set; }
    }

    [Table("t_dog_more_sell")]
    public class DogMoreSell
    {
        [Key]
        public long Id { get; set; }
        public string SymbolName { get; set; }
        public string AccountId { get; set; }
        public string UserName { get; set; }
        public long BuyOrderId { get; set; }
        public long SellOrderId { get; set; }
        public string SellOrderResult { get; set; }
        public decimal SellQuantity { get; set; }
        public decimal SellOrderPrice { get; set; }
        public decimal SellTradePrice { get; set; }
        public string SellState { get; set; }
        public string SellFlex { get; set; }
        public string SellOrderDetail { get; set; }
        public string SellOrderMatchResults { get; set; }
        public string SellMemo { get; set; }
        public DateTime SellDate { get; set; }
    }

    public class StateConst
    {
        public const string PreSubmitted = "pre-submitted"; // 准备提交,
        public const string Submitting = "submitting"; //
        public const string Submitted = "submitted"; // 已提交
        public const string PartialFilled = "partial-filled"; // 部分成交
        public const string PartialCanceled = "partial-canceled"; // 部分成交撤销
        public const string Filled = "filled"; // 完全成交
        public const string Canceled = "canceled"; // 已撤销
    }
}
