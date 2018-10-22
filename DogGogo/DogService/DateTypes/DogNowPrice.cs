using SharpDapper.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DogService.DateTypes
{
    [Table("t_dog_now_price")]
    public class DogNowPrice
    {
        [Key]
        public long Id { get; set; }
        public string SymbolName { get; set; }
        public string QuoteCurrency { get; set; }
        public decimal NowPrice { get; set; }
        public long NowTime { get; set; }
    }
}
