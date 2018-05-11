using SharpDapper.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DogService.DateTypes
{
    [Table("t_pig_more")]
    public class PigMore
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string AccountId { get; set; }
        public string UserName { get; set; }
        public decimal BQuantity { get; set; }
        public decimal BOrderP { get; set; }
        public decimal BTradeP { get; set; }
        public DateTime BDate { get; set; }
        public string BOrderResult { get; set; }
        public string BState { get; set; }
        public decimal SQuantity { get; set; }
        public decimal SOrderP { get; set; }
        public decimal STradeP { get; set; }
        public DateTime SDate { get; set; }
        public string SOrderResult { get; set; }
        public string SState { get; set; }
        public string BFlex { get; set; }
        public string SFlex { get; set; }
        public string BMemo { get; set; }
        public string SMemo { get; set; }
        public long BOrderId { get; set; }
        public string BOrderDetail { get; set; }
        public string BOrderMatchResults { get; set; }
        public long SOrderId { get; set; }
        public string SOrderDetail { get; set; }
        public string SOrderMatchResults { get; set; }
        public decimal FlexPercent { get; set; }
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
