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
            var list = new List<DogEmptySell>();
            var symbols = CoinUtils.GetAllCommonSymbols();
            symbols = symbols.Where(it => it.BaseCurrency != "btc").ToList();
            Dictionary<string, decimal> closeDic = new Dictionary<string, decimal>();
            if (string.IsNullOrEmpty(symbolName))
            {
                list = new DogEmptySellDao().listEveryMaxPriceEmptySellIsNotFinished();
                list = list.Where(it => it.SymbolName != "btc").ToList();
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
            }
            else
            {
                list = new DogEmptySellDao().ListDogEmptySellNotFinished(symbolName);

                var key = HistoryKlinePools.GetKey(symbols.Find(it => it.BaseCurrency == symbolName), "1min");
                var historyKlineData = HistoryKlinePools.Get(key);
                var close = historyKlineData.Data[0].Close;
                closeDic.Add(symbolName, close);
            }

            return new { list, closeDic };
        }

        [HttpGet]
        [ActionName("listEmptySellIsFinished")]
        public async Task<object> ListEmptySellIsFinished(string userName, string symbolName)
        {
            try
            {
                return new DogEmptySellDao().ListDogEmptySellFinished(userName, symbolName);
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
        [ActionName("getEmptySellDetail")]
        public object GetEmptySellDetail(long sellOrderId)
        {
            try
            {
                var dogEmptySell = new DogEmptySellDao().GetDogEmptySellBySellOrderId(sellOrderId);
                var orderMatchResult = JsonConvert.DeserializeObject<HBResponse<List<OrderMatchResult>>>(dogEmptySell.SellOrderMatchResults);
                var sellQuantity = (decimal)0;
                var sellAmount = (decimal)0;
                var sellFees = (decimal)0;
                foreach (var item in orderMatchResult.Data)
                {
                    sellAmount += item.FilledAmount * item.price;
                    sellQuantity += item.FilledAmount;
                    sellFees += item.FilledFees;
                }

                // 交易量，交易总额，  出售总额 出售数量， 
                var buyQuantity = (decimal)0;
                var buyAmount = (decimal)0;
                var buyFees = (decimal)0;
                var dogEmptyBuyList = new DogEmptyBuyDao().ListDogEmptyBuyBySellOrderId(sellOrderId);

                foreach (var buy in dogEmptyBuyList)
                {
                    var buyOrderMatchResult = JsonConvert.DeserializeObject<HBResponse<List<OrderMatchResult>>>(buy.BuyOrderMatchResults);
                    foreach (var item in buyOrderMatchResult.Data)
                    {
                        buyAmount += item.FilledAmount * item.price;
                        buyQuantity += item.FilledAmount;
                        buyFees += item.FilledFees;
                    }
                }
                return new
                {
                    sellOrderId,
                    symbolName = dogEmptySell.SymbolName,
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
        [ActionName("listEmptySellIsFinishedDetail")]
        public async Task<object> ListEmptySellIsFinishedDetail(string userName, string symbolName, int pageIndex, int pageSize)
        {
            try
            {
                try
                {
                    var sellOrderIds = new DogEmptyBuyDao().ListDogEmptyBuy(userName, symbolName, pageIndex, pageSize);
                    var result = new List<DogEmptyFinishedDTO>();
                    foreach (var sellOrderId in sellOrderIds)
                    {
                        var item = await GetDogEmptyFinishedDTO(sellOrderId);
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
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
                return null;
            }
        }

        private async Task<DogEmptyFinishedDTO> GetDogEmptyFinishedDTO(long sellOrderId)
        {
            var dogEmptySell = new DogEmptySellDao().GetDogEmptySellBySellOrderId(sellOrderId);
            var orderMatchResult = JsonConvert.DeserializeObject<HBResponse<List<OrderMatchResult>>>(dogEmptySell.SellOrderMatchResults);
            var sellQuantity = (decimal)0;
            var sellAmount = (decimal)0;
            var sellFees = (decimal)0;
            foreach (var item in orderMatchResult.Data)
            {
                sellAmount += item.FilledAmount * item.price;
                sellQuantity += item.FilledAmount;
                sellFees += item.FilledFees;
            }

            // 交易量，交易总额，  出售总额 出售数量， 
            var buyQuantity = (decimal)0;
            var buyAmount = (decimal)0;
            var buyFees = (decimal)0;
            var buyTradePrice = (decimal)0;
            var buyDate = DateTime.MaxValue;
            var dogEmptyBuyList = new DogEmptyBuyDao().ListDogEmptyBuyBySellOrderId(sellOrderId);

            foreach (var buy in dogEmptyBuyList)
            {
                var buyOrderMatchResult = JsonConvert.DeserializeObject<HBResponse<List<OrderMatchResult>>>(buy.BuyOrderMatchResults);
                foreach (var item in buyOrderMatchResult.Data)
                {
                    buyAmount += item.FilledAmount * item.price;
                    buyQuantity += item.FilledAmount;
                    buyFees += item.FilledFees;
                    if (buyTradePrice < item.price)
                    {
                        buyTradePrice = item.price;
                    }
                }
                if (buy.BuyDate < buyDate)
                {
                    buyDate = buy.BuyDate;
                }
            }
            return new DogEmptyFinishedDTO
            {
                SellOrderId = sellOrderId,
                SymbolName = dogEmptySell.SymbolName,
                UserName = dogEmptySell.UserName,
                SellTradePrice = dogEmptySell.SellTradePrice,
                SellDate = dogEmptySell.SellDate,
                SellState = dogEmptySell.SellState,
                BuyQuantity = buyQuantity,
                BuyTradePrice = buyTradePrice,
                BuyDate = buyDate,
                BuyAmount = buyAmount,
                BuyFees = buyFees,
                SellAmount = sellAmount,
                SellQuantity = sellQuantity,
                SellFees = sellFees,
                Usdt = sellAmount - buyAmount - sellFees,
                BaseSymbol = buyQuantity - sellQuantity - buyFees
            };
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

            var list = new DogMoreBuyDao().listMoreBuyIsNotFinished(symbolName, userName);
            var totalQuantity = (decimal)0;
            list.ForEach(it => totalQuantity += it.BuyQuantity);

            return new { balanceItem, list, totalQuantity };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="symbolName"></param>
        /// <returns></returns>
        [HttpPost]
        [ActionName("doEmpty")]
        public async Task<object> DoEmpty(string userName, string symbolName)
        {
            // 立马空单
            var symbols = CoinUtils.GetAllCommonSymbols();
            var symbol = symbols.Find(it => it.BaseCurrency == symbolName);
            var dao = new KlineDao();
            var lastKlines = dao.List24HourKline(symbol.BaseCurrency);
            // 大于今天最小值15%才行 or 大于24小时20%
            var nowPrice = lastKlines[0].Close;
            var min24 = decimal.MaxValue;
            var minToday = decimal.MaxValue;
            foreach (var item in lastKlines)
            {
                if (item.Close < min24)
                {
                    min24 = item.Close;
                }
                if (item.Close < minToday && Utils.GetDateById(item.Id) > DateTime.Today)
                {
                    minToday = item.Close;
                }
            }
            if (nowPrice / min24 > (decimal)1.20 || nowPrice / minToday > (decimal)1.15)
            {
                CoinTrade.DoEmpty(symbol, userName, AccountConfigUtils.GetAccountConfig(userName).MainAccountId);
            }
            return new { nowPrice, min24, minToday };
        }
    }
}
