using DogAccount;
using DogPlatform;
using DogPlatform.Model;
using DogRunService;
using DogRunService.Helper;
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

        [HttpPut]
        [ActionName("initEmpty30Percent")]
        public async Task UpdateEmpty30Percent(string symbolName, string quoteCurrency)
        {
            var control = new DogControlDao().GetDogControl(symbolName, quoteCurrency);
            if (control == null)
            {
                return;
            }
            control.EmptyPrice = (control.HistoryMax - control.HistoryMin) * (decimal)0.7 + control.HistoryMin;
            control.EmptyExpiredTime = DateTime.Now.AddYears(2);
            await new DogControlDao().CreateDogControl(control);
            return;
        }

        [HttpGet]
        [ActionName("list")]
        public async Task<object> List(string quoteCurrency)
        {
            try
            {
                var res = await new DogControlDao().ListDogControl(quoteCurrency);
                return res.OrderBy(it => it.SymbolName).ToList();
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
                throw ex;
            }
        }

        [HttpPut]
        [ActionName("setUnvalid")]
        public async Task<string> SetUnvalid(string symbolName, string quoteCurrency)
        {
            try
            {
                await new DogControlDao().SetUnvalid(symbolName, quoteCurrency);
                return "修改完成";
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
                return ex.Message;
            }
        }

        [HttpGet]
        [ActionName("refreshHistoryMaxMin")]
        public async Task RefreshHistoryMaxMin(string symbolName, string quoteCurrency)
        {
            try
            {
                PlatformApi api = PlatformApi.GetInstance("xx");
                var period = "4hour";
                var klines = api.GetHistoryKline(symbolName + quoteCurrency, period, 1000);
                var min = decimal.MinValue;
                var max = decimal.MaxValue;

                min = klines.Where(it => it.Low > min).Min(it => it.Low);
                min = klines.Where(it => it.Low > min).Min(it => it.Low);
                min = klines.Where(it => it.Low > min).Min(it => it.Low);

                max = klines.Where(it => it.High < max).Max(it => it.High);
                max = klines.Where(it => it.High < max).Max(it => it.High);
                max = klines.Where(it => it.High < max).Max(it => it.High);

                // 判断max
                var maxNotSell = new DogMoreBuyDao().GetMaxPriceOfNotSellFinished(quoteCurrency, symbolName);
                if (maxNotSell > max)
                {
                    max = maxNotSell;
                }

                var inDB = new DogControlDao().GetDogControl(symbolName, quoteCurrency);
                if (inDB == null)
                {
                    inDB = new DogControl()
                    {
                        SymbolName = symbolName,
                        QuoteCurrency = quoteCurrency,
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

        //[HttpGet]
        //[ActionName("getFlexCount")]
        //public async Task<Object> GetFlexCount(string symbolName, string quoteCurrency)
        //{
        //    var symbols = CoinUtils.GetAllCommonSymbols(quoteCurrency);
        //    CommonSymbols symbol = symbols.Find(it => it.BaseCurrency == symbolName);
        //    KlineUtils.InitOneKine(symbol);
        //    var key = HistoryKlinePools.GetKey(symbol, "1min");
        //    var historyKlineData = HistoryKlinePools.Get(key);

        //    var historyKlines = historyKlineData.Data;
        //    var outFlexPercent = (decimal)0;
        //    var flexCount = GetFlexpointCount(historyKlines, out outFlexPercent);
        //    var inDB = new DogControlDao().GetDogControl(symbolName, quoteCurrency);
        //    inDB.LadderSellPercent = outFlexPercent;
        //    inDB.LadderSellExpiredTime = DateTime.Now.AddYears(1);
        //    await new DogControlDao().CreateDogControl(inDB);

        //    return flexCount;
        //}

        private Dictionary<decimal, int> GetFlexpointCount(List<HistoryKline> historyKlines, out decimal outFlexPercent)
        {
            Dictionary<decimal, int> result = new Dictionary<decimal, int>();
            decimal flexPercent = (decimal)1.02;
            outFlexPercent = flexPercent;
            for (int i = 0; i < 30; i++)
            {
                var flexPointList = CoinAnalyze.Analyze(historyKlines, flexPercent);
                if (flexPointList.Count != 0)
                {
                    outFlexPercent = flexPercent;
                }
                result.Add(flexPercent, flexPointList.Count);

                flexPercent += (decimal)0.005;
            }
            return result;
        }

        [HttpGet]
        [ActionName("initAccountInfo")]
        public async Task<object> InitAccountInfo(string userName)
        {
            try
            {
                PlatformApi api = PlatformApi.GetInstance(userName);
                var accountInfo = api.GetAccountBalance(AccountConfigUtils.GetAccountConfig(userName).MainAccountId);
                logger.Info(JsonConvert.SerializeObject(accountInfo.Data.list));

                var result = new List<Dictionary<string, object>>();

                foreach (var balanceItem in accountInfo.Data.list)
                {
                    try
                    {
                        if(balanceItem.balance < (decimal)0.00001 || balanceItem.type == "frozen")
                        {
                            continue;
                        }

                        var list = new DogMoreBuyDao().listMoreBuyIsNotFinished(userName, balanceItem.currency);
                        var totalQuantity = list.Sum(it => it.BuyQuantity);

                        Dictionary<string, object> item = new Dictionary<string, object>();
                        item.Add("currency", balanceItem.currency);
                        item.Add("buyQuantity", totalQuantity);
                        item.Add("balance", balanceItem.balance);
                        //item.Add("nowPrice", close);
                        item.Add("canEmptyQuantity", balanceItem.balance - totalQuantity);
                        //item.Add("canEmptyAmount", (balanceItem.balance - totalQuantity) * close);

                        result.Add(item);
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex.Message, ex);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
                throw ex;
            }
        }
    }
}
