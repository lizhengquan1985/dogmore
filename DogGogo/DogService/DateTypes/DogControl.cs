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
        /// 基础分区，btc，eth，usdt，ht
        /// </summary>
        public string QuoteCurrency { get; set; }
        /// <summary>
        /// 到了这个价可以考虑做空，做空也要阶梯式的做。
        /// </summary>
        public decimal EmptyPrice { get; set; }
        /// <summary>
        /// 最大投入价格
        /// </summary>
        public decimal MaxInputPrice { get; set; }
        /// <summary>
        /// 每点击一次保持，都会把过去的设置改成false
        /// </summary>
        public bool IsValid { get; set; }
        public DateTime CreateTime { get; set; }

        public decimal HistoryMax { get; set; }
        public decimal HistoryMin { get; set; }
        public decimal AvgPrice { get; set; }
        /// <summary>
        /// 将要推出
        /// </summary>
        public bool WillDelist { get; set; }
    }

    [Table("t_dog_control")]
    public class DogControlMemo : DogControl
    {
        public decimal Min8
        {
            get { return this.HistoryMin * (decimal)Math.Pow(1.08, 10); }
        }
    }
}
