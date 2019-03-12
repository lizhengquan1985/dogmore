using SharpDapper.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DogService.DateTypes
{
    [Table("t_dog_order_stat")]
    public class DogOrderStat
    {
        public long Id { get; set; }
        public string Date { get; set; }
        public string DataCount { get; set; }
    }
}
