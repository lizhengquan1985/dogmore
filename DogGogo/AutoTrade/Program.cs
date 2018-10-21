using DogPlatform;
using DogRunService;
using DogRunService.Helper;
using log4net.Config;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoTrade
{
    class Program
    {
        static void Main(string[] args)
        {
            // 注册日志
            XmlConfigurator.Configure(new FileInfo("log4net.config"));

            BeginTrade();
        }

        private static void BeginTrade()
        {
            // 初始化
            CoinUtils.Init();

            UserPools.Push("xx");
            UserPools.Push("qq");

            // 不停的对每个币做操作
            BuyOrSellUtils.Begin();

            // 状态检查
            TradeStateUtils.Begin();
        }
    }
}
