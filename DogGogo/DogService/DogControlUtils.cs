﻿using DogPlatform.Model;
using DogService.Dao;
using log4net;
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
            { "usdt", 52 }, { "btc", 15 }, { "eth", 24 }, { "ht", 10 }
        };

        public static void InitAsync()
        {
            foreach (var key in coinCount.Keys)
            {
                try
                {
                    long count = new DogControlDao().GetCount(key).Result;
                    if (count >= coinCount[key])
                    {
                        coinCount[key] = (int)count;
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex.Message, ex);
                }
            }

        }

        public static decimal GetRecommendBuyAmount(CommonSymbol symbol, decimal recommendAmount, decimal nowPrice)
        {
            if (symbol.QuoteCurrency == "usdt")
            {
                var ladderPosition = GetLadderPosition(symbol.BaseCurrency, symbol.QuoteCurrency, nowPrice);
                var min = (decimal)1.5;
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
                if (recommendAmount < (decimal)0.0006)
                {
                    recommendAmount = (decimal)0.0006;
                }
            }
            else if (symbol.QuoteCurrency == "eth")
            {
                if (recommendAmount < (decimal)0.008)
                {
                    recommendAmount = (decimal)0.008;
                }
            }
            else if (symbol.QuoteCurrency == "ht")
            {
                if (recommendAmount < (decimal)1.1)
                {
                    recommendAmount = (decimal)1.1;
                }
            }
            return recommendAmount;
        }

        public static decimal GetLadderBuy(string symbolName, string quoteCurrency, decimal nowPrice, decimal defaultLadderBuyPercent = (decimal)1.1)
        {
            try
            {
                var max = (decimal)1.12;
                var min = (decimal)1.06;
                var control = new DogControlDao().GetDogControl(symbolName, quoteCurrency);
                if (control == null)
                {
                    return defaultLadderBuyPercent;
                }

                if (control.HistoryMin >= control.HistoryMax || control.HistoryMin <= 0 || control.HistoryMax <= 0)
                {
                    return defaultLadderBuyPercent;
                }

                // 防止价格波动后的, 分隔过合理. 下
                if (control.HistoryMax <= control.HistoryMin * (decimal)2)
                {
                    control.HistoryMax = control.HistoryMax * (decimal)1.2;
                    control.HistoryMin = control.HistoryMin * (decimal)0.4;
                }
                else if (control.HistoryMax <= control.HistoryMin * (decimal)3)
                {
                    control.HistoryMax = control.HistoryMax * (decimal)1;
                    control.HistoryMin = control.HistoryMin * (decimal)0.8;
                }

                if (nowPrice >= control.HistoryMax)
                {
                    defaultLadderBuyPercent = max;
                }
                else if (nowPrice <= control.HistoryMin)
                {
                    defaultLadderBuyPercent = min;
                }
                else
                {
                    var percent = (control.HistoryMax - nowPrice) / (control.HistoryMax - control.HistoryMin);
                    defaultLadderBuyPercent = max - percent * (max - min);
                }

                // 计算出来阶梯
                defaultLadderBuyPercent = Math.Max(defaultLadderBuyPercent, min);
                defaultLadderBuyPercent = Math.Min(defaultLadderBuyPercent, max);
                defaultLadderBuyPercent = Math.Max(defaultLadderBuyPercent, control.LadderBuyPercent);
                return defaultLadderBuyPercent;
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
                return defaultLadderBuyPercent;
            }
        }

        public static decimal GetLadderSell(string symbolName, string quoteCurrency, decimal nowPrice, decimal defaultLadderSellPercent = (decimal)1.1)
        {
            try
            {
                var max = (decimal)1.135;
                var min = (decimal)1.055;
                var control = new DogControlDao().GetDogControl(symbolName, quoteCurrency);
                if (control == null)
                {
                    return defaultLadderSellPercent;
                }

                if (control.HistoryMin >= control.HistoryMax || control.HistoryMin <= 0 || control.HistoryMax <= 0)
                {
                    return defaultLadderSellPercent;
                }

                // 防止价格波动后的, 分隔过合理. 下
                if (control.HistoryMax <= control.HistoryMin * (decimal)2)
                {
                    control.HistoryMax = control.HistoryMax * (decimal)1.4;
                    control.HistoryMin = control.HistoryMin * (decimal)0.8;
                }
                else if (control.HistoryMax <= control.HistoryMin * (decimal)3)
                {
                    control.HistoryMax = control.HistoryMax * (decimal)1.2;
                    control.HistoryMin = control.HistoryMin * (decimal)1;
                }

                if (nowPrice >= control.HistoryMax)
                {
                    defaultLadderSellPercent = min;
                }
                else if (nowPrice <= control.HistoryMin)
                {
                    defaultLadderSellPercent = max;
                }
                else
                {
                    var percent = (control.HistoryMax - nowPrice) / (control.HistoryMax - control.HistoryMin);
                    defaultLadderSellPercent = (max - min) * percent + min;
                }

                // 计算出来阶梯
                defaultLadderSellPercent = Math.Max(defaultLadderSellPercent, min);
                defaultLadderSellPercent = Math.Min(defaultLadderSellPercent, max);
                defaultLadderSellPercent = Math.Min(defaultLadderSellPercent, control.LadderSellPercent);
                return defaultLadderSellPercent;
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
                return defaultLadderSellPercent;
            }
        }

        /// <summary>
        /// 做空的阶梯
        /// </summary>
        /// <param name="symbolName"></param>
        /// <param name="nowPrice"></param>
        /// <param name="defaultLadderSellPercent"></param>
        /// <returns></returns>
        public static decimal GetEmptyLadderSell(string symbolName, string quoteCurrency, decimal nowPrice, decimal defaultEmptyLadderSellPercent = (decimal)1.1)
        {
            try
            {
                var min = (decimal)1.10;
                var max = (decimal)1.16;
                var control = new DogControlDao().GetDogControl(symbolName, quoteCurrency);
                if (control == null || control.HistoryMin >= control.HistoryMax || control.HistoryMin <= 0 || control.HistoryMax <= 0)
                {
                    return defaultEmptyLadderSellPercent;
                }

                // 防止价格波动后的, 分隔过合理. 上
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

                if (control.HistoryMax > control.HistoryMin)
                {
                    var percent = (control.HistoryMax - nowPrice) / (control.HistoryMax - control.HistoryMin);
                    defaultEmptyLadderSellPercent = (max - min) * percent + min;
                }

                defaultEmptyLadderSellPercent = Math.Max(defaultEmptyLadderSellPercent, min);
                defaultEmptyLadderSellPercent = Math.Min(defaultEmptyLadderSellPercent, max);

                return defaultEmptyLadderSellPercent;
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
                return defaultEmptyLadderSellPercent;
            }
        }

        public static int GetRecommendDivideForMore(string symbolName, string quoteCurrency, decimal nowPrice)
        {
            int divide = coinCount[quoteCurrency] * 20;
            try
            {
                var control = new DogControlDao().GetDogControl(symbolName, quoteCurrency);
                if (control == null || control.HistoryMax <= control.HistoryMin || control.HistoryMin <= 0 || control.HistoryMax <= 0)
                {
                    return divide;
                }
                var max = coinCount[quoteCurrency] * 30;
                var min = Math.Max(coinCount[quoteCurrency] * 4, 100);

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

        public static int GetRecommendDivideForEmpty(string symbolName, string quoteCurrency, decimal nowPrice, decimal shouyiQuantity, int divide = 35)
        {
            try
            {
                var max = 70;
                var min = 20;
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

                var percent = (control.HistoryMax - nowPrice) / (control.HistoryMax - control.HistoryMin);
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
    }
}
