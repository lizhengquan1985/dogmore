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
                throw ex;
            }
        }

        [HttpGet]
        [ActionName("list")]
        public async Task<object> List()
        {
            try
            {
                return await new DogControlDao().ListDogControl();
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
                throw ex;
            }
        }

        [HttpPut]
        [ActionName("setUnvalid")]
        public async Task SetUnvalid(string symbolName)
        {
            try
            {
                await new DogControlDao().SetUnvalid(symbolName);
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
                throw ex;
            }
        }

        [HttpGet]
        [ActionName("refreshHistoryMaxMin")]
        public async Task RefreshHistoryMaxMin(string symbolName)
        {
            try
            {
                PlatformApi api = PlatformApi.GetInstance("xx");
                var period = "4hour";
                var klines = api.GetHistoryKline(symbolName + "usdt", period, 1000);
                var min = decimal.MaxValue;
                var max = decimal.MinValue;
                foreach (var item in klines)
                {
                    if (item.Low < min)
                    {
                        min = item.Low;
                    }
                    if (item.High > max)
                    {
                        max = item.High;
                    }
                }
                var inDB = new DogControlDao().GetDogControl(symbolName);
                if (inDB == null)
                {
                    inDB = new DogControl()
                    {
                        HistoryMax = max,
                        HistoryMin = min
                    };
                    await new DogControlDao().CreateDogControl(inDB);
                }
                else
                {
                    inDB.HistoryMax = max;
                    inDB.HistoryMin = min;
                    await new DogControlDao().CreateDogControl(inDB);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
                throw ex;
            }
        }
    }
}
