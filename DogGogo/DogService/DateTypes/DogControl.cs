using SharpDapper.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DogService.DateTypes
{
    [Table("t_dog_control")]
    public class DogControl
    {
        [Key]
        public long Id { get; set; }
        public string SymbolName { get; set; }
        /// <summary>
        /// 预计价格，等接近预计价格时候再做处理
        /// </summary>
        public decimal PredictPrice { get; set; }
        public DateTime PredictExpiredTime { get; set; }
        /// <summary>
        /// 到了这个价可以考虑做空，做空也要阶梯式的做。
        /// </summary>
        public decimal EmptyPrice { get; set; }
        public DateTime EmptyExpiredTime { get; set; }
        /// <summary>
        /// 平均投入额度
        /// </summary>
        public decimal AvgInputAmount { get; set; }
        public DateTime AvgInputExpiredTime { get; set; }
        /// <summary>
        /// 最大投入价格
        /// </summary>
        public decimal MaxInputPrice { get; set; }
        public DateTime MaxInputPriceExpiredTime { get; set; }
        /// <summary>
        /// 购买阶梯数， 2%~6%， 一般情况是3%
        /// </summary>
        public decimal LadderBuyPercent { get; set; }
        public DateTime LadderBuyExpiredTime { get; set; }
        /// <summary>
        /// 出售阶梯数， 2.5%~8%， 一般情况是3.5%
        /// </summary>
        public decimal LadderSellPercent { get; set; }
        public DateTime LadderSellExpiredTime { get; set; }
        /// <summary>
        /// 每点击一次保持，都会把过去的设置改成false
        /// </summary>
        public bool IsValid { get; set; }
        public DateTime CreateTime { get; set; }
    }
}
