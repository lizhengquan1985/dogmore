using DogPlatform;
using DogPlatform.Model;
using DogRunService.DataTypes;
using DogService;
using DogService.DateTypes;
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

        public static decimal GetPercent(decimal nowPrice, decimal nearHighPrice, long dateId, decimal percent)
        {
            if (nowPrice > nearHighPrice)
            {
                return percent;
            }

            if (nowPrice * (decimal)1.15 < nearHighPrice && Utils.GetDateById(dateId) > DateTime.Now.AddMinutes(-30))
            {
                // 急速下降了10%, 则 4%都可以出售了
                return (decimal)1.03;
            }

            if (nowPrice * (decimal)1.10 < nearHighPrice && Utils.GetDateById(dateId) > DateTime.Now.AddMinutes(-30))
            {
                // 急速下降了10%, 则 4%都可以出售了
                return (decimal)1.04;
            }

            if (nowPrice * (decimal)1.08 < nearHighPrice && Utils.GetDateById(dateId) > DateTime.Now.AddMinutes(-30))
            {
                // 急速下降了10%, 则 4%都可以出售了
                return (decimal)1.045;
            }

            if (nowPrice * (decimal)1.05 < nearHighPrice && Utils.GetDateById(dateId) > DateTime.Now.AddMinutes(-30))
            {
                // 急速下降了10%, 则 4%都可以出售了
                return (decimal)1.05;
            }

            return percent;
        }

        public static bool CheckCanSellForHuiDiao(DogMoreBuy dogMoreBuy, decimal nowPrice, decimal nearHighPrice)
        {
            var minHuidiao = (decimal)1.005;
            var maxHuidiao = (decimal)1.03;
            var huidiao = minHuidiao;
            var upPercent = nowPrice / dogMoreBuy.BuyTradePrice;
            if (upPercent <= (decimal)1.02)
            {
                // 这个太差了吧.
                return false;
            }

            huidiao = 1 + ((nowPrice / dogMoreBuy.BuyTradePrice) - 1) / 10;
            huidiao = Math.Max(huidiao, minHuidiao);
            huidiao = Math.Min(huidiao, maxHuidiao);

            if (nowPrice > nearHighPrice)
            {
                return true;
            }
            return nowPrice * huidiao < nearHighPrice && nowPrice * upPercent > nearHighPrice;
        }

        public static bool CheckCanSell(decimal buyPrice, decimal afterBuyHighClosePrice, decimal nowPrice, decimal gaoyuPercentSell = (decimal)1.03, bool needHuitou = true)
        {
            if (nowPrice < buyPrice)
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

        public static decimal CalcSellQuantityForMoreShouge(decimal buyQuantity, decimal buyTradePrice, decimal nowPrice, CommonSymbol symbol)
        {
            if (nowPrice <= buyTradePrice * (decimal)1.01)
            {
                throw new Exception("收割多价格不合理");
            }
            var position = DogControlUtils.GetLadderPosition(symbol.BaseCurrency, symbol.QuoteCurrency, nowPrice);
            position += (decimal)0.15;
            if (position <= (decimal)0.45)
            {
                position = (decimal)0.45;
            }
            if (position >= (decimal)0.85)
            {
                position = (decimal)0.85;
            }
            // 计算啥
            decimal sellQuantity = buyQuantity * buyTradePrice / nowPrice;
            sellQuantity = sellQuantity + (buyQuantity - sellQuantity) * position;
            sellQuantity = decimal.Round(sellQuantity, symbol.AmountPrecision);
            Console.WriteLine($" ------------ {buyQuantity}, {sellQuantity}, {symbol.AmountPrecision}");

            //if (buyQuantity > sellQuantity && buyQuantity * buyTradePrice < sellQuantity * nowPrice)
            //{
            //    return sellQuantity;
            //}

            if (sellQuantity == buyQuantity && symbol.BaseCurrency == "bsv" && symbol.QuoteCurrency == "usdt")
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
                else if (symbol.AmountPrecision == 0)
                {
                    newSellQuantity -= (decimal)1;
                }
            }

            if (!CoinUtils.IsBiggerThenLeast(symbol.BaseCurrency, symbol.QuoteCurrency, sellQuantity))
            {
                newSellQuantity = CoinUtils.GetLeast(symbol.BaseCurrency, symbol.QuoteCurrency);
            }

            if (buyQuantity > newSellQuantity && buyQuantity * buyTradePrice < newSellQuantity * nowPrice)
            {
                return newSellQuantity;
            }

            if (symbol.BaseCurrency == "xrp")
            {
                return decimal.Round(buyQuantity, symbol.AmountPrecision);
            }

            throw new Exception($"计算sellquantity不合理, buyQuantity:{buyQuantity},newSellQuantity:{newSellQuantity}， 没有赚头");
        }
    }
}
