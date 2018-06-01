using SharpDapper.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DogService.DateTypes
{
    /// <summary>
    /// 收割空单，收割多单（收割比率），做空指令
    /// </summary>
    [Table("t_order_reap")]
    public class OrderReap
    {
        [Key]
        public int Id { get; set; }
        public ReapType ReapType { get; set; }
        public long OrderId { get; set; }
        public DateTime CreateTime { get; set; }
        public bool IsMore { get; set; }
        public decimal Percent { get; set; }
    }

    public enum ReapType
    {
        /// <summary>
        /// 收割
        /// </summary>
        ShougeMore = 0,
        /// <summary>
        /// 强制收割
        /// </summary>
        ShougeEmpty = 1,
        /// <summary>
        /// 做空指令
        /// </summary>
        MakeEmpty = 2
    }
}
