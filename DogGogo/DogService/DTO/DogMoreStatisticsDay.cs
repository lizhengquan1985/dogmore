using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DogService.DTO
{
    public class DogMoreStatisticsDay
    {
        public string BDate { get; set; }
        public int BCount { get; set; }
        public decimal BAmount { get; set; }
        public string SDate { get; set; }
        public int SCount { get; set; }
        public decimal SAmount { get; set; }
        public decimal Earning { get; set; }
    }
}
