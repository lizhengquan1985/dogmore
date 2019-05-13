using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DogPlatform.Model
{
    public class CommonSymbol
    {
        /// <summary>
        /// 如 x-rp,e-os, s-oc, a-ct
        /// </summary>
        [JsonProperty(PropertyName = "base-currency")]
        public string BaseCurrency { get; set; }

        /// <summary>
        /// 如 u-s-d-t, b-t-c, e-th,h-t,
        /// </summary>
        [JsonProperty(PropertyName = "quote-currency")]
        public string QuoteCurrency { get; set; }

        /// <summary>
        /// 价格精度
        /// </summary>
        [JsonProperty(PropertyName = "price-precision")]
        public int PricePrecision { get; set; }

        /// <summary>
        /// 数量精度
        /// </summary>
        [JsonProperty(PropertyName = "amount-precision")]
        public int AmountPrecision { get; set; }

        [JsonProperty(PropertyName = "symbol-partition")]
        public string SymbolPartition { get; set; }

        /// <summary>
        /// 最小购买数量， api不返回， 自己加上去
        /// </summary>
        public decimal LeastBuyQuantity { get; set; }
    }

    /// <summary>
    /// {"open":0.396338,"close":0.408307,"low":0.395745,"high":0.414551,"amount":1250.7924232536443,"count":5198,
    /// "vol":500.2103917836005,"symbol":"xmreth"}
    /// </summary>
    public class Ticker
    {
        public decimal open { get; set; }
        public decimal close { get; set; }
        public decimal low { get; set; }
        public decimal high { get; set; }
        public decimal amount { get; set; }
        public decimal count { get; set; }
        public decimal vol { get; set; }
        public string symbol { get; set; }
    }
}
