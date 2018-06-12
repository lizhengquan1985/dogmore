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
        private static Dictionary<string, CommonSymbols> coins = new Dictionary<string, CommonSymbols>();

        public static void Init()
        {
            PlatformApi api = PlatformApi.GetInstance("xx"); // 不需要角色,可以随意xx
            var commonSymbols = api.GetCommonSymbols();
            commonSymbols = commonSymbols.FindAll(it => it.QuoteCurrency == "usdt");
            foreach (var item in commonSymbols)
            {
                item.LeastBuyQuantity = 1; // TODO 改进
                if (!coins.ContainsKey(item.BaseCurrency))
                {
                    coins.Add(item.BaseCurrency, item);
                }
            }
        }

        public static CommonSymbols Get(string name)
        {
            if (!coins.ContainsKey(name))
            {
                Init();
            }

            return coins[name];
        }

        public static List<string> GetAllCoins()
        {
            // 总共其实有36个, 后期还会增加
            if (coins.Count < 10)
            {
                Init();
            }
            return coins.Keys.ToList();
        }

        public static List<CommonSymbols> GetAllCommonSymbols()
        {
            // 总共其实有36个, 后期还会增加
            if (coins.Count < 30)
            {
                Init();
            }
            return coins.Values.ToList();
        }
    }
}
