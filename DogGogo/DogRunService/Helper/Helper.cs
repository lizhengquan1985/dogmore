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
        /// <param name="amountPrecision"></param>
        /// <returns></returns>
        public static decimal CalcBuyQuantityForEmptyShouge(decimal sellQuantity, decimal sellTradePrice, decimal nowPrice, int amountPrecision, CommonSymbols symbol)
        {
            if (nowPrice >= sellTradePrice * (decimal)0.99)
            {
                throw new Exception("收割空价格不合理");
            }
            var position = DogControlUtils.GetLadderPosition(symbol.BaseCurrency, nowPrice);
            if (position <= 0)
            {
                position = (decimal)0.3;
            }
            if (position >= 1)
            {
                position = (decimal)0.7;
            }

            decimal buyQuantity = sellQuantity * sellTradePrice / nowPrice;
            buyQuantity = buyQuantity - (buyQuantity - sellQuantity) * position;
            buyQuantity = decimal.Round(buyQuantity, amountPrecision);
            if (buyQuantity > sellQuantity && buyQuantity * nowPrice < sellQuantity * sellTradePrice)
            {
                return buyQuantity;
            }

            var newBuyQuantity = buyQuantity;
            if (newBuyQuantity == sellQuantity)
            {
                if (amountPrecision == 1)
                {
                    newBuyQuantity += (decimal)0.1;
                }
                if (amountPrecision == 2)
                {
                    newBuyQuantity += (decimal)0.01;
                }
                if (amountPrecision == 3)
                {
                    newBuyQuantity += (decimal)0.001;
                }
                if (amountPrecision == 4)
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
    }
}
