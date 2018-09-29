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

        public static decimal? GetPredictPrice(string symbolName)
        {
            try
            {
                var control = new DogControlDao().GetDogControl(symbolName);
                if (control == null || control.PredictExpiredTime < DateTime.Now)
                {
                    return null;
                }
                return control.PredictPrice;
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
                return null;
            }
        }

        public static decimal? GetEmptyPrice(string symbolName)
        {
            try
            {
                var control = new DogControlDao().GetDogControl(symbolName);
                if (control == null || control.EmptyExpiredTime < DateTime.Now)
                {
                    return null;
                }
                return control.EmptyPrice;
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
                return null;
            }
        }

        public static decimal? GetAvgInputAmount(string symbolName)
        {
            try
            {
                var control = new DogControlDao().GetDogControl(symbolName);
                if (control == null || control.AvgInputExpiredTime < DateTime.Now)
                {
                    return null;
                }
                return control.AvgInputAmount;
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
                return null;
            }
        }

        public static decimal? GetMaxInputPrice(string symbolName)
        {
            try
            {
                var control = new DogControlDao().GetDogControl(symbolName);
                if (control == null || control.MaxInputPriceExpiredTime < DateTime.Now)
                {
                    return null;
                }
                return control.MaxInputPrice;
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
                return null;
            }
        }

        public static decimal GetLadderBuy(string symbolName, decimal nowPrice, decimal defaultLadderBuyPercent = (decimal)1.1)
        {
            try
            {
                var max = (decimal)1.135;
                var min = (decimal)1.055;
                var control = new DogControlDao().GetDogControl(symbolName);
                if (control == null)
                {
                    return defaultLadderBuyPercent;
                }

                if (control.LadderBuyExpiredTime > DateTime.Now && control.LadderBuyPercent >= min)
                {
                    return control.LadderBuyPercent;
                }

                if (control.HistoryMin >= control.HistoryMax || control.HistoryMin <= 0 || control.HistoryMax <= 0)
                {
                    return defaultLadderBuyPercent;
                }

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

                // 计算出来阶梯
                var percent = (control.HistoryMax - nowPrice) / (control.HistoryMax - control.HistoryMin);
                defaultLadderBuyPercent = max - percent * (max - min);
                defaultLadderBuyPercent = Math.Max(defaultLadderBuyPercent, min);
                defaultLadderBuyPercent = Math.Min(defaultLadderBuyPercent, max);
                return defaultLadderBuyPercent;
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
                return defaultLadderBuyPercent;
            }
        }

        public static decimal GetLadderSell(string symbolName, decimal nowPrice, decimal defaultLadderSellPercent = (decimal)1.05)
        {
            try
            {
                var min = (decimal)1.04;
                var max = (decimal)1.08;
                var control = new DogControlDao().GetDogControl(symbolName);
                if (control != null && control.HistoryMin > 0 && control.HistoryMax > 0 && control.HistoryMax > control.HistoryMin)
                {
                    max = max + (control.HistoryMax / control.HistoryMin - 2) / 180;
                    if (max > (decimal)1.12)
                    {
                        max = (decimal)1.12;
                    }
                    if (max < (decimal)1.06)
                    {
                        max = (decimal)1.06;
                    }

                    var percent = (control.HistoryMax - nowPrice) / (control.HistoryMax - control.HistoryMin);
                    defaultLadderSellPercent = (max - min) * percent + min;

                    if (control.LadderSellExpiredTime > DateTime.Now && control.LadderSellPercent < defaultLadderSellPercent)
                    {
                        defaultLadderSellPercent = control.LadderSellPercent > min ? control.LadderSellPercent : min;
                    }
                }
                defaultLadderSellPercent = Math.Max(defaultLadderSellPercent, min);
                defaultLadderSellPercent = Math.Min(defaultLadderSellPercent, max);
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
        public static decimal GetEmptyLadderSell(string symbolName, decimal nowPrice, decimal defaultEmptyLadderSellPercent = (decimal)1.1)
        {
            try
            {
                var min = (decimal)1.10;
                var max = (decimal)1.16;
                var control = new DogControlDao().GetDogControl(symbolName);
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

        public static int GetRecommendDivideForMore(string symbolName, decimal nowPrice, int divide = 700)
        {
            try
            {
                var control = new DogControlDao().GetDogControl(symbolName);
                if (control == null || control.HistoryMax <= control.HistoryMin || control.HistoryMin <= 0 || control.HistoryMax <= 0)
                {
                    return divide;
                }
                var max = 1500;
                var min = 200;

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

        public static int GetRecommendDivideForEmpty(string symbolName, decimal nowPrice, decimal shouyiQuantity, int divide = 24)
        {
            try
            {
                var max = 52;
                var min = 12;
                if (nowPrice * shouyiQuantity > 200)
                {
                    max = 60;
                    min = 15;
                }
                if (nowPrice * shouyiQuantity > 300)
                {
                    max = 70;
                    min = 18;
                }
                var control = new DogControlDao().GetDogControl(symbolName);
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
        public static decimal GetLadderPosition(string symbolName, decimal nowPrice, decimal defaultPosition = (decimal)0.5)
        {
            try
            {
                var control = new DogControlDao().GetDogControl(symbolName);
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
