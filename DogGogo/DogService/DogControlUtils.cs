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
                var control = new DogControlDao().GetDogControl(symbolName);
                if (control != null && control.LadderBuyExpiredTime > DateTime.Now)
                {
                    defaultLadderBuyPercent = control.LadderBuyPercent;
                }
                else if (control != null && nowPrice <= control.HistoryMax && nowPrice >= control.HistoryMin)
                {
                    // 计算出来阶梯
                    var percent = (control.HistoryMax - nowPrice) / (control.HistoryMax - control.HistoryMin);
                    var max = (decimal)1.12;
                    var min = (decimal)1.05;
                    defaultLadderBuyPercent = max - Convert.ToInt32(percent * (max - min));
                    if (defaultLadderBuyPercent > max)
                    {
                        defaultLadderBuyPercent = max;
                    }
                    if (defaultLadderBuyPercent < min)
                    {
                        defaultLadderBuyPercent = min;
                    }
                }
                defaultLadderBuyPercent = Math.Max(defaultLadderBuyPercent, (decimal)1.05);
                defaultLadderBuyPercent = Math.Min(defaultLadderBuyPercent, (decimal)1.12);
                return defaultLadderBuyPercent;
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
                return defaultLadderBuyPercent;
            }
        }

        public static decimal GetLadderSell(string symbolName, decimal defaultLadderSellPercent = (decimal)1.045)
        {
            try
            {
                var control = new DogControlDao().GetDogControl(symbolName);
                if (control != null && control.LadderSellExpiredTime > DateTime.Now)
                {
                    defaultLadderSellPercent = control.LadderSellPercent;
                }
                defaultLadderSellPercent = Math.Max(defaultLadderSellPercent, (decimal)1.035);
                defaultLadderSellPercent = Math.Min(defaultLadderSellPercent, (decimal)1.12);
                return defaultLadderSellPercent;
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
                return defaultLadderSellPercent;
            }
        }

        public static int GetRecommendDivide(string symbolName, decimal nowPrice, int divide = 250)
        {
            try
            {
                var control = new DogControlDao().GetDogControl(symbolName);
                if (control == null || control.HistoryMax <= control.HistoryMin || nowPrice > control.HistoryMax || nowPrice < control.HistoryMin)
                {
                    return divide;
                }

                var percent = (control.HistoryMax - nowPrice) / (control.HistoryMax - control.HistoryMin);
                var max = 300;
                var min = 220;
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
    }
}
