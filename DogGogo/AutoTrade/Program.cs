using DogPlatform;
using DogPlatform.Model;
using DogRunService;
using DogRunService.Helper;
using log4net;
using log4net.Config;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AutoTrade
{
    class Program
    {
        static ILog logger = LogManager.GetLogger(typeof(Program));

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
            BeginTradeAllSymbol();

            // 状态检查
            TradeStateUtils.Begin();
        }

        public static void BeginTradeAllSymbol()
        {
            var symbols = CoinUtils.GetAllCommonSymbols("usdt");
            RunCoin(symbols.Where(it => it.BaseCurrency != "btc" && it.BaseCurrency != "ven").ToList());
        }

        private static void RunCoin(List<CommonSymbols> symbols)
        {
            Task.Run(() =>
            {
                while (true)
                {
                    var begin = DateTime.Now;
                    for (var i = 0; i < symbols.Count; i++)
                    {
                        var symbol = symbols[i];
                        Console.WriteLine($"---> {i}   {symbol.BaseCurrency},{symbol.QuoteCurrency}");
                        try
                        {
                            // 判断kline存不存在, 不存在读取一次.
                            var key = HistoryKlinePools.GetKey(symbol, "1min");
                            var historyKlineData = HistoryKlinePools.Get(key);
                            if(historyKlineData == null)
                            {
                                KlineUtils.InitKlineInToPool(symbol);
                                historyKlineData = HistoryKlinePools.Get(key);
                            }

                            if (historyKlineData == null || historyKlineData.Data == null || historyKlineData.Data.Count < 100 || historyKlineData.Date < DateTime.Now.AddSeconds(-20))
                            {
                                continue;
                            }

                            CoinTrade.Run(symbol);

                        }
                        catch (Exception ex)
                        {
                            logger.Error("RunCoin:  " + ex.Message, ex);
                        }
                    }

                    var useTime = (DateTime.Now - begin).TotalSeconds;
                    if (useTime >= 60)
                    {
                        logger.Error("一轮总共耗时：" + (DateTime.Now - begin).TotalSeconds);
                    }
                    else
                    {
                        Console.WriteLine("一轮总共耗时：" + (DateTime.Now - begin).TotalSeconds);
                        Thread.Sleep(1000 * (60 - (int)useTime));
                    }
                }
            });
        }
    }
}
