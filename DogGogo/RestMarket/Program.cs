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

            CoinUtils.Init();

            InitUsdtData();
            InitBtcData();
            InitEthData();
            InitHtData();

            Console.ReadLine();
        }

        public static void InitUsdtData()
        {
            // 准备好各种对
            var symbols = CoinUtils.GetAllCommonSymbols("btc");
            var removeCoins = new List<string> {
                "ven"
            };
            var addSymbols = symbols.Where(it => !removeCoins.Contains(it.BaseCurrency)).ToList();

            foreach (var symbol in addSymbols)
            {
                KlineUtils.CheckTableExistAndCreate(symbol);
            }

            RunCoin(addSymbols.ToList());
        }

        public static void InitBtcData()
        {
            // 准备好各种对
            var btcSymbols = CoinUtils.GetAllCommonSymbols("btc");
            var addCoins = new List<string> {
                "eos","xrp","eth","ada"
            };
            var addSymbols = btcSymbols.Where(it => addCoins.Contains(it.BaseCurrency)).ToList();

            foreach (var symbol in addSymbols)
            {
                KlineUtils.CheckTableExistAndCreate(symbol);
            }

            RunCoin(addSymbols.ToList());
        }


        public static void InitEthData()
        {
            // 准备好各种对
            var btcSymbols = CoinUtils.GetAllCommonSymbols("eth");
            var addCoins = new List<string> {
                "eos", "xrp", "ada"
            };
            var addSymbols = btcSymbols.Where(it => addCoins.Contains(it.BaseCurrency)).ToList();

            foreach (var symbol in addSymbols)
            {
                KlineUtils.CheckTableExistAndCreate(symbol);
            }

            RunCoin(addSymbols.ToList());
        }


        public static void InitHtData()
        {
            // 准备好各种对
            var symbols = CoinUtils.GetAllCommonSymbols("ht");
            var addCoins = new List<string> {
                "eos", "xrp" ,"ada"
            };
            var addSymbols = symbols.Where(it => addCoins.Contains(it.BaseCurrency)).ToList();

            foreach (var symbol in addSymbols)
            {
                KlineUtils.CheckTableExistAndCreate(symbol);
            }

            RunCoin(addSymbols.ToList());
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

                        Console.WriteLine($"---> {i + 1}   {symbol.BaseCurrency}");
                        try
                        {
                            KlineUtils.InitMarketInDB(symbol);
                        }
                        catch (Exception ex)
                        {
                            logger.Error("RunCoin:  " + ex.Message, ex);
                        }
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
