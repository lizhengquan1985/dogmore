using SharpDapper.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DogService.DateTypes
{
    [Table("t_dog_more_statistics_day")]
    public class DogMoreStatisticsDay
    {
        public long Id { get; set; }
        public string RecordDate { get; set; }
        public int BuyCount { get; set; }
        public decimal BuyAmount { get; set; }
        public int SellCount { get; set; }
        public decimal SellAmount { get; set; }
        public decimal Earning { get; set; }
    }
}
