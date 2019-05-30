using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DogRunService.Helper
{
    public class TradeStateUtils
    {
        static ILog logger = LogManager.GetLogger(typeof(TradeStateUtils));

        public static void Begin()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        CoinTrade.CheckBuyOrSellState();
                    }
                    catch (Exception ex)
                    {
                        logger.Error("查看购买以及出售结果" + ex.Message, ex);
                    }
                    Thread.Sleep(1000 * 30);
                }
            });
        }
    }
}
