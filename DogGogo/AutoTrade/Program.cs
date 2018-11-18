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

            Console.WriteLine("------begin-------");
            Console.ReadLine();
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
            var runCoins = new List<CommonSymbol>();
            runCoins.AddRange(InitUsdtData());
            runCoins.AddRange(InitBtcData());
            runCoins.AddRange(InitEthData());
            runCoins.AddRange(InitHtData());
            RunCoin(runCoins);
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
            var addCoins = "eth,xrp,bch,ltc,etc,eos,ht,ada,zec,omg,iota,dash,xmr,ardr,steem".Split(',').ToList();
            var addSymbols = btcSymbols.Where(it => addCoins.Contains(it.BaseCurrency)).ToList();

            foreach (var symbol in addSymbols)
            {
                KlineUtils.CheckTableExistAndCreate(symbol);
            }

            return addSymbols.ToList();
        }


        public static List<CommonSymbol> InitEthData()
        {
            // 准备好各种对
            var ethSymbols = CoinUtils.GetAllCommonSymbols("eth");
            var addCoins = "ada,ae,ardr,bat,btm,bts,eos,gnt,hc,hit,ht,icx,iota,lsk,omg,ont,pai,qtum,steem,trx,vet,xlm,xmr,zrx".Split(',').ToList();
            var addSymbols = ethSymbols.Where(it => addCoins.Contains(it.BaseCurrency)).ToList();

            foreach (var symbol in addSymbols)
            {
                KlineUtils.CheckTableExistAndCreate(symbol);
            }

            return addSymbols.ToList();
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
                            CoinTrade.Run(i, symbol);
                        }
                        catch (Exception ex)
                        {
                            logger.Error("RunCoin:  " + ex.Message, ex);
                        }
                    }

                    var useTime = (DateTime.Now - begin).TotalSeconds;
                    if (useTime >= 30)
                    {
                        logger.Error("一轮总共耗时：" + (DateTime.Now - begin).TotalSeconds);
                    }
                    else
                    {
                        Console.WriteLine("一轮总共耗时：" + (DateTime.Now - begin).TotalSeconds);
                        Thread.Sleep(1000 * (30 - (int)useTime));
                    }
                }
            });
        }
    }
}
