using DogPlatform;
using DogRunService;
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
                var res = await new DogControlDao().ListDogControl();
                res = res.OrderBy(it => it.SymbolName).ToList();

                var symbols = CoinUtils.GetAllCommonSymbols();
                symbols = symbols.Where(it => it.BaseCurrency != "btc").ToList();
                Dictionary<string, decimal> closeDic = new Dictionary<string, decimal>();
                foreach (var symbol in symbols)
                {
                    try
                    {
                        var key = HistoryKlinePools.GetKey(symbols.Find(it => it.BaseCurrency == symbol.BaseCurrency), "1min");
                        var historyKlineData = HistoryKlinePools.Get(key);
                        var close = historyKlineData.Data[0].Close;
                        closeDic.Add(symbol.BaseCurrency, close);
                    }
                    catch (Exception ex)
                    {

                    }
                }
                return new { list = res, closeDic };
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
                var min1 = decimal.MaxValue;
                var min2 = decimal.MaxValue;
                var min3 = decimal.MaxValue;
                var max = decimal.MinValue;
                var max1 = decimal.MinValue;
                var max2 = decimal.MinValue;
                var max3 = decimal.MinValue;
                foreach (var item in klines)
                {
                    if (item.Low < min3)
                    {
                        min = min1;
                        min1 = min2;
                        min2 = min3;
                        min3 = item.Low;
                    }
                    else if (item.Low < min2)
                    {
                        min = min1;
                        min1 = min2;
                        min2 = item.Low;
                    }
                    else if (item.Low < min1)
                    {
                        min = min1;
                        min1 = item.Low;
                    }
                    else if (item.Low < min)
                    {
                        min = item.Low;
                    }

                    if (item.High >= max3)
                    {
                        max = max1;
                        max1 = max2;
                        max2 = max3;
                        max3 = item.High;
                    }
                    else if (item.High >= max2)
                    {
                        max = max1;
                        max1 = max2;
                        max2 = item.High;
                    }
                    else if (item.High >= max1)
                    {
                        max = max1;
                        max1 = item.High;
                    }
                    else if (item.High >= max)
                    {
                        max = item.High;
                    }
                }
                var inDB = new DogControlDao().GetDogControl(symbolName);
                if (inDB == null)
                {
                    inDB = new DogControl()
                    {
                        SymbolName = symbolName,
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
