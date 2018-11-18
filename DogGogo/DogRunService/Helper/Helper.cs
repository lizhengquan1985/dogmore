using DogPlatform.Model;
using DogService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DogRunService.Helper
{
    public class CommonHelper
    {
        /// <summary>
        /// 空单收割时候,计算收割购入的数量
        /// </summary>
        /// <param name="sellQuantity"></param>
        /// <param name="sellTradePrice"></param>
        /// <param name="nowPrice"></param>
        /// <returns></returns>
        public static decimal CalcBuyQuantityForEmptyShouge(decimal sellQuantity, decimal sellTradePrice, decimal nowPrice, CommonSymbol symbol)
        {
            if (nowPrice >= sellTradePrice * (decimal)0.99)
            {
                throw new Exception("收割空价格不合理");
            }
            var position = DogControlUtils.GetLadderPosition(symbol.BaseCurrency, symbol.QuoteCurrency, nowPrice);
            position += (decimal)0.16;
            if (position <= (decimal)0.42)
            {
                position = (decimal)0.42;
            }
            if (position >= (decimal)0.82)
            {
                position = (decimal)0.82;
            }

            decimal buyQuantity = sellQuantity * sellTradePrice / nowPrice;
            buyQuantity = buyQuantity - (buyQuantity - sellQuantity) * position;
            buyQuantity = decimal.Round(buyQuantity, symbol.AmountPrecision);
            if (buyQuantity > sellQuantity && buyQuantity * nowPrice < sellQuantity * sellTradePrice)
            {
                return buyQuantity;
            }

            var newBuyQuantity = buyQuantity;
            if (newBuyQuantity == sellQuantity)
            {
                if (symbol.AmountPrecision == 1)
                {
                    newBuyQuantity += (decimal)0.1;
                }
                if (symbol.AmountPrecision == 2)
                {
                    newBuyQuantity += (decimal)0.01;
                }
                if (symbol.AmountPrecision == 3)
                {
                    newBuyQuantity += (decimal)0.001;
                }
                if (symbol.AmountPrecision == 4)
                {
                    newBuyQuantity += (decimal)0.0001;
                }
            }
            if (newBuyQuantity > sellQuantity && newBuyQuantity * nowPrice <= sellQuantity * sellTradePrice)
            {
                return newBuyQuantity;
            }
            throw new Exception("计算buyQuantity不合理");
        }

        /// <summary>
        /// 检查余额是否充足
        /// </summary>
        /// <param name="quoteCurrency"></param>
        /// <param name="balance"></param>
        /// <param name="notShougeEmptySellAmount"></param>
        /// <returns></returns>
        public static bool CheckBalanceForDoMore(string quoteCurrency, decimal balance, decimal notShougeEmptySellAmount)
        {
            if (notShougeEmptySellAmount >= balance)
            {
                return false;
            }
            if (quoteCurrency == "usdt" && balance - notShougeEmptySellAmount < (decimal)2)
            {
                return false;
            }
            if (quoteCurrency == "btc" && balance - notShougeEmptySellAmount < (decimal)0.0004)
            {
                return false;
            }
            if (quoteCurrency == "eth" && balance - notShougeEmptySellAmount < (decimal)0.008)
            {
                return false;
            }
            if (quoteCurrency == "ht" && balance - notShougeEmptySellAmount < (decimal)0.9)
            {
                return false;
            }

            return true;
        }
    }
}
