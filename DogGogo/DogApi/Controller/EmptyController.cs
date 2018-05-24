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
                if(dogEmptyBuyList.Count > 0 && dogEmptyBuyList.Find(it=>it.BuyState != StateConst.Canceled.ToString() && it.BuyState != StateConst.PartialFilled.ToString() && it.BuyState != StateConst.Filled.ToString()) != null)
                {
                    // 存在操作中的,则不操作
                }

                CoinTrade.ShouGeEmpty(dogEmptySell);
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
            }
        }
    }
}
