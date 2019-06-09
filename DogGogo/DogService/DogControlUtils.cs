using DogPlatform.Model;
using DogService.Dao;
using DogService.DateTypes;
using log4net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DogService
{
    /// <summary>
    /// 这里是管控的代码
    /// </summary>
    public class DogControlUtils
    {
        static ILog logger = LogManager.GetLogger(typeof(DogControlUtils));
        static Dictionary<string, int> coinCount = new Dictionary<string, int> {
            { "usdt", 82 }, { "btc", 200 }, { "eth", 180 }, { "ht", 16 }
        };

        static List<DogControl> dogControls = new List<DogControl>();
        static long dogControlTime = 0;

        public static void InitAsync()
        {
            try
            {
                {
                    long count = new DogControlDao().GetCount("usdt").Result;
                    coinCount["usdt"] = Math.Max(coinCount["usdt"], (int)count);
                }

                {
                    long count = new DogControlDao().GetCount("btc").Result;
                    coinCount["btc"] = Math.Max(coinCount["btc"], (int)count);
                }

                {
                    long count = new DogControlDao().GetCount("eth").Result;
                    coinCount["eth"] = Math.Max(coinCount["eth"], (int)count);
                }

                {
                    long count = new DogControlDao().GetCount("ht").Result;
                    coinCount["ht"] = Math.Max(coinCount["ht"], (int)count);
                }
                Console.WriteLine(JsonConvert.SerializeObject(coinCount));
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
            }


        }

        public static decimal GetRecommendBuyAmount(CommonSymbol symbol, decimal recommendAmount, decimal nowPrice)
        {
            if (symbol.QuoteCurrency == "usdt")
            {
                var ladderPosition = GetLadderPosition(symbol.BaseCurrency, symbol.QuoteCurrency, nowPrice);
                var min = (decimal)2;
                if (ladderPosition < (decimal)0.2)
                {
                    min = (decimal)2;
                }
                if (recommendAmount < min)
                {
                    recommendAmount = min;
                }
            }
            else if (symbol.QuoteCurrency == "btc")
            {
                recommendAmount = (decimal)0.0002;
            }
            else if (symbol.QuoteCurrency == "eth")
            {
                recommendAmount = (decimal)0.008;
            }
            else if (symbol.QuoteCurrency == "ht")
            {
                if (recommendAmount < (decimal)1.3)
                {
                    recommendAmount = (decimal)1.3;
                }
            }
            return recommendAmount;
        }

        public static int GetRecommendDivideForMore(string symbolName, string quoteCurrency, decimal nowPrice)
        {
            int divide = coinCount[quoteCurrency] * 25;
            try
            {
                var control = new DogControlDao().GetDogControl(symbolName, quoteCurrency);
                if (control == null || control.HistoryMax <= control.HistoryMin || control.HistoryMin <= 0 || control.HistoryMax <= 0)
                {
                    return divide;
                }
                var max = coinCount[quoteCurrency] * 38;
                var min = Math.Max(coinCount[quoteCurrency] * 10, 100);

                // 防止价格波动后的, 分隔过合理. 下
                if (control.HistoryMax < control.HistoryMin * (decimal)2)
                {
                    control.HistoryMax = control.HistoryMax * (decimal)1.2;
                    control.HistoryMin = control.HistoryMin * (decimal)0.4;
                }
                if (control.HistoryMax <= control.HistoryMin * (decimal)3)
                {
                    control.HistoryMax = control.HistoryMax * (decimal)1;
                    control.HistoryMin = control.HistoryMin * (decimal)0.8;
                }

                if (nowPrice >= control.HistoryMax)
                {
                    return max;
                }

                if (nowPrice <= control.HistoryMin)
                {
                    return min;
                }

                var percent = (control.HistoryMax - nowPrice) / (control.HistoryMax - control.HistoryMin);
                divide = max - Convert.ToInt32(percent * (max - min));
                divide = Math.Min(divide, max);
                divide = Math.Max(divide, min);

                //logger.Error($"分隔数据：{symbolName} {quoteCurrency} --- divide: {divide} ---- nowPrice: {nowPrice}");

                return divide;
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
                return divide;
            }
        }

        public static int GetRecommendDivideForEmpty(string symbolName, string quoteCurrency, decimal nowPrice, decimal shouyiQuantity, int divide = 35)
        {
            try
            {
                var max = 80;
                var min = 25;
                var control = new DogControlDao().GetDogControl(symbolName, quoteCurrency);
                if (control == null || control.HistoryMax <= control.HistoryMin || control.HistoryMin <= 0 || control.HistoryMax <= 0)
                {
                    return max;
                }

                // 防止价格波动后的, 分隔过合理.
                if (control.HistoryMax < control.HistoryMin * (decimal)2)
                {
                    control.HistoryMax = control.HistoryMax * (decimal)1.5;
                    control.HistoryMin = control.HistoryMin * (decimal)0.8;
                }
                if (control.HistoryMax <= control.HistoryMin * (decimal)3)
                {
                    control.HistoryMax = control.HistoryMax * (decimal)1.2;
                    control.HistoryMin = control.HistoryMin * (decimal)1;
                }

                if (nowPrice < control.HistoryMin)
                {
                    return max;
                }
                if (nowPrice > control.HistoryMax)
                {
                    return min;
                }

                var percent = (control.HistoryMax * (decimal)1.2 - nowPrice) / (control.HistoryMax * (decimal)1.2 - control.HistoryMin * (decimal)1.2);
                divide = min + Convert.ToInt32(percent * (max - min));
                if (divide > max)
                {
                    divide = max;
                }
                if (divide < min)
                {
                    divide = min;
                }
                return divide;
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
                return divide;
            }
        }

        /// <summary>
        /// 0~1
        /// </summary>
        /// <param name="symbolName"></param>
        /// <param name="nowPrice"></param>
        /// <returns></returns>
        public static decimal GetLadderPosition(string symbolName, string quoteCurrency, decimal nowPrice, decimal defaultPosition = (decimal)0.5)
        {
            try
            {
                var control = new DogControlDao().GetDogControl(symbolName, quoteCurrency);
                if (control == null || control.HistoryMin == 0 || control.HistoryMax == 0 || control.HistoryMax < control.HistoryMin)
                {
                    return defaultPosition;
                }

                if (nowPrice > control.HistoryMax)
                {
                    return (decimal)1;
                }

                if (nowPrice < control.HistoryMin)
                {
                    return (decimal)0;
                }

                var percent = (nowPrice - control.HistoryMin) / (control.HistoryMax - control.HistoryMin);
                return percent;
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
                return defaultPosition;
            }
        }

        public static DogControl GetDogControl(string symbolName, string quoteCurrency)
        {
            if (dogControlTime < Utils.GetIdByDate(DateTime.Now) - 60 * 60)
            {
                dogControls = new DogControlDao().ListAllDogControl();
                dogControlTime = Utils.GetIdByDate(DateTime.Now);
                Console.WriteLine("----------从数据库里面读取dogControl-------");
            }

            return dogControls.Find(it => it.SymbolName == symbolName && it.QuoteCurrency == quoteCurrency);
        }
    }
}
