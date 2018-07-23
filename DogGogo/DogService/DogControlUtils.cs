using DogService.Dao;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DogService
{
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

        public static decimal GetLadderBuy(string symbolName, decimal nowPrice, decimal defaultLadderBuyPercent = (decimal)1.06)
        {
            try
            {
                var max = (decimal)1.12;
                var min = (decimal)1.06;
                var control = new DogControlDao().GetDogControl(symbolName);
                if (control != null && control.LadderBuyExpiredTime > DateTime.Now)
                {
                    defaultLadderBuyPercent = control.LadderBuyPercent;
                }
                else if (control != null && nowPrice <= control.HistoryMax && nowPrice >= control.HistoryMin)
                {
                    // 计算出来阶梯
                    var percent = (control.HistoryMax - nowPrice) / (control.HistoryMax - control.HistoryMin);
                    defaultLadderBuyPercent = max - percent * (max - min);
                    if (defaultLadderBuyPercent > max)
                    {
                        defaultLadderBuyPercent = max;
                    }
                    if (defaultLadderBuyPercent < min)
                    {
                        defaultLadderBuyPercent = min;
                    }
                }
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
                if (control != null && control.LadderSellExpiredTime > DateTime.Now)
                {
                    defaultLadderSellPercent = control.LadderSellPercent;
                }
                else if (control != null && control.HistoryMin > 0 && control.HistoryMax > 0 && control.HistoryMax > control.HistoryMin)
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

        public static int GetRecommendDivide(string symbolName, decimal nowPrice, int divide = 220)
        {
            try
            {
                var control = new DogControlDao().GetDogControl(symbolName);
                if (control == null || control.HistoryMax <= control.HistoryMin || nowPrice > control.HistoryMax || nowPrice < control.HistoryMin)
                {
                    return divide;
                }

                var percent = (control.HistoryMax - nowPrice) / (control.HistoryMax - control.HistoryMin);
                var max = 800;
                var min = 200;
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

        public static int GetEmptySellDivide(string symbolName, decimal nowPrice, int divide = 12)
        {
            try
            {
                var max = 52;
                var min = 6;
                var control = new DogControlDao().GetDogControl(symbolName);
                if (control == null || control.HistoryMax <= control.HistoryMin || nowPrice < control.HistoryMin)
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
    }
}
