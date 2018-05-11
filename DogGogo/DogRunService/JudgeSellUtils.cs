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

        public static bool CheckCanSell(decimal buyPrice, decimal nearHigherOpen, decimal nowPrice, decimal gaoyuPercentSell = (decimal)1.03, bool needHuitou = true)
        {
            //item.BuyPrice, higher, itemNowOpen
            // if (item.BuyPrice * (decimal)1.05 < higher && itemNowOpen * (decimal)1.005 < higher)
            if (nowPrice < buyPrice * gaoyuPercentSell)
            {
                // 如果不高于 3% 没有意义
                return false;
            }

            if (nowPrice * (decimal)1.005 < nearHigherOpen && needHuitou)
            {
                // 表示回头趋势， 暂时定为 0.5% 就有回头趋势
                return true;
            }

            if (nowPrice * (decimal)1.001 < nearHigherOpen && !needHuitou)
            {
                return true;
            }

            return false;
        }

        public static decimal AnalyzeNeedSell(decimal comparePrice, DateTime compareDate, string coin, string toCoin, List<HistoryKline> data)
        {
            decimal higher = new decimal(0);

            try
            {
                List<FlexPoint> flexPointList = new List<FlexPoint>();

                decimal closeHigh = data[0].Close;
                decimal closeLow = data[0].Close;
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
    }
}
