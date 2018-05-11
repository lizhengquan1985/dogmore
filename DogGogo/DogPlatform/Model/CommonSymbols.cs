using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DogPlatform.Model
{
    public class CommonSymbols
    {
        /// <summary>
        /// 如 e-t-h
        /// </summary>
        [JsonProperty(PropertyName = "base-currency")]
        public string BaseCurrency { get; set; }

        /// <summary>
        /// 如 u-s-d-t
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
}
