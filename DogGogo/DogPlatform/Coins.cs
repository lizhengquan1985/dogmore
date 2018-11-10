using DogPlatform.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DogPlatform
{
    public class CoinUtils
    {
        private static Dictionary<string, CommonSymbol> usdtCoins = new Dictionary<string, CommonSymbol>();
        private static Dictionary<string, CommonSymbol> btcCoins = new Dictionary<string, CommonSymbol>();
        private static Dictionary<string, CommonSymbol> ethCoins = new Dictionary<string, CommonSymbol>();
        private static Dictionary<string, CommonSymbol> htCoins = new Dictionary<string, CommonSymbol>();

        public static void Init()
        {
            PlatformApi api = PlatformApi.GetInstance("xx"); // 不需要角色,可以随意xx
            var commonSymbols = api.GetCommonSymbols();

            // usdt
            var usdtCommonSymbols = commonSymbols.FindAll(it => it.QuoteCurrency == "usdt");
            Console.WriteLine($"us-dt对数量: {usdtCommonSymbols.Count}");
            foreach (var item in usdtCommonSymbols)
            {
                item.LeastBuyQuantity = 1; // TODO 改进
                if (!usdtCoins.ContainsKey(item.BaseCurrency))
                {
                    usdtCoins.Add(item.BaseCurrency, item);
                }
            }

            // b
            var btcCommonSymbols = commonSymbols.FindAll(it => it.QuoteCurrency == "btc");
            Console.WriteLine($"b-tc对数量: {btcCommonSymbols.Count}");
            foreach (var item in btcCommonSymbols)
            {
                item.LeastBuyQuantity = 1; // TODO 改进
                if (!btcCoins.ContainsKey(item.BaseCurrency))
                {
                    btcCoins.Add(item.BaseCurrency, item);
                }
            }

            // e
            var ethCommonSymbols = commonSymbols.FindAll(it => it.QuoteCurrency == "eth");
            Console.WriteLine($"e-th对数量: {ethCommonSymbols.Count}");
            foreach (var item in ethCommonSymbols)
            {
                item.LeastBuyQuantity = 1; // TODO 改进
                if (!ethCoins.ContainsKey(item.BaseCurrency))
                {
                    ethCoins.Add(item.BaseCurrency, item);
                }
            }

            // ht
            var htCommonSymbols = commonSymbols.FindAll(it => it.QuoteCurrency == "ht");
            Console.WriteLine($"h-t对数量: {htCommonSymbols.Count}");
            foreach (var item in htCommonSymbols)
            {
                item.LeastBuyQuantity = 1; // TODO 改进
                if (!htCoins.ContainsKey(item.BaseCurrency))
                {
                    htCoins.Add(item.BaseCurrency, item);
                }
            }
        }

        public static List<CommonSymbol> GetAllCommonSymbols(string quoteCurrency)
        {
            if (quoteCurrency == "usdt")
            {
                var res = usdtCoins.Values.ToList();
                return res.Where(it => it.BaseCurrency != "ven" && it.BaseCurrency != "btc" && it.BaseCurrency != "hsr").ToList();
            }
            else if (quoteCurrency == "btc")
            {
                var res = btcCoins.Values.ToList();
                return res;
            }
            else if (quoteCurrency == "eth")
            {
                var res = ethCoins.Values.ToList();
                return res;
            }
            else if (quoteCurrency == "ht")
            {
                return htCoins.Values.ToList();
            }
            throw new ApplicationException("error");
        }

        public static CommonSymbol GetCommonSymbol(string baseCurrency, string quoteCurrency)
        {
            var symbols = GetAllCommonSymbols(quoteCurrency);
            var symbol = symbols.Find(it => it.BaseCurrency == baseCurrency);
            return symbol;
        }
    }
}
