using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DogPlatform.Model
{
    public class AccountBalance
    {
        public long id { get; set; }
        public string type { get; set; }
        public string state { get; set; }
        public List<AccountBalanceItem> list { get; set; }
    }

    public class AccountBalanceItem
    {
        public string currency { get; set; }
        public decimal balance { get; set; }
        public string type { get; set; }
    }
}
