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
    public class MoreController : ApiController
    {
        static ILog logger = LogManager.GetLogger(typeof(MoreController));

        [HttpGet]
        [ActionName("shouge")]
        public async Task shouge(long orderId)
        {
            try
            {
                var dogEmptySell = new DogMoreBuyDao().GetDogMoreBuyByBuyOrderId(orderId);
                if (dogEmptySell.IsFinished)
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

                CoinTrade.ShouGeEmpty(dogEmptySell);
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
            }
        }
    }
