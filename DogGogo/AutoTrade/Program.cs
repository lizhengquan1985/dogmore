using DogPlatform;
using DogRunService;
using DogRunService.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoTrade
{
    class Program
    {
        static void Main(string[] args)
        {
            BeginTrade();
        }

        private static void BeginTrade()
        {
            // 初始化
            CoinUtils.Init();

            UserPools.Push("xx");
            UserPools.Push("qq");

            // 初始化k线
            //KlineUtils.Begin();

            // 不停的对每个币做操作
            BuyOrSellUtils.Begin();

            // 状态检查
            TradeStateUtils.Begin();
        }
    }
}
