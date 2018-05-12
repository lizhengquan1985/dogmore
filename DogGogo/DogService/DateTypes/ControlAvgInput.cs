using SharpDapper.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DogService.DateTypes
{
    [Table("t_control_avg_input")]
    public class ControlAvgInput
    {
        [Key]
        public long Id { get; set; }
        public string SymbolName { get; set; }
        public int InputAvgNum { get; set; }
        public DateTime ExpiredTime { get; set; }
        public bool IsValid { get; set; }
    }
}
