﻿using DogApi.DTO;
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
    public class DayController:ApiController
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
                var pigMoreSellList = await new DogMoreStatisticsDao().ListTodayBuy(userName);

                var buyCount = 0;
                var sellCount = 0;
                var buyAmount = (decimal)0.0;
                var sellAmount = (decimal)0.0;
                var sellEarnings = (decimal)0.0;
                var list = new List<TodayTradeDTO>();
                pigMoreBuyList.ForEach(it =>
                {
                    if (it.BuyDate >= Utils.GetSmallestOfTheDate(DateTime.Now))
                    {
                        buyCount++;
                        buyAmount += it.BuyQuantity * it.BuyTradePrice;
                    }
                    if (it.SOrderId > 0)
                    {
                        sellCount++;
                        sellAmount += it.SQuantity * it.STradeP;
                        sellEarnings += it.SQuantity * it.STradeP - it.BQuantity * it.BTradeP;
                    }
                    list.Add(new TodayTradeDTO()
                    {
                        Name = it.SymbolName,
                        BDate = it.BuyDate,
                        BQuantity = it.BuyQuantity,
                        BTradeP = it.BuyTradePrice,
                        SQuantity = it.SQuantity,
                        SDate = it.SDate,
                        STradeP = it.STradeP,
                    });
                });
                return new
                {
                    buyCount,
                    sellCount,
                    buyAmount,
                    sellAmount,
                    sellEarnings,
                    list
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
