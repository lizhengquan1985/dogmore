using DogPlatform;
using DogPlatform.Model;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DogRunService.Helper
{
    public class BuyOrSellUtils
    {
        static ILog logger = LogManager.GetLogger(typeof(BuyOrSellUtils));

        public static void Begin()
        {
            var symbols = CoinUtils.GetAllCommonSymbols("usdt");
            RunCoin(symbols.Where(it => it.BaseCurrency != "btc" && it.BaseCurrency != "ven").ToList());
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
                        Console.WriteLine($"---> {i}   {symbol.BaseCurrency},{symbol.QuoteCurrency}");
                        try
                        {
                            // 判断kline存不存在, 不存在读取一次.
                            var key = HistoryKlinePools.GetKey(symbol, "1min");
                            var historyKlineData = HistoryKlinePools.Get(key);
                            if (historyKlineData == null || historyKlineData.Data == null || historyKlineData.Data.Count == 0 || historyKlineData.Date < DateTime.Now.AddSeconds(-10))
                            {
                                //KlineUtils.InitOneKine(symbol);
                                continue;
                            }
                            else
                            {
                                KlineUtils.InitKlineInToPool(symbol);
                            }

                            //CoinTrade.Run(i, symbol);

                        }
                        catch (Exception ex)
                        {
                            logger.Error("RunCoin:  " + ex.Message, ex);
                        }
                    }

                    var useTime = (DateTime.Now - begin).TotalSeconds;
                    if (useTime >= 60)
                    {
                        if (useTime >= 80)
                        {
                            logger.Error("一轮总共耗时：" + (DateTime.Now - begin).TotalSeconds);
                        }
                    }
                    else
                    {
                        Thread.Sleep(1000 * (60 - (int)useTime));
                    }
                }
            });
        }
    }
}
