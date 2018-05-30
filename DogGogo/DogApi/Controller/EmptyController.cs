using DogAccount;
using DogPlatform;
using DogRunService;
using DogService;
using DogService.DateTypes;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace DogApi.Controller
{
    public class EmptyController : ApiController
    {
        static ILog logger = LogManager.GetLogger(typeof(EmptyController));

        [HttpGet]
        [ActionName("shouge")]
        public async Task shouge(long orderId)
        {
            try
            {
                var dogEmptySell = new DogEmptySellDao().GetDogEmptySellBySellOrderId(orderId);
                if (dogEmptySell.IsFinished)
                {
                    return;
                }

                var dogEmptyBuyList = new DogEmptyBuyDao().ListDogEmptyBuyBySellOrderId(orderId);
                if (dogEmptyBuyList.Count > 0 && dogEmptyBuyList.Find(it => it.BuyState != StateConst.Canceled.ToString() && it.BuyState != StateConst.PartialFilled.ToString() && it.BuyState != StateConst.Filled.ToString()) != null)
                {
                    // 存在操作中的,则不操作
                    return;
                }

                CoinTrade.ShouGeEmpty(dogEmptySell);
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
                var dogEmptySell = new DogEmptySellDao().GetDogEmptySellBySellOrderId(orderId);
                if (dogEmptySell.IsFinished)
                {
                    return;
                }

                var dogEmptyBuyList = new DogEmptyBuyDao().ListDogEmptyBuyBySellOrderId(orderId);
                if (dogEmptyBuyList.Count > 0 && dogEmptyBuyList.Find(it => it.BuyState != StateConst.Canceled.ToString() && it.BuyState != StateConst.PartialFilled.ToString() && it.BuyState != StateConst.Filled.ToString()) != null)
                {
                    // 存在操作中的,则不操作
                    return;
                }

                CoinTrade.ShouGeEmpty(dogEmptySell, (decimal)1.01);
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
            }
        }

        [HttpGet]
        [ActionName("listEmptySellIsNotFinished")]
        public async Task<object> listEmptySellIsNotFinished(string symbolName)
        {
            try
            {
                return new DogEmptySellDao().ListDogEmptySellNotFinished(symbolName);
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
                return null;
            }
        }

        [HttpGet]
        [ActionName("delete")]
        public async Task Delete(long sellOrderId)
        {
            new DogEmptySellDao().Delete(sellOrderId);
        }

        [HttpGet]
        [ActionName("emptyInfo")]
        public async Task<object> EmptyInfo(string userName, string symbolName)
        {
            PlatformApi api = PlatformApi.GetInstance(userName);

            var accountInfo = api.GetAccountBalance(AccountConfigUtils.GetAccountConfig(userName).MainAccountId);
            var balanceItem = accountInfo.Data.list.Find(it => it.currency == symbolName);

            var list = new DogMoreBuyDao().listMoreBuyIsNotFinished(symbolName);
            var totalQuantity = (decimal)0;
            list.ForEach(it => totalQuantity += it.BuyQuantity);

            return balanceItem;
        }

        public async Task DoEmpty(string userName, string symbolName)
        {
            // 加入一个做空指令， 来决定是否做空。
        }
    }
}
