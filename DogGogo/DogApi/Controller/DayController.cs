using DogService;
using DogService.Dao;
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

        public DogMoreStatisticsDao DogMoreStatisticsDao { get; set; }

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
                throw ex;
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
                logger.Error(ex.Message, ex);
                throw ex;
            }
        }

        [HttpGet]
        [ActionName("kline")]
        public async Task<object> kline(string userName, string symbolName, string quoteCurrency)
        {
            try
            {
                var begin = DateTime.Now.AddMinutes(-60 * 24 * 7);
                var end = DateTime.Now.AddMinutes(10);

                var buyList = await new DogMoreStatisticsDao().ListBuy(userName, symbolName, quoteCurrency, begin, end);
                var sellList = await new DogMoreStatisticsDao().ListSell(userName, symbolName, quoteCurrency, begin, end);
                var klineList = new KlineDao().ListTodayKline(symbolName, quoteCurrency, begin, end);
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
                throw ex;
            }
        }

        [HttpGet]
        [ActionName("symbolKline")]
        public async Task<object> symbolKline(string userName, string symbolName, string quoteCurrency, int day)
        {
            try
            {
                var begin = DateTime.Now.AddDays(-60 * 24 * day);
                var end = DateTime.Now;

                var buyList = await new DogMoreStatisticsDao().ListBuy(userName, symbolName, quoteCurrency, begin, end);
                var sellList = await new DogMoreStatisticsDao().ListSell(userName, symbolName, quoteCurrency, begin, end);
                var klineList = new KlineDao().ListTodayKline(symbolName, quoteCurrency, begin, end);
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
                throw ex;
            }
        }

        [HttpGet]
        [ActionName("getNeedEmpty")]
        public async Task<object> getNeedEmpty(string quoteCurrency)
        {
            try
            {
                var list = new List<string>();

                var nowPriceList = new DogNowPriceDao().ListDogNowPrice(quoteCurrency);

                foreach (var nowPriceItem in nowPriceList)
                {
                    if (nowPriceItem.SymbolName == "gxs" || nowPriceItem.SymbolName == "btc")
                    {
                        continue;
                    }
                    if (nowPriceItem.QuoteCurrency == "btc")
                    {
                        if (nowPriceItem.SymbolName == "btm" || nowPriceItem.SymbolName == "iost" || nowPriceItem.SymbolName == "icx")
                        {
                            continue;
                        }
                    }
                    if (nowPriceItem.QuoteCurrency == "eth")
                    {
                        if (nowPriceItem.SymbolName == "ela" || nowPriceItem.SymbolName == "smt" || nowPriceItem.SymbolName == "mana")
                        {
                            continue;
                        }
                    }

                    var nowPrice = nowPriceList.Find(it => it.SymbolName == nowPriceItem.SymbolName).NowPrice;

                    {
                        // 一个月内最上面
                        var maxPrice = new KlineDao().GetMaxPrice(quoteCurrency, nowPriceItem.SymbolName, DateTime.Now.AddDays(-30));
                        var minPrice = new KlineDao().GetMinPrice(quoteCurrency, nowPriceItem.SymbolName, DateTime.Now.AddDays(-30));
                        if (nowPrice >= maxPrice)
                        {
                            list.Add(nowPriceItem.SymbolName);
                            continue;
                        }
                        if (nowPrice <= minPrice)
                        {
                            continue;
                        }
                        if ((nowPrice - minPrice) > (maxPrice - nowPrice) * 2)
                        {
                            list.Add(nowPriceItem.SymbolName);
                        }
                    }

                    {
                        // 一周内最上面
                        var maxPrice = new KlineDao().GetMaxPrice(quoteCurrency, nowPriceItem.SymbolName, DateTime.Now.AddDays(-7));
                        var minPrice = new KlineDao().GetMinPrice(quoteCurrency, nowPriceItem.SymbolName, DateTime.Now.AddDays(-7));
                        if (nowPrice >= maxPrice)
                        {
                            list.Add(nowPriceItem.SymbolName);
                            continue;
                        }
                        if (nowPrice <= minPrice)
                        {
                            continue;
                        }
                        if ((nowPrice - minPrice) > (maxPrice - nowPrice) * 3)
                        {
                            list.Add(nowPriceItem.SymbolName);
                        }
                    }

                    {
                        // 一天内最上面
                        var maxPrice = new KlineDao().GetMaxPrice(quoteCurrency, nowPriceItem.SymbolName, DateTime.Now.AddDays(-1));
                        var minPrice = new KlineDao().GetMinPrice(quoteCurrency, nowPriceItem.SymbolName, DateTime.Now.AddDays(-1));
                        if (nowPrice >= maxPrice)
                        {
                            list.Add(nowPriceItem.SymbolName);
                            continue;
                        }
                        if (nowPrice <= minPrice)
                        {
                            continue;
                        }
                        if ((nowPrice - minPrice) > (maxPrice - nowPrice) * 4)
                        {
                            list.Add(nowPriceItem.SymbolName);
                        }
                    }
                }

                return list;
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
                throw ex;
            }
        }
    }
}
