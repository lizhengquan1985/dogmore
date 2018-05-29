using DogPlatform;
using DogService.Dao;
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
    public class ControlController : ApiController
    {
        static ILog logger = LogManager.GetLogger(typeof(ControlController));

        [HttpPost]
        [ActionName("new")]
        public async Task Create([FromBody] DogControl dogControl)
        {
            try
            {
                await new DogControlDao().CreateDogControl(dogControl);

            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
            }
        }

        [HttpGet]
        [ActionName("list")]
        public async Task<object> List()
        {
            try
            {
                var res = await new DogControlDao().ListDogControl();
                var symbols = CoinUtils.GetAllCommonSymbols();
                var baseSymbols = symbols.Select(it => it.BaseCurrency).ToList();
                foreach (var itemSymbol in baseSymbols)
                {
                    if (res.Find(it => it.SymbolName == itemSymbol) != null)
                    {
                        continue;
                    }
                    res.Add(new DogControl()
                    {
                        SymbolName = itemSymbol,
                        AvgInputExpiredTime = DateTime.Now,
                        EmptyExpiredTime = DateTime.Now,
                        MaxInputPriceExpiredTime = DateTime.Now,
                        PredictExpiredTime = DateTime.Now,
                        CreateTime = DateTime.Now
                    });
                }
                return res;
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
                return null;
            }
        }

    }
}
