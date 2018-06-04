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

        public static decimal GetLadderBuy(string symbolName, decimal defaultLadderBuyPercent = (decimal)1.03)
        {
            try
            {
                var control = new DogControlDao().GetDogControl(symbolName);
                if (control != null && control.LadderBuyExpiredTime > DateTime.Now)
                {
                    defaultLadderBuyPercent = control.LadderBuyPercent;
                }
                defaultLadderBuyPercent = Math.Max(defaultLadderBuyPercent, (decimal)1.02);
                defaultLadderBuyPercent = Math.Min(defaultLadderBuyPercent, (decimal)1.08);
                return defaultLadderBuyPercent;
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
                return defaultLadderBuyPercent;
            }
        }

        public static decimal GetLadderSell(string symbolName, decimal defaultLadderSellPercent = (decimal)1.04)
        {
            try
            {
                var control = new DogControlDao().GetDogControl(symbolName);
                if (control != null && control.LadderSellExpiredTime > DateTime.Now)
                {
                    defaultLadderSellPercent = control.LadderSellPercent;
                }
                defaultLadderSellPercent = Math.Max(defaultLadderSellPercent, (decimal)1.025);
                defaultLadderSellPercent = Math.Min(defaultLadderSellPercent, (decimal)1.08);
                return defaultLadderSellPercent;
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
                return defaultLadderSellPercent;
            }
        }
    }
}
