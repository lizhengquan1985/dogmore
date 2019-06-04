using DogPlatform;
using DogPlatform.Model;
using DogRunService;
using DogRunService.Helper;
using DogService.Dao;
using log4net;
using log4net.Config;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RestMarket
{
    class Program
    {
        static ILog logger = LogManager.GetLogger(typeof(Program));

        static void Main(string[] args)
        {
            // 注册日志
            XmlConfigurator.Configure(new FileInfo("log4net.config"));
            logger.Info("-----------------------Main---------------------------");

            CoinUtils.Init();

            var runCoins = new List<CommonSymbol>();
            runCoins.AddRange(InitUsdtData());
            runCoins.AddRange(InitBtcData());
            runCoins.AddRange(InitEthData());
            runCoins.AddRange(InitHtData());

            foreach (var symbol in runCoins)
            {
                new DogCoinDao().CreateNewDogCoin(symbol.BaseCurrency, 100000);
            }

            // 开始
            RunCoin(runCoins);

            Console.ReadLine();
        }

        public static List<CommonSymbol> InitUsdtData()
        {
            // 准备好各种对
            var symbols = CoinUtils.GetAllCommonSymbols("usdt");
            foreach (var symbol in symbols)
            {
                KlineUtils.CheckTableExistAndCreate(symbol);
            }
            return symbols.ToList();
        }

        public static List<CommonSymbol> InitBtcData()
        {
            // 准备好各种对
            var btcSymbols = CoinUtils.GetAllCommonSymbols("btc");
            foreach (var symbol in btcSymbols)
            {
                KlineUtils.CheckTableExistAndCreate(symbol);
            }
            return btcSymbols.ToList();
        }


        public static List<CommonSymbol> InitEthData()
        {
            // 准备好各种对
            var ethSymbols = CoinUtils.GetAllCommonSymbols("eth");
            foreach (var symbol in ethSymbols)
            {
                KlineUtils.CheckTableExistAndCreate(symbol);
            }
            return ethSymbols.ToList();
        }


        public static List<CommonSymbol> InitHtData()
        {
            // 准备好各种对
            var symbols = CoinUtils.GetAllCommonSymbols("ht");
            foreach (var symbol in symbols)
            {
                KlineUtils.CheckTableExistAndCreate(symbol);
            }
            return symbols.ToList();
        }

        private static void RunCoin(List<CommonSymbol> symbols)
        {
            Task.Run(() =>
            {
                while (true)
                {
                    var begin = DateTime.Now;

                    for (var i = 0; i < symbols.Count; i++)
                    {
                        var symbol = symbols[i];

                        try
                        {
                        }
                        catch (Exception ex)
                        {
                            logger.Error("RunCoin:  " + ex.Message, ex);
                        }

                        Thread.Sleep(1000);
                    }
                }
            });
        }
    }
}
