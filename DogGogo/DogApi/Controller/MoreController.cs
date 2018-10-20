using DogAccount;
using DogApi.DTO;
using DogPlatform;
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

                CoinTrade.ShouGeDoMoreForSellEmpty(dogMoreBuy, (decimal)1.035);
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="symbolName"></param>
        /// <returns></returns>
        [HttpGet]
        [ActionName("listMoreBuyIsNotFinished")]
        public async Task<object> listMoreBuyIsNotFinished(string userName, string symbolName, string sort = "lastbuy")
        {
            var list = new List<DogMoreBuy>();
            var symbols = CoinUtils.GetAllCommonSymbols();
            symbols = symbols.Where(it => it.BaseCurrency != "btc").ToList();
            Dictionary<string, decimal> closeDic = new Dictionary<string, decimal>();
            Dictionary<string, decimal> todayDic = new Dictionary<string, decimal>();
            if (string.IsNullOrEmpty(symbolName))
            {
                list = new DogMoreBuyDao().listEveryMinPriceMoreBuyIsNotFinished(userName);
                list = list.Where(it => it.SymbolName != "btc" && it.SymbolName != "ven" && it.SymbolName != "hsr").ToList();
                foreach (var symbol in symbols)
                {
                    try
                    {
                        var key = HistoryKlinePools.GetKey(symbols.Find(it => it.BaseCurrency == symbol.BaseCurrency), "1min");
                        var historyKlineData = HistoryKlinePools.Get(key);
                        if (historyKlineData == null)
                        {
                            continue;
                        }
                        var close = historyKlineData.Data[0].Close;
                        closeDic.Add(symbol.BaseCurrency, close);

                        var item = list.Find(it => it.SymbolName == symbol.BaseCurrency);

                        var todayList = historyKlineData.Data.Where(it => Utils.GetDateById(it.Id) >= DateTime.Now.Date).Select(it => it).ToList();
                        todayDic.Add(symbol.BaseCurrency, todayList.Max(it => it.Close) / todayList.Min(it => it.Close));
                        todayDic.Add(symbol.BaseCurrency + "-", close / todayList.Min(it => it.Close));
                        todayDic.Add(symbol.BaseCurrency + "+", todayList.Where(it => Utils.GetDateById(it.Id) >= item.BuyDate).Max(it => it.Close) / close);
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex.Message);
                    }
                }
                if (sort != "lastbuy")
                {
                    list.Sort((a, b) =>
                    {
                        if (!closeDic.ContainsKey(a.SymbolName) || !closeDic.ContainsKey(b.SymbolName))
                        {
                            return 1;
                        }
                        var ap = closeDic[a.SymbolName] / a.BuyTradePrice;
                        var bp = closeDic[b.SymbolName] / b.BuyTradePrice;
                        if (sort == "more")
                        {
                            return ap > bp ? 1 : -1;
                        }
                        else
                        {
                            return ap > bp ? -1 : 1;
                        }
                    });
                }
                else
                {
                    list.Sort((a, b) =>
                    {
                        return b.BuyDate > a.BuyDate ? 1 : -1;
                    });
                }
            }
            else
            {
                try
                {
                    list = new DogMoreBuyDao().listMoreBuyIsNotFinished(userName, symbolName);
                    var key = HistoryKlinePools.GetKey(symbols.Find(it => it.BaseCurrency == symbolName), "1min");
                    var historyKlineData = HistoryKlinePools.Get(key);
                    var close = historyKlineData.Data[0].Close;
                    closeDic.Add(symbolName, close);

                    var item = list.Find(it => it.SymbolName == symbolName);

                    var todayList = historyKlineData.Data.Where(it => Utils.GetDateById(it.Id) >= DateTime.Now.Date).Select(it => it).ToList();
                    todayDic.Add(symbolName, todayList.Max(it => it.Close) / todayList.Min(it => it.Close));
                    todayDic.Add(symbolName + "-", close / todayList.Min(it => it.Close));
                    todayDic.Add(symbolName + "+", todayList.Where(it => Utils.GetDateById(it.Id) >= item.BuyDate).Max(it => it.Close) / close);
                }
                catch (Exception ex)
                {
                    logger.Error(ex.Message);
                }
            }

            Dictionary<string, decimal> ladderDic = new Dictionary<string, decimal>();
            foreach (var item in list)
            {
                if (ladderDic.ContainsKey(item.SymbolName) || !closeDic.ContainsKey(item.SymbolName))
                {
                    continue;
                }
                ladderDic.Add(item.SymbolName, DogControlUtils.GetLadderSell(item.SymbolName, item.QuoteCurrency, closeDic[item.SymbolName]));
            }

            Dictionary<string, decimal> ladderBuyDic = new Dictionary<string, decimal>();
            foreach (var item in list)
            {
                if (ladderBuyDic.ContainsKey(item.SymbolName) || !closeDic.ContainsKey(item.SymbolName))
                {
                    continue;
                }
                ladderBuyDic.Add(item.SymbolName, DogControlUtils.GetLadderBuy(item.SymbolName, item.QuoteCurrency, closeDic[item.SymbolName]));
            }

            return new { list, closeDic, ladderDic, ladderBuyDic, todayDic };
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
                return GetDogMoreFinishedDTO(buyOrderId);
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
                return null;
            }
        }

        [HttpGet]
        [ActionName("listMoreBuyIsFinishedDetail")]
        public async Task<List<DogMoreFinishedDTO>> listMoreBuyIsFinishedDetail(string userName, string symbolName, int pageIndex, int pageSize)
        {
            try
            {
                var buyOrderIds = new DogMoreSellDao().listDogMoreSell(userName, symbolName, pageIndex, pageSize);
                var result = new List<DogMoreFinishedDTO>();
                foreach (var buyOrderId in buyOrderIds)
                {
                    var item = await GetDogMoreFinishedDTO(buyOrderId);
                    result.Add(item);
                }
                return result;
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
                return null;
            }
        }

        private async Task<DogMoreFinishedDTO> GetDogMoreFinishedDTO(long buyOrderId)
        {
            var dogMoreBuy = new DogMoreBuyDao().GetByBuyOrderId(buyOrderId);
            var orderMatchResult = JsonConvert.DeserializeObject<HBResponse<List<OrderMatchResult>>>(dogMoreBuy.BuyOrderMatchResults);
            var buyQuantity = (decimal)0;
            var buyAmount = (decimal)0;
            var buyFees = (decimal)0;
            foreach (var item in orderMatchResult.Data)
            {
                buyAmount += item.FilledAmount * item.price;
                if (item.symbol.IndexOf("ven") >= 0)
                {
                    buyQuantity += item.FilledAmount * 100;
                }
                else
                {
                    buyQuantity += item.FilledAmount;
                }
                buyFees += item.FilledFees;
            }

            // 交易量，交易总额，  出售总额 出售数量， 
            var sellQuantity = (decimal)0;
            var sellAmount = (decimal)0;
            var sellFees = (decimal)0;
            var sellTradePrice = (decimal)999999;
            var sellDate = DateTime.MinValue;
            var dogMoreSellList = new DogMoreSellDao().ListDogMoreSellByBuyOrderId(buyOrderId);

            foreach (var sell in dogMoreSellList)
            {
                var sellOrderMatchResult = JsonConvert.DeserializeObject<HBResponse<List<OrderMatchResult>>>(sell.SellOrderMatchResults);
                foreach (var item in sellOrderMatchResult.Data)
                {
                    sellAmount += item.FilledAmount * item.price;
                    sellQuantity += item.FilledAmount;
                    sellFees += item.FilledFees;
                    if (item.price < sellTradePrice)
                    {
                        sellTradePrice = item.price;
                    }
                }
                if (sell.SellDate > sellDate)
                {
                    sellDate = sell.SellDate;
                }
            }

            return new DogMoreFinishedDTO
            {
                BuyOrderId = buyOrderId,
                SymbolName = dogMoreBuy.SymbolName,
                UserName = dogMoreBuy.UserName,
                BuyTradePrice = dogMoreBuy.BuyTradePrice,
                BuyDate = dogMoreBuy.BuyDate,
                BuyState = dogMoreBuy.BuyState,
                BuyQuantity = buyQuantity,
                BuyAmount = buyAmount,
                BuyFees = buyFees,
                SellAmount = sellAmount,
                SellQuantity = sellQuantity,
                SellTradePrice = sellTradePrice,
                SellFees = sellFees,
                SellDate = sellDate,
                Usdt = sellAmount - buyAmount - sellFees,
                BaseSymbol = buyQuantity - sellQuantity - buyFees
            };
        }

        [HttpGet]
        [ActionName("delete")]
        public async Task Delete(long buyOrderId)
        {
            new DogMoreBuyDao().Delete(buyOrderId);
        }

        [HttpGet]
        [ActionName("listDogMoreBuyNotFinishedStatistics")]
        public async Task<object> ListDogMoreBuyNotFinishedStatistics(string userName, string sort)
        {
            var res = new DogMoreBuyDao().ListDogMoreBuyNotFinishedStatistics(userName);

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
            foreach (var item in res)
            {
                if (closeDic.ContainsKey(item.SymbolName))
                {
                    item.NowPrice = closeDic[item.SymbolName];
                    item.NowTotalAmount = closeDic[item.SymbolName] * item.TotalQuantity;
                }
            }
            if (sort == "maxmin")
            {
                res.Sort((b, a) =>
                {
                    if (a.MinPrice == 0 || b.MinPrice == 0)
                    {
                        return 0;
                    }
                    if (a.MaxPrice / a.MinPrice > b.MaxPrice / b.MinPrice)
                    {
                        return 1;
                    }
                    else
                    {
                        return -1;
                    }
                });
            }
            if (sort == "amount")
            {
                res.Sort((b, a) => (int)(a.TotalAmount - b.TotalAmount));
            }
            if (sort == "nowamount")
            {
                res.Sort((b, a) => (int)(a.NowTotalAmount - b.NowTotalAmount));
            }
            if (sort == "diffamount")
            {
                res.Sort((b, a) => (int)(a.TotalAmount - a.NowTotalAmount - (b.TotalAmount - b.NowTotalAmount)));
            }
            if (sort == "count")
            {
                res.Sort((b, a) => a.Count - b.Count);
            }
            return res;
        }

        [HttpPost]
        [ActionName("doMore")]
        public async Task<string> DoMore(string userName, string symbolName)
        {
            try
            {
                var symbols = CoinUtils.GetAllCommonSymbols();
                var symbol = symbols.Find(it => it.BaseCurrency == symbolName);

                var ladder = (decimal)1.05;
                if (symbolName == "hb10" || symbolName == "eth" || symbolName == "ltc" || symbolName == "xrp" || symbolName == "bch" || symbolName == "etc" || symbolName == "eos" || symbolName == "ht"
                    || symbolName == "dash" || symbolName == "zec" || symbolName == "omg" || symbolName == "ada" || symbolName == "iota")
                {
                    ladder = (decimal)1.04;
                }
                return CoinTrade.BuyWhenDoMoreAnalyze(symbol, userName, AccountConfigUtils.GetAccountConfig(userName).MainAccountId, ladder);
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
                return ex.Message;
            }
        }
    }
}
