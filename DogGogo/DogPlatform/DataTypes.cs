using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DogPlatform
{
    public class ResponseKline
    {
        public string status { get; set; }
        public string ch { get; set; }
        public string ts { get; set; }
        public List<KlineData> data { get; set; }
    }

    public class KlineData
    {
        public long id { get; set; }
        public decimal amount { get; set; }
        public decimal count { get; set; }
        public decimal open { get; set; }
        public decimal close { get; set; }
        public decimal low { get; set; }
        public decimal high { get; set; }
        public decimal vol { get; set; }
    }

    public class ResponseAccount
    {
        public string status { get; set; }
        public List<AccountData> data { get; set; }
    }

    public class AccountData
    {
        public string id { get; set; }
        public string type { get; set; }
        public string subtype { get; set; }
        /// <summary>
        /// working正常
        /// </summary>
        public string state { get; set; }
    }

    public class ResponseOrder
    {
        public string status { get; set; }
        public string data { get; set; }
    }
     

    public class OrderDetailData
    {
        public string id { get; set; }
        // 如 gntusdt
        public string symbol { get; set; }
        public string type { get; set; }
        public decimal price { get; set; }
        public string source { get; set; }
    }

}
