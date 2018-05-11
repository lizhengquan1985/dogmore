using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DogApi
{
    class Program
    {
        static void Main(string[] args)
        {
            // 注册日志
            XmlConfigurator.Configure(new FileInfo("log4net.config"));

            // 启用监听
            StartOptions options = new StartOptions();
            options.Urls.Add("http://localhost:6666");
            options.Urls.Add("http://127.0.0.1:6666");
            options.Urls.Add(string.Format("http://{0}:6666", Environment.MachineName));
            options.Urls.Add("http://+:6666");
            options.Urls.Add("http://localhost:80");
            options.Urls.Add("http://127.0.0.1:80");
            options.Urls.Add(string.Format("http://{0}:80", Environment.MachineName));
            options.Urls.Add("http://+:80");
            WebApp.Start<Startup>(options);

            BeginTrade();

            while (true)
            {
                Console.ReadLine();
            }
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
