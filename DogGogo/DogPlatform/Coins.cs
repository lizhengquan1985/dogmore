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
        private static Dictionary<string, CommonSymbols> usdtCoins = new Dictionary<string, CommonSymbols>();
        private static Dictionary<string, CommonSymbols> btcCoins = new Dictionary<string, CommonSymbols>();
        private static Dictionary<string, CommonSymbols> ethCoins = new Dictionary<string, CommonSymbols>();
        private static Dictionary<string, CommonSymbols> htCoins = new Dictionary<string, CommonSymbols>();

        public static void Init()
        {
            PlatformApi api = PlatformApi.GetInstance("xx"); // 不需要角色,可以随意xx
            var commonSymbols = api.GetCommonSymbols();

            // usdt
            var usdtCommonSymbols = commonSymbols.FindAll(it => it.QuoteCurrency == "usdt");
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
            foreach (var item in htCommonSymbols)
            {
                item.LeastBuyQuantity = 1; // TODO 改进
                if (!htCoins.ContainsKey(item.BaseCurrency))
                {
                    htCoins.Add(item.BaseCurrency, item);
                }
            }
        }

        //public static CommonSymbols Get(string name)
        //{
        //    if (!coins.ContainsKey(name))
        //    {
        //        Init();
        //    }

        //    return coins[name];
        //}

        //public static List<string> GetAllCoins()
        //{
        //    // 总共其实有36个, 后期还会增加
        //    if (coins.Count < 10)
        //    {
        //        Init();
        //    }
        //    return coins.Keys.ToList();
        //}

        public static List<CommonSymbols> GetAllCommonSymbols(string quoteCurrency)
        {
            if (quoteCurrency == "usdt")
            {
                var res = usdtCoins.Values.ToList();
                return res.Where(it => it.BaseCurrency != "ven" && it.BaseCurrency != "btc").ToList();
            }
            else if (quoteCurrency == "btc")
            {
                return btcCoins.Values.ToList();
            }
            else if (quoteCurrency == "eth")
            {
                return ethCoins.Values.ToList();
            }
            else if (quoteCurrency == "ht")
            {
                var res = htCoins.Values.ToList();
                return res.Where(it => it.BaseCurrency != "kcash" && it.BaseCurrency != "mt").ToList();
            }
            throw new ApplicationException("error");
        }
    }
}
