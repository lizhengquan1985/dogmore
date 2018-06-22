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
        public static decimal CalcBuyQuantityForEmptyShouge(decimal sellQuantity, decimal sellTradePrice, decimal nowPrice, int amountPrecision)
        {
            decimal buyQuantity = sellQuantity * (decimal)1.005;
            buyQuantity = decimal.Round(buyQuantity, amountPrecision);
            if (buyQuantity > sellQuantity && buyQuantity * nowPrice <= sellQuantity * sellTradePrice)
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
            return buyQuantity;
        }
    }
}
