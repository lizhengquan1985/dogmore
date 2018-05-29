using DogPlatform.Model;
using DogRunService;
using DogService;
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
        [HttpGet]
        [ActionName("getMoreBuyDetail")]
        public async Task<object> GetMoreBuyDetail(long buyOrderId)
        {
            try
            {
                var dogMoreBuy = new DogMoreBuyDao().GetByBuyOrderId(buyOrderId);
                var orderDetail = JsonConvert.DeserializeObject<HBResponse<OrderDetail>>(dogMoreBuy.BuyOrderDetail);
                //orderDetail.Data.

                var dogMoreSellList = new DogMoreSellDao().ListDogMoreSellByBuyOrderId(buyOrderId);
                return new { orderDetail , dogMoreSellList };
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
                return null;
            }
        }
    }
}
