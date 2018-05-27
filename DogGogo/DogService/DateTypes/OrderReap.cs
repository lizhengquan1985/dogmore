using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DogService.DateTypes
{
    public class OrderReap
    {
        public int Id { get; set; }
        public ReapType ReapType { get; set; }
        public long OrderId { get; set; }
        public DateTime CreateTime { get; set; }
        public bool IsMore { get; set; }
    }

    public enum ReapType
    {
        Shouge,
        ForceShouge
    }
}
