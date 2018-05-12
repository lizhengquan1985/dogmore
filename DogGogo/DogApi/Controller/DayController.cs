using DogService;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace DogApi.Controller
{
    public class DayController : ApiController
    {
        static ILog logger = LogManager.GetLogger(typeof(DayController));

        public DogMoreStatisticsDao PigMoreStatisticsDao { get; set; }

        /// <summary>
        /// 今日交易， 购买数量，出售数量， 购买总额，出售总额，每条记录（）。
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [ActionName("listTodayTrade")]
        public async Task<object> ListTodayTrade(string userName)
        {
            try
            {
                var pigMoreBuyList = await new DogMoreStatisticsDao().ListTodayBuy(userName);
                var pigMoreSellList = await new DogMoreStatisticsDao().ListTodaySell(userName);

                var buyCount = 0;
                var sellCount = 0;
                var buyAmount = (decimal)0.0;
                var sellAmount = (decimal)0.0;
                var sellEarnings = (decimal)0.0;
                pigMoreBuyList.ForEach(it =>
                {
                    if (it.BuyDate >= Utils.GetSmallestOfTheDate(DateTime.Now))
                    {
                        buyCount++;
                        buyAmount += it.BuyQuantity * it.BuyTradePrice;
                    }
                });
                var buyList = await new DogMoreStatisticsDao().ListBuyByBuyOrderId(pigMoreSellList.Select(it => it.BuyOrderId).ToList());
                pigMoreSellList.ForEach(it =>
                {
                    sellCount++;
                    sellAmount += it.SellQuantity * it.SellTradePrice;
                });
                buyList.ForEach(it =>
                {
                    var sellList = pigMoreSellList.FindAll(item => it.BuyOrderId == it.BuyOrderId);
                    sellEarnings += sellList.Sum(s => s.SellQuantity * s.SellTradePrice) - it.BuyQuantity * it.BuyTradePrice;
                });
                return new
                {
                    buyCount,
                    sellCount,
                    buyAmount,
                    sellAmount,
                    sellEarnings,
                    buyList = pigMoreBuyList.Select(it => new
                    {
                        it.BuyQuantity,
                        it.BuyTradePrice,
                        it.BuyDate,
                        it.IsFinished,
                        SellPrice = pigMoreSellList.FindAll(s => s.BuyOrderId == it.BuyOrderId).Min(s => s.SellTradePrice)
                    }),
                    sellList = pigMoreSellList.Select(it => new
                    {
                        it.SellQuantity,
                        it.SellTradePrice,
                        it.SellDate,
                        it.SellState,
                        BuyPrice = buyList.Find(b => b.BuyOrderId == it.BuyOrderId).BuyTradePrice
                    })
                };
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
                return ex.Message;
            }
        }

        [HttpGet]
        [ActionName("listPigMoreStatisticsDay")]
        public async Task<object> ListPigMoreStatisticsDay(string userName)
        {
            try
            {
                return await new DogMoreStatisticsDayDao().ListStatisticsData(userName);
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        [HttpGet]
        [ActionName("kline")]
        public async Task<object> kline(string userName, string name, DateTime date)
        {
            try
            {
                var begin = date.AddMinutes(-60 * 24);
                var end = date.AddMinutes(10);

                var buyList = await new DogMoreStatisticsDao().ListBuy(userName, name, begin, end);
                var sellList = await new DogMoreStatisticsDao().ListSell(userName, name, begin, end);
                var klineList = new KlineDao().ListKline(name, begin, end);
                return new
                {
                    buyList = buyList.Select(it => new { it.BuyDate, it.BuyTradePrice }),
                    sellList = sellList.Select(it => new { it.SellDate, it.SellTradePrice }),
                    klineList = klineList.Select(it => new { it.Close, it.Id, it.High })
                };
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
                return null;
            }
        }
    }
}
