﻿using DogPlatform;
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
            var runCoins = new List<CommonSymbols>();
            runCoins.AddRange(InitUsdtData());
            //runCoins.AddRange(InitBtcData());
            //runCoins.AddRange(InitEthData());
            runCoins.AddRange(InitHtData());
            RunCoin(runCoins);
        }

        public static List<CommonSymbols> InitUsdtData()
        {
            // 准备好各种对
            var symbols = CoinUtils.GetAllCommonSymbols("usdt");
            var removeCoins = new List<string> {
                "ven","btc"
            };
            var addSymbols = symbols.Where(it => !removeCoins.Contains(it.BaseCurrency)).ToList();

            foreach (var symbol in addSymbols)
            {
                KlineUtils.CheckTableExistAndCreate(symbol);
            }

            return addSymbols.ToList();
        }

        public static List<CommonSymbols> InitBtcData()
        {
            // 准备好各种对
            var btcSymbols = CoinUtils.GetAllCommonSymbols("btc");
            var addCoins = "xmr,bch,eth,ltc,etc,eos,omg,xrp,dash,zec,ada,steem,iota".Split(',').ToList();
            var addSymbols = btcSymbols.Where(it => addCoins.Contains(it.BaseCurrency)).ToList();

            foreach (var symbol in addSymbols)
            {
                KlineUtils.CheckTableExistAndCreate(symbol);
            }

            return addSymbols.ToList();
        }


        public static List<CommonSymbols> InitEthData()
        {
            // 准备好各种对
            var ethSymbols = CoinUtils.GetAllCommonSymbols("eth");
            var addCoins = "xmr,eos,omg,iota,ada,steem,ht,btm,iost,smt,ela,trx".Split(',').ToList();
            var addSymbols = ethSymbols.Where(it => addCoins.Contains(it.BaseCurrency)).ToList();

            foreach (var symbol in addSymbols)
            {
                KlineUtils.CheckTableExistAndCreate(symbol);
            }

            return addSymbols.ToList();
        }


        public static List<CommonSymbols> InitHtData()
        {
            // 准备好各种对
            var symbols = CoinUtils.GetAllCommonSymbols("ht");
            foreach (var symbol in symbols)
            {
                KlineUtils.CheckTableExistAndCreate(symbol);
            }

            return symbols.ToList();
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
