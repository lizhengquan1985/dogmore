using DogPlatform.Model;
using DogRunService.DataTypes;
using DogService;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DogRunService
{
    public class JudgeSellUtils
    {
        static ILog logger = LogManager.GetLogger(typeof(JudgeSellUtils));

        public static bool CheckCanSellForHuiDiao(decimal nowPrice, decimal nearHighPrice)
        {
            return nowPrice * (decimal)1.005 < nearHighPrice && nowPrice * (decimal)1.06 > nearHighPrice;
        }

        public static bool CheckCanSell(decimal buyPrice, decimal afterBuyHighClosePrice, decimal nowPrice, decimal gaoyuPercentSell = (decimal)1.03, bool needHuitou = true)
        {
            if(nowPrice < buyPrice)
            {
                return false;
            }

            if (nowPrice < buyPrice * gaoyuPercentSell || nowPrice < buyPrice * (decimal)1.02)
            {
                // 如果不高于 2% 没有意义, 其中 gaoyuPercentSell用来管控的.
                return false;
            }

            if (nowPrice * (decimal)1.005 < afterBuyHighClosePrice && needHuitou)
            {
                // 表示回头趋势， 暂时定为 0.5% 就有回头趋势
                return true;
            }

            if (nowPrice * (decimal)1.05 < afterBuyHighClosePrice && !needHuitou)
            {
                return true;
            }

            return false;
        }

        public static bool CheckCanMakeEmpty(decimal afterBuyHighClosePrice, decimal nowPrice, decimal gaoyuPercentSell = (decimal)1.03)
        {
            if (nowPrice * (decimal)1.005 < afterBuyHighClosePrice)
            {
                // 表示回头趋势， 暂时定为 0.5% 就有回头趋势
                return true;
            }

            if (nowPrice * (decimal)1.001 < afterBuyHighClosePrice)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 获取购买之后的高价
        /// </summary>
        /// <param name="comparePrice"></param>
        /// <param name="compareDate"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static decimal GetHigherPriceAfterByDate(decimal comparePrice, DateTime compareDate, List<HistoryKline> data)
        {
            decimal higher = new decimal(0);

            try
            {
                foreach (var item in data)
                {
                    if (Utils.GetDateById(item.Id) < compareDate)
                    {
                        continue;
                    }

                    if (item.Open > higher)
                    {
                        higher = item.Close;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
                Console.WriteLine("AnalyzeNeedSell over  " + ex.Message);
            }
            return higher;
        }

        public static decimal CalcSellQuantityForMoreShouge(decimal buyQuantity, decimal buyTradePrice, decimal nowPrice, CommonSymbols symbol)
        {

            decimal sellQuantity = buyQuantity * (decimal)0.99;
            sellQuantity = decimal.Round(sellQuantity, symbol.AmountPrecision);

            if (buyQuantity > sellQuantity && buyQuantity * buyTradePrice < sellQuantity * nowPrice)
            {
                return sellQuantity;
            }

            var newSellQuantity = sellQuantity;
            if (newSellQuantity == buyQuantity)
            {
                if (symbol.AmountPrecision == 4)
                {
                    newSellQuantity -= (decimal)0.0001;
                }
                else if (symbol.AmountPrecision == 3)
                {
                    newSellQuantity -= (decimal)0.001;
                }
                else if (symbol.AmountPrecision == 2)
                {
                    newSellQuantity -= (decimal)0.01;
                }
                else if (symbol.AmountPrecision == 1)
                {
                    newSellQuantity -= (decimal)0.1;
                }
            }
            if (buyQuantity > newSellQuantity && buyQuantity * buyTradePrice < newSellQuantity * nowPrice)
            {
                return newSellQuantity;
            }
            return sellQuantity;
        }
    }
}
