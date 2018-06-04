using DogService.Dao;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DogService
{
    public class DogControlUtils
    {
        public static decimal? GetPredictPrice(string symbolName)
        {
            var control = new DogControlDao().GetDogControl(symbolName);
            if (control == null || control.PredictExpiredTime < DateTime.Now)
            {
                return null;
            }
            return control.PredictPrice;
        }

        public static decimal? GetEmptyPrice(string symbolName)
        {
            var control = new DogControlDao().GetDogControl(symbolName);
            if (control == null || control.EmptyExpiredTime < DateTime.Now)
            {
                return null;
            }
            return control.EmptyPrice;
        }

        public static decimal? GetAvgInputAmount(string symbolName)
        {
            var control = new DogControlDao().GetDogControl(symbolName);
            if (control == null || control.AvgInputExpiredTime < DateTime.Now)
            {
                return null;
            }
            return control.AvgInputAmount;
        }

        public static decimal? GetMaxInputPrice(string symbolName)
        {
            var control = new DogControlDao().GetDogControl(symbolName);
            if (control == null || control.MaxInputPriceExpiredTime < DateTime.Now)
            {
                return null;
            }
            return control.MaxInputPrice;
        }

        public static decimal GetLadderBuy(string symbolName, decimal defaultLadderBuyPercent = (decimal)1.03)
        {
            var control = new DogControlDao().GetDogControl(symbolName);
            if (control != null || control.LadderBuyExpiredTime > DateTime.Now)
            {
                defaultLadderBuyPercent = control.LadderBuyPercent;
            }
            return defaultLadderBuyPercent;
        }

        public static decimal GetLadderSell(string symbolName, decimal defaultLadderSellPercent = (decimal)1.04)
        {
            var control = new DogControlDao().GetDogControl(symbolName);
            if (control != null || control.LadderSellExpiredTime > DateTime.Now)
            {
                defaultLadderSellPercent = control.LadderSellPercent;
            }
            return defaultLadderSellPercent;
        }
    }
}
