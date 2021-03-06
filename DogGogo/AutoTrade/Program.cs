﻿using DogPlatform;
using DogPlatform.Model;
using DogRunService;
using DogRunService.Helper;
using DogService;
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

            DogControlUtils.InitAsync();

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
            var usdtSymbols = CoinUtils.GetAllCommonSymbols("usdt");
            foreach (var symbol in usdtSymbols)
            {
                KlineUtils.CheckTableExistAndCreate(symbol);
            }
            return usdtSymbols.ToList();
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
            var htSymbols = CoinUtils.GetAllCommonSymbols("ht");
            foreach (var symbol in htSymbols)
            {
                KlineUtils.CheckTableExistAndCreate(symbol);
            }
            return htSymbols.ToList();
        }

        private static void RunCoin(List<CommonSymbol> symbols)
        {
            Task.Run(() =>
            {
                while (true)
                {
                    var begin = DateTime.Now;
                    CoinTrade.RunCount = 0;
                    try
                    {
                        var tickers = PlatformApi.GetInstance("xx").GetTickers();
                        for (var i = 0; i < symbols.Count; i++)
                        {
                            var symbol = symbols[i];
                            try
                            {
                                DateTime now = DateTime.Now;
                                var bl = CoinTrade.Run(i, symbol, tickers);
                                var mis = (DateTime.Now - now).TotalMilliseconds;
                                if (mis > 1000)
                                {
                                    Console.WriteLine("每轮------------------------>" + mis);
                                }
                                if (bl)
                                {
                                    Thread.Sleep(150);
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.Error("RunCoin:  " + ex.Message, ex);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex.Message, ex);
                    }
                    var useTime = (DateTime.Now - begin).TotalSeconds;
                    logger.Error("一轮总共耗时：" + (DateTime.Now - begin).TotalSeconds + ", 执行次数：" + CoinTrade.RunCount);


                    Thread.Sleep(1000);

                    if (DateTime.Now.Hour == 0 || DateTime.Now.Hour == 4 || DateTime.Now.Hour == 8
                        || DateTime.Now.Hour == 12 || DateTime.Now.Hour == 16 || DateTime.Now.Hour == 20)
                    {
                        if (DateTime.Now.Minute < 25)
                        {
                            new DogOrderStatDao().AddStatRecord();
                        }
                    }
                }
            });
        }
    }
}
