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

            var runCoins = new List<CommonSymbols>();
            runCoins.AddRange(InitUsdtData());
            runCoins.AddRange(InitBtcData());
            runCoins.AddRange(InitEthData());
            runCoins.AddRange(InitHtData());

            // 开始
            RunCoin(runCoins);

            Console.ReadLine();
        }

        public static List<CommonSymbols> InitUsdtData()
        {
            // 准备好各种对
            var symbols = CoinUtils.GetAllCommonSymbols("usdt");
            var removeCoins = new List<string> {
                "ven"
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

                        Console.WriteLine($"---> {i + 1}   {symbol.BaseCurrency}{symbol.QuoteCurrency}");
                        try
                        {
                            KlineUtils.InitMarketInDB(symbol);
                        }
                        catch (Exception ex)
                        {
                            logger.Error("RunCoin:  " + ex.Message, ex);
                        }

                        // 暂停100毫秒
                        Thread.Sleep(50);
                    }

                    var useTime = (DateTime.Now - begin).TotalSeconds;
                    if (useTime > 60)
                    {
                        logger.Error("一轮总共耗时：{useTime}秒");
                    }
                    else
                    {
                        Console.WriteLine($"一轮总共耗时：{useTime}秒");

                        Thread.Sleep((60 - (int)useTime) * 1000);
                    }
                }
            });
        }
    }
}
