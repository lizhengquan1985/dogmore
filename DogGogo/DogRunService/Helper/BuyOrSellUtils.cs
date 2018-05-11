using DogPlatform;
using DogPlatform.Model;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DogRunService.Helper
{
    public class BuyOrSellUtils
    {
        static ILog logger = LogManager.GetLogger(typeof(BuyOrSellUtils));

        public static void Begin()
        {
            var symbols = CoinUtils.GetAllCommonSymbols();
            RunCoin(symbols.Where(it => it.BaseCurrency != "btc").ToList());
            //var splitIndex = 16;
            //RunCoin(symbols.GetRange(0, splitIndex + 1));
            //RunCoin(symbols.GetRange(splitIndex, symbols.Count - splitIndex));
        }

        private static void RunCoin(List<CommonSymbols> symbols)
        {
            Task.Run(() =>
            {
                while (true)
                {
                    foreach (var symbol in symbols)
                    {
                        try
                        {
                            // 判断kline存不存在, 不存在读取一次.
                            var key = HistoryKlinePools.GetKey(symbol, "1min");
                            var historyKlineData = HistoryKlinePools.Get(key);
                            if (historyKlineData == null || historyKlineData.Data == null || historyKlineData.Data.Count == 0 || historyKlineData.Date < DateTime.Now.AddSeconds(-10))
                            {
                                KlineUtils.InitOneKine(symbol);
                            }

                            CoinTrade.Run(symbol);

                        }
                        catch (Exception ex)
                        {
                            logger.Error("RunCoin:  " + ex.Message, ex);
                        }
                        //Thread.Sleep(1000 * 1);
                    }
                }
            });
        }
    }
}
