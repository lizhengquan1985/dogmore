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
        /// 预计价格
        /// </summary>
        public decimal PredictPrice { get; set; }
        public DateTime PredictExpiredTime { get; set; }
        /// <summary>
        /// 到了这个价可以考虑做空一次
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
        public bool IsValid { get; set; }
        public DateTime CreateTime { get; set; }
    }
}
