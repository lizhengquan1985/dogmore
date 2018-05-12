using SharpDapper.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DogService.DateTypes
{
    [Table("t_control_buy_critical")]
    public class ControlBuyCritical
    {
        [Key]
        public long Id { get; set; }
        public string SymbolName { get; set; }
        /// <summary>
        /// 最大临界值
        /// </summary>
        public decimal MaxCriticalValue { get; set; }
        public DateTime ExpiredTime { get; set; }
        public bool IsValid { get; set; }
    }
}
