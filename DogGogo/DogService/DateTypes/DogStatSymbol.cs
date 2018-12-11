using SharpDapper.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DogService.DateTypes
{
    [Table("t_dog_stat_symbol")]
    public class DogStatSymbol
    {
        public string SymbolName { get; set; }
        public string StatDate { get; set; }
        public decimal Amount { get; set; }
        public DateTime CreateTime { get; set; }
        public decimal EarnAmount { get; set; }
    }
}
