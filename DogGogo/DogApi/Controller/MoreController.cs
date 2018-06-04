using DogPlatform.Model;
using DogRunService;
using DogService;
using DogService.Dao;
using DogService.DateTypes;
using log4net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace DogApi.Controller
{
    public class MoreController : ApiController
    {
        static ILog logger = LogManager.GetLogger(typeof(MoreController));

        [HttpGet]
        [ActionName("shouge")]
        public async Task shouge(long orderId)
        {
            try
            {
                var dogMoreBuy = new DogMoreBuyDao().GetDogMoreBuyByBuyOrderId(orderId);
                if (dogMoreBuy.IsFinished)
                {
                    return;
                }

                var dogMoreSellList = new DogMoreSellDao().ListDogMoreSellByBuyOrderId(orderId);
                if (dogMoreSellList.Count > 0 &&
                    dogMoreSellList.Find(it =>
                        it.SellState != StateConst.Canceled.ToString()
                        && it.SellState != StateConst.PartialFilled.ToString()
                        && it.SellState != StateConst.Filled.ToString()
                    ) != null)
                {
                    // 存在操作中的,则不操作
                    return;
                }

                CoinTrade.ShouGeMore(dogMoreBuy);
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
            }
        }

        [HttpGet]
        [ActionName("forceShouge")]
        public async Task forceShouge(long orderId)
        {
            try
            {
                var dogMoreBuy = new DogMoreBuyDao().GetDogMoreBuyByBuyOrderId(orderId);
                if (dogMoreBuy.IsFinished)
                {
                    return;
                }

                var dogMoreSellList = new DogMoreSellDao().ListDogMoreSellByBuyOrderId(orderId);
                if (dogMoreSellList.Count > 0 &&
                    dogMoreSellList.Find(it =>
                        it.SellState != StateConst.Canceled.ToString()
                        && it.SellState != StateConst.PartialFilled.ToString()
                        && it.SellState != StateConst.Filled.ToString()
                    ) != null)
                {
                    // 存在操作中的,则不操作
                    return;
                }

                CoinTrade.ShouGeMore(dogMoreBuy, (decimal)1.01);
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
            }
        }

        [HttpGet]
        [ActionName("listMoreBuyIsNotFinished")]
        public async Task<object> listMoreBuyIsNotFinished(string symbolName)
        {
            try
            {
                return new DogMoreBuyDao().listMoreBuyIsNotFinished(symbolName);
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
                return null;
            }
        }

        [HttpGet]
        [ActionName("listMoreBuyIsFinished")]
        public async Task<object> listMoreBuyIsFinished(string userName, string symbolName)
        {
            try
            {
                return new DogMoreBuyDao().listDogMoreBuyIsFinished(userName, symbolName);
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
                return null;
            }
        }

        /// <summary>
        /// 查看一个购买后出售的详情。
        /// </summary>
        /// <param name="buyOrderId"></param>
        /// <returns></returns>
        [HttpGet]
        [ActionName("getMoreBuyDetail")]
        public object GetMoreBuyDetail(long buyOrderId)
        {
            try
            {
                var dogMoreBuy = new DogMoreBuyDao().GetByBuyOrderId(buyOrderId);
                var orderMatchResult = JsonConvert.DeserializeObject<HBResponse<List<OrderMatchResult>>>(dogMoreBuy.BuyOrderMatchResults);
                var buyQuantity = (decimal)0;
                var buyAmount = (decimal)0;
                var buyFees = (decimal)0;
                foreach (var item in orderMatchResult.Data)
                {
                    buyAmount += item.FilledAmount * item.price;
                    buyQuantity += item.FilledAmount;
                    buyFees += item.FilledFees;
                }

                // 交易量，交易总额，  出售总额 出售数量， 
                var sellQuantity = (decimal)0;
                var sellAmount = (decimal)0;
                var sellFees = (decimal)0;
                var dogMoreSellList = new DogMoreSellDao().ListDogMoreSellByBuyOrderId(buyOrderId);

                foreach (var sell in dogMoreSellList)
                {
                    var sellOrderMatchResult = JsonConvert.DeserializeObject<HBResponse<List<OrderMatchResult>>>(sell.SellOrderMatchResults);
                    foreach (var item in sellOrderMatchResult.Data)
                    {
                        sellAmount += item.FilledAmount * item.price;
                        sellQuantity += item.FilledAmount;
                        sellFees += item.FilledFees;
                    }
                }
                return new
                {
                    buyOrderId,
                    symbolName = dogMoreBuy.SymbolName,
                    buyQuantity,
                    buyAmount,
                    buyFees,
                    sellAmount,
                    sellQuantity,
                    sellFees,
                    usdt = sellAmount - buyAmount - sellFees,
                    baseSymbol = buyQuantity - sellQuantity - buyFees
                };
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
                return null;
            }
        }

        [HttpGet]
        [ActionName("delete")]
        public async Task Delete(long buyOrderId)
        {
            new DogMoreBuyDao().Delete(buyOrderId);
        }
    }
}
