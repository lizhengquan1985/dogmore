using DogAccount;
using DogPlatform;
using DogPlatform.Model;
using DogRunService.DataTypes;
using DogRunService.Helper;
using DogService;
using DogService.Dao;
using DogService.DateTypes;
using log4net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DogRunService
{
    public class CoinTrade
    {
        static ILog logger = LogManager.GetLogger(typeof(CoinTrade));

        public static void Run(int index, CommonSymbol symbol)
        {
            try
            {
                AnalyzeResult analyzeResult = AnalyzeResult.GetAnalyzeResult(symbol);
                if (analyzeResult != null)
                {
                    Console.WriteLine($"--->domore {index + 1}   {symbol.BaseCurrency},{symbol.QuoteCurrency}");
                    // 计算是否适合购买
                    RunBuy(symbol, analyzeResult);
                }
            }
            catch (Exception ex)
            {
                logger.Error($"---> 购买异常: {JsonConvert.SerializeObject(symbol)}" + ex.Message, ex);
            }

            try
            {
                AnalyzeResult analyzeResult = AnalyzeResult.GetAnalyzeResult(symbol);
                if (analyzeResult != null)
                {
                    Console.WriteLine($"--->doempty {index + 1}   {symbol.BaseCurrency},{symbol.QuoteCurrency}");
                    // 计算是否适合出售
                    RunSell(symbol, analyzeResult);
                }
            }
            catch (Exception ex)
            {
                logger.Error($"---> 购买异常: {JsonConvert.SerializeObject(symbol)}" + ex.Message, ex);
            }
        }

        private static void RunBuy(CommonSymbol symbol, AnalyzeResult analyzeResult)
        {
            var historyKlines = analyzeResult.HistoryKlines;
            var nowPrice = analyzeResult.NowPrice;

            var userNames = UserPools.GetAllUserName();
            var ladderBuyPercent = DogControlUtils.GetLadderBuy(symbol.BaseCurrency, symbol.QuoteCurrency, nowPrice);

            // 空单的自动波动收割
            foreach (var userName in userNames)
            {
                var dogEmptySellList = new DogEmptySellDao().GetNeedShougeDogEmptySell(userName, symbol.BaseCurrency, symbol.QuoteCurrency);
                Console.WriteLine("做空收割 " + symbol.BaseCurrency + symbol.QuoteCurrency + $", nowPrice:{nowPrice} 空单数量：" + dogEmptySellList.Count);
                foreach (var item in dogEmptySellList)
                {
                    Console.WriteLine($"      {item.SellTradePrice}");
                }


                foreach (var dogEmptySellItem in dogEmptySellList)
                {
                    if (!analyzeResult.CheckCanBuyForHuiDiao(dogEmptySellItem))
                    {
                        continue;
                    }

                    ShouGeDogEmpty(dogEmptySellItem, symbol, analyzeResult, ladderBuyPercent);
                }
            }

            // 1.低于管控的购入价
            if (!JudgeBuyUtils.ControlCanBuy(symbol.BaseCurrency, symbol.QuoteCurrency, nowPrice))
            {
                return;
            }

            // 自动波动做多
            foreach (var userName in userNames)
            {
                AccountConfig accountConfig = AccountConfigUtils.GetAccountConfig(userName);
                var accountId = accountConfig.MainAccountId;

                try
                {
                    BuyWhenDoMore(symbol, userName, accountId, analyzeResult, ladderBuyPercent);
                }
                catch (Exception ex)
                {
                    //logger.Error($"{userName}--{JsonConvert.SerializeObject(symbol)}--{ex.Message}", ex);
                    continue;
                }
            }

        }

        /// <summary>
        /// 购买,做多的时候
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="userName"></param>
        /// <param name="accountId"></param>
        public static void BuyWhenDoMore(CommonSymbol symbol, string userName, string accountId, AnalyzeResult analyzeResult,
            decimal ladderBuyPercent)
        {
            var nowPrice = analyzeResult.NowPrice;
            ladderBuyPercent = Math.Max(ladderBuyPercent, (decimal)1.04);

            var dogMoreBuy = new DogMoreBuyDao().GetMinBuyPriceDataOfNotSellFinished(accountId, userName, symbol.QuoteCurrency, symbol.BaseCurrency);
            var lastBuyMinPrice = (decimal)10000;
            if (dogMoreBuy != null)
            {
                if (dogMoreBuy.BuyTradePrice <= 0)
                {
                    logger.Error("--BuyWhenDoMore-- 获取上一次最小购入价位出错");
                    return;
                }
                lastBuyMinPrice = Math.Min(dogMoreBuy.BuyTradePrice, dogMoreBuy.BuyOrderPrice);
            }

            if (nowPrice * ladderBuyPercent > lastBuyMinPrice)
            {
                throw new ApplicationException("有价格比这个更低得还没有收割。不能重新做多。");
            }

            if (!analyzeResult.CheckCanBuyForHuiDiao(dogMoreBuy))
            {
                return;
            }

            PlatformApi api = PlatformApi.GetInstance(userName);
            var accountInfo = api.GetAccountBalance(accountId);
            var quoteCurrency = accountInfo.Data.list.Find(it => it.currency == symbol.QuoteCurrency);
            // 要减去空单未收割得额度总和
            var notShougeEmptySellAmount = new DogEmptySellDao().GetSumNotShougeDogEmptySell(userName, symbol.QuoteCurrency);
            if (!CommonHelper.CheckBalanceForDoMore(symbol.QuoteCurrency, quoteCurrency.balance, notShougeEmptySellAmount))
            {
                Console.WriteLine($"{symbol.BaseCurrency}{symbol.QuoteCurrency}余额不足notShougeEmptySellAmount:{notShougeEmptySellAmount},balance:{quoteCurrency.balance}");
                return;
            }
            decimal recommendAmount = (quoteCurrency.balance - notShougeEmptySellAmount) / DogControlUtils.GetRecommendDivideForMore(symbol.BaseCurrency, symbol.QuoteCurrency, nowPrice);

            if (symbol.QuoteCurrency == "usdt")
            {
                var ladderPosition = DogControlUtils.GetLadderPosition(symbol.BaseCurrency, symbol.QuoteCurrency, nowPrice);
                var min = (decimal)1.5;
                if (ladderPosition < (decimal)0.2)
                {
                    min = (decimal)2;
                }
                if (recommendAmount < min)
                {
                    recommendAmount = min;
                }
            }
            else if (symbol.QuoteCurrency == "btc")
            {
                if (recommendAmount < (decimal)0.0004)
                {
                    recommendAmount = (decimal)0.0004;
                }
            }
            else if (symbol.QuoteCurrency == "eth")
            {
                if (recommendAmount < (decimal)0.005)
                {
                    recommendAmount = (decimal)0.005;
                }
            }
            else if (symbol.QuoteCurrency == "ht")
            {
                if (recommendAmount < (decimal)0.9)
                {
                    recommendAmount = (decimal)0.9;
                }
            }

            // 购买的要求
            decimal buyQuantity = recommendAmount / nowPrice;
            buyQuantity = decimal.Round(buyQuantity, symbol.AmountPrecision);
            decimal orderPrice = decimal.Round(nowPrice * (decimal)1.006, symbol.PricePrecision);

            OrderPlaceRequest req = new OrderPlaceRequest();
            req.account_id = accountId;
            req.amount = buyQuantity.ToString();
            req.price = orderPrice.ToString();
            req.source = "api";
            req.symbol = symbol.BaseCurrency + symbol.QuoteCurrency;
            req.type = "buy-limit";
            if (BuyLimitUtils.Record(userName, symbol.BaseCurrency + symbol.QuoteCurrency))
            {
                logger.Error(" --------------------- 两个小时内购买次数太多，暂停一会 --------------------- ");
                Thread.Sleep(1000 * 10);
                return;
            }

            HBResponse<long> order = null;
            try
            {
                logger.Error($"1开始下单 -----------------------------{JsonConvert.SerializeObject(req)}");
                order = api.OrderPlace(req);
                logger.Error($"2下单结束 -----------------------------{JsonConvert.SerializeObject(req)}");

                if (order.Status == "ok")
                {
                    new DogMoreBuyDao().CreateDogMoreBuy(new DogMoreBuy()
                    {
                        SymbolName = symbol.BaseCurrency,
                        QuoteCurrency = symbol.QuoteCurrency,
                        AccountId = accountId,
                        UserName = userName,

                        BuyQuantity = buyQuantity,
                        BuyOrderPrice = orderPrice,
                        BuyDate = DateTime.Now,
                        BuyOrderResult = JsonConvert.SerializeObject(order),
                        BuyState = StateConst.PreSubmitted,
                        BuyTradePrice = 0,
                        BuyOrderId = order.Data,
                        BuyMemo = "",
                        BuyOrderDetail = "",
                        BuyOrderMatchResults = "",
                        IsFinished = false
                    });

                    // 下单成功马上去查一次
                    QueryBuyDetailAndUpdate(userName, order.Data);
                }
                logger.Error($"3入库结束 -----------------------------做多  下单购买结果 {JsonConvert.SerializeObject(req)}, notShougeEmptySellAmount:{notShougeEmptySellAmount}, order：{JsonConvert.SerializeObject(order)}, nowPrice：{nowPrice}, accountId：{accountId},");
            }
            catch (Exception ex)
            {
                logger.Error($"严重严重 ---------------  下的出错  --------------{JsonConvert.SerializeObject(req)}");
                Thread.Sleep(1000 * 60 * 5);
                throw ex;
            }

        }

        public static void ShouGeDogEmpty(DogEmptySell dogEmptySell, CommonSymbol symbol, AnalyzeResult analyzeResult, decimal percent = (decimal)1.02)
        {
            var nowPrice = analyzeResult.NowPrice;
            if (nowPrice * percent > dogEmptySell.SellTradePrice)
            {
                return;
            }

            decimal buyQuantity = CommonHelper.CalcBuyQuantityForEmptyShouge(dogEmptySell.SellQuantity, dogEmptySell.SellTradePrice, nowPrice, symbol.AmountPrecision, symbol);
            decimal orderPrice = decimal.Round(nowPrice * (decimal)1.01, symbol.PricePrecision);

            OrderPlaceRequest req = new OrderPlaceRequest();
            req.account_id = dogEmptySell.AccountId;
            req.amount = buyQuantity.ToString();
            req.price = orderPrice.ToString();
            req.source = "api";
            req.symbol = symbol.BaseCurrency + symbol.QuoteCurrency;
            req.type = "buy-limit";

            if (BuyLimitUtils.Record(dogEmptySell.UserName, symbol.BaseCurrency + symbol.QuoteCurrency))
            {
                logger.Error(" --------------------- 两个小时内购买次数太多，暂停一会 --------------------- ");
                logger.Error(" --------------------- 两个小时内购买次数太多，暂停一会 --------------------- ");
                logger.Error(" --------------------- 两个小时内购买次数太多，暂停一会 --------------------- ");
                Thread.Sleep(1000 * 5);
                return;
            }

            PlatformApi api = PlatformApi.GetInstance(dogEmptySell.UserName);
            HBResponse<long> order = null;
            try
            {
                logger.Error($"开始下单 -----------------------------{JsonConvert.SerializeObject(req)}");
                order = api.OrderPlace(req);
                logger.Error($"下单结束 -----------------------------{JsonConvert.SerializeObject(req)}");

                if (order.Status == "ok")
                {
                    new DogEmptyBuyDao().CreateDogEmptyBuy(new DogEmptyBuy()
                    {
                        SymbolName = symbol.BaseCurrency,
                        QuoteCurrency = symbol.QuoteCurrency,
                        AccountId = dogEmptySell.AccountId,
                        UserName = dogEmptySell.UserName,
                        SellOrderId = dogEmptySell.SellOrderId,

                        BuyQuantity = buyQuantity,
                        BuyOrderPrice = orderPrice,
                        BuyDate = DateTime.Now,
                        BuyOrderResult = JsonConvert.SerializeObject(order),
                        BuyState = StateConst.PreSubmitted,
                        BuyTradePrice = 0,
                        BuyOrderId = order.Data,
                        BuyMemo = "",
                        BuyOrderDetail = "",
                        BuyOrderMatchResults = "",
                    });
                    // 下单成功马上去查一次
                    QueryEmptyBuyDetailAndUpdate(dogEmptySell.UserName, order.Data);
                }
                logger.Error($"入库结束 -----------------------------空单收割 {JsonConvert.SerializeObject(req)}, order：{JsonConvert.SerializeObject(order)}, ,nowPrice：{nowPrice}, accountId：{dogEmptySell.AccountId}");
            }
            catch (Exception ex)
            {
                logger.Error($"严重 ---------------  ShouGeDogEmpty出错  -------------- {JsonConvert.SerializeObject(req)}");
                Thread.Sleep(1000 * 60 * 5);
                throw ex;
            }
        }

        private static void RunSell(CommonSymbol symbol, AnalyzeResult analyzeResult)
        {
            var historyKlines = analyzeResult.HistoryKlines;
            var nowPrice = analyzeResult.NowPrice;

            var userNames = UserPools.GetAllUserName();

            // 多单的自动波动收割
            foreach (var userName in userNames)
            {
                var needSellDogMoreBuyList = new DogMoreBuyDao().GetNeedSellDogMoreBuy(userName, symbol.BaseCurrency, symbol.QuoteCurrency);

                foreach (var dogMoreBuyItem in needSellDogMoreBuyList)
                {
                    decimal gaoyuPercentSell = DogControlUtils.GetLadderSell(dogMoreBuyItem.SymbolName, dogMoreBuyItem.QuoteCurrency, nowPrice); //(decimal)1.035;

                    try
                    {
                        ShouGeDogMore(dogMoreBuyItem, symbol, gaoyuPercentSell);
                    }
                    catch (Exception ex)
                    {
                        logger.Error($"收割出错 {ex.Message} {JsonConvert.SerializeObject(dogMoreBuyItem)}", ex);
                        continue;
                    }
                }
            }

            // 快速下降和不符合管控的，则不考虑做空
            if (JudgeBuyUtils.IsQuickDrop(symbol, historyKlines)
                || !JudgeBuyUtils.ControlCanSell(symbol.BaseCurrency, symbol.QuoteCurrency, nowPrice))
            {
                return;
            }

            foreach (var userName in userNames)
            {
                try
                {
                    var dogEmptySell = new DogEmptySellDao().GetDogEmptySellOfLastNotFinished(userName, symbol.BaseCurrency, symbol.QuoteCurrency);
                    if (!analyzeResult.CheckCanSellForHuiDiao(dogEmptySell))
                    {
                        continue;
                    }
                    // 和上次做空价格要相差8%
                    var maxSellTradePrice = new DogEmptySellDao().GetMaxSellTradePrice(userName, symbol.BaseCurrency, symbol.QuoteCurrency);
                    var emptyLadder = DogControlUtils.GetEmptyLadderSell(symbol.BaseCurrency, symbol.QuoteCurrency, nowPrice);
                    if (maxSellTradePrice != null && nowPrice < maxSellTradePrice * emptyLadder)
                    {
                        // 上一次还没收割得，相差10%， 要等等
                        continue;
                    }

                    var accountConfig = AccountConfigUtils.GetAccountConfig(userName);
                    var accountId = accountConfig.MainAccountId;
                    PlatformApi api = PlatformApi.GetInstance(userName);

                    var accountInfo = api.GetAccountBalance(AccountConfigUtils.GetAccountConfig(userName).MainAccountId);
                    var balanceItem = accountInfo.Data.list.Find(it => it.currency == symbol.BaseCurrency);
                    // 要减去未收割得。
                    var notShougeQuantity = new DogMoreBuyDao().GetBuyQuantityNotShouge(userName, symbol.BaseCurrency);
                    if (notShougeQuantity >= balanceItem.balance || notShougeQuantity <= 0)
                    {
                        logger.Error($"未收割得数量大于余额，有些不合理, {symbol.BaseCurrency}, {userName}, {notShougeQuantity}, {balanceItem.balance}");
                        continue;
                    }
                    if ((balanceItem.balance - notShougeQuantity) * nowPrice < (decimal)0.8)
                    {
                        //LogNotBuy(symbol.BaseCurrency, $"收益不超过0.8usdt,, balance: {balanceItem.balance},  notShougeQuantity:{notShougeQuantity}, {nowPrice}, yu: {(balanceItem.balance - notShougeQuantity) * nowPrice}");
                        continue;
                    }

                    var devide = DogControlUtils.GetRecommendDivideForEmpty(symbol.BaseCurrency, symbol.QuoteCurrency, nowPrice, (balanceItem.balance - notShougeQuantity));
                    decimal sellQuantity = (balanceItem.balance - notShougeQuantity) / devide; // 暂定每次做空1/12
                    if (sellQuantity * nowPrice > 10)
                    {
                        sellQuantity = 10 / nowPrice;
                    }
                    if ((balanceItem.balance - notShougeQuantity) * nowPrice < 10)
                    {
                        sellQuantity = (balanceItem.balance - notShougeQuantity) / 12;
                        if ((balanceItem.balance - notShougeQuantity) * nowPrice < 5)
                        {
                            sellQuantity = (balanceItem.balance - notShougeQuantity) / 9;
                            if ((balanceItem.balance - notShougeQuantity) * nowPrice < 2)
                            {
                                sellQuantity = (balanceItem.balance - notShougeQuantity) / 5;
                                if ((balanceItem.balance - notShougeQuantity) * nowPrice < 1)
                                {
                                    sellQuantity = (balanceItem.balance - notShougeQuantity) / 3;
                                }
                            }
                        }
                    }
                    sellQuantity = decimal.Round(sellQuantity, symbol.AmountPrecision);
                    if (sellQuantity * nowPrice < (decimal)0.8)
                    {
                        //LogNotBuy(symbol.BaseCurrency, $"做空不超过0.8usdt,, balance: {balanceItem.balance},  notShougeQuantity:{notShougeQuantity}, {nowPrice}, yu: {(balanceItem.balance - notShougeQuantity) * nowPrice}");
                        continue;
                    }

                    // 出售
                    decimal sellPrice = decimal.Round(nowPrice * (decimal)0.98, symbol.PricePrecision);
                    SellWhenDoEmpty(accountId, userName, symbol, sellQuantity, sellPrice, $"device:{devide}");
                }
                catch (Exception ex)
                {
                    logger.Error(ex.Message, ex);
                }
            }

        }

        private static void SellWhenDoEmpty(string accountId, string userName, CommonSymbol symbol, decimal sellQuantity, decimal sellPrice, string sellMemo = "")
        {
            try
            {
                if (sellQuantity < symbol.AmountPrecision)
                {
                    LogNotBuy(symbol.BaseCurrency + "-empty-sell", $"sell userName:{userName} 出错,{symbol.BaseCurrency} 的精度为 {symbol.AmountPrecision}, 但是却要出售{sellQuantity}  ");
                    return;
                }

                OrderPlaceRequest req = new OrderPlaceRequest();
                req.account_id = accountId;
                req.amount = sellQuantity.ToString();
                req.price = sellPrice.ToString();
                req.source = "api";
                req.symbol = symbol.BaseCurrency + symbol.QuoteCurrency; ;
                req.type = "sell-limit";

                PlatformApi api = PlatformApi.GetInstance(userName);
                HBResponse<long> order = null;
                try
                {
                    logger.Error($"开始下单 -----------------------------{JsonConvert.SerializeObject(req)}");
                    order = api.OrderPlace(req);
                    logger.Error($"下单结束 -----------------------------{JsonConvert.SerializeObject(req)}");

                    if (order.Status == "ok")
                    {
                        DogEmptySell dogEmptySell = new DogEmptySell()
                        {
                            AccountId = accountId,
                            UserName = userName,
                            SellOrderId = order.Data,
                            SellOrderResult = JsonConvert.SerializeObject(order),
                            SellDate = DateTime.Now,
                            SellQuantity = sellQuantity,
                            SellOrderPrice = sellPrice,
                            SellState = StateConst.Submitted,
                            SellTradePrice = 0,
                            SymbolName = symbol.BaseCurrency,
                            QuoteCurrency = symbol.QuoteCurrency,
                            SellMemo = sellMemo,
                            SellOrderDetail = "",
                            SellOrderMatchResults = "",
                            IsFinished = false
                        };
                        new DogEmptySellDao().CreateDogEmptySell(dogEmptySell);

                        // 下单成功马上去查一次
                        QueryEmptySellDetailAndUpdate(userName, order.Data);
                    }
                    logger.Error($"入库结束 -----------------------------做空 {JsonConvert.SerializeObject(req)}下单出售结果：" + JsonConvert.SerializeObject(order));
                }
                catch (Exception ex)
                {
                    logger.Error($"严重 ---------------  做空出错  --------------{JsonConvert.SerializeObject(req)}");
                    Thread.Sleep(1000 * 60 * 5);
                    throw ex;
                }
            }
            catch (Exception ex)
            {
                logger.Error($"SellWhenDoEmpty  {ex.Message}", ex);
            }
        }

        public static void ShouGeDogMore(DogMoreBuy dogMoreBuy, CommonSymbol symbol, decimal sellPercent)
        {
            AnalyzeResult analyzeResult = AnalyzeResult.GetAnalyzeResult(symbol);
            if (analyzeResult == null)
            {
                return;
            }

            var nowPrice = analyzeResult.NowPrice;

            // 没有大于预期, 也不能收割
            if (nowPrice < dogMoreBuy.BuyTradePrice * sellPercent// JudgeSellUtils.GetPercent(nowPrice, flexPointList[0].close, flexPointList[0].id, sellPercent)
                || nowPrice < dogMoreBuy.BuyTradePrice * (decimal)1.03)
            {
                if (sellPercent < (decimal)1.04)
                {
                    logger.Error($"------------{dogMoreBuy.SymbolName}------------> 没有大于预期, 也不能收割");
                }
                return;
            }

            if (!analyzeResult.CheckCanSellForHuiDiao(dogMoreBuy))
            {
                Console.WriteLine("不满足回调");
                // 判断是否有回掉
                return;
            }

            decimal sellQuantity = JudgeSellUtils.CalcSellQuantityForMoreShouge(dogMoreBuy.BuyQuantity, dogMoreBuy.BuyTradePrice, nowPrice, symbol);
            if (sellQuantity > dogMoreBuy.BuyQuantity)
            {
                // 一定要赚才能出售
                logger.Error($"{dogMoreBuy.SymbolName} sellQuantity:{sellQuantity}, BuyQuantity:{dogMoreBuy.BuyQuantity}");
                return;
            }

            // 出售
            decimal sellPrice = decimal.Round(nowPrice * (decimal)0.98, symbol.PricePrecision);
            OrderPlaceRequest req = new OrderPlaceRequest();
            req.account_id = dogMoreBuy.AccountId;
            req.amount = sellQuantity.ToString();
            req.price = sellPrice.ToString();
            req.source = "api";
            req.symbol = symbol.BaseCurrency + symbol.QuoteCurrency; ;
            req.type = "sell-limit";
            PlatformApi api = PlatformApi.GetInstance(dogMoreBuy.UserName);
            HBResponse<long> order = null;
            try
            {
                logger.Error($"开始下单 -----------------------------{JsonConvert.SerializeObject(req)}");
                order = api.OrderPlace(req);
                logger.Error($"下单结束 -----------------------------{JsonConvert.SerializeObject(order)}");

                // 下单出错, 报了异常, 也需要查询是否下单成功. 查询最近的订单.
                if (order.Status == "ok")
                {
                    DogMoreSell dogMoreSell = new DogMoreSell()
                    {
                        AccountId = dogMoreBuy.AccountId,
                        UserName = dogMoreBuy.UserName,
                        BuyOrderId = dogMoreBuy.BuyOrderId,
                        SellOrderId = order.Data,
                        SellOrderResult = JsonConvert.SerializeObject(order),
                        SellDate = DateTime.Now,
                        SellQuantity = sellQuantity,
                        SellOrderPrice = sellPrice,
                        SellState = StateConst.Submitted,
                        SellTradePrice = 0,
                        SymbolName = symbol.BaseCurrency,
                        QuoteCurrency = symbol.QuoteCurrency,
                        SellMemo = "",
                        SellOrderDetail = "",
                        SellOrderMatchResults = ""
                    };
                    new DogMoreSellDao().CreateDogMoreSell(dogMoreSell);

                    // 下单成功马上去查一次
                    QuerySellDetailAndUpdate(dogMoreBuy.UserName, order.Data);
                }
                logger.Error($"入库结束 -----------------------------多单收割 --> nowPrice：{nowPrice}，{JsonConvert.SerializeObject(dogMoreBuy)}");
            }
            catch (Exception ex)
            {
                logger.Error($"严重 ---------------  多单收割出错  --------------");
                Thread.Sleep(1000 * 60 * 5);
                throw ex;
            }
        }

        #region 手动

        public static void DoEmpty(CommonSymbol symbol, string userName, string accountId)
        {
            AnalyzeResult analyzeResult = AnalyzeResult.GetAnalyzeResult(symbol);
            if (analyzeResult == null)
            {
                throw new ApplicationException("做空失败，分析出错");
            }
            var nowPrice = analyzeResult.NowPrice;

            if (nowPrice * (decimal)1.06 < analyzeResult.MaxPrice)
            {
                throw new ApplicationException("已经降低了6%， 不要做空，谨慎起见");
            }

            var maxSellTradePrice = new DogEmptySellDao().GetMaxSellTradePrice(userName, symbol.BaseCurrency, symbol.QuoteCurrency);
            if (maxSellTradePrice != null && nowPrice < maxSellTradePrice * (decimal)1.06)
            {
                throw new ApplicationException("有价格比这个更高得还没有收割。不能重新做空。");
            }

            PlatformApi api = PlatformApi.GetInstance(userName);

            var accountInfo = api.GetAccountBalance(AccountConfigUtils.GetAccountConfig(userName).MainAccountId);
            var balanceItem = accountInfo.Data.list.Find(it => it.currency == symbol.BaseCurrency);
            // 要减去未收割得。
            var notShougeQuantity = new DogMoreBuyDao().GetBuyQuantityNotShouge(userName, symbol.BaseCurrency);
            if (notShougeQuantity >= balanceItem.balance || notShougeQuantity <= 0)
            {
                logger.Error($"未收割得数量大于余额，有些不合理，  {symbol.BaseCurrency},, {userName},, {notShougeQuantity}, {balanceItem.balance}");
                return;
            }

            var devide = DogControlUtils.GetRecommendDivideForEmpty(symbol.BaseCurrency, symbol.QuoteCurrency, nowPrice, (balanceItem.balance - notShougeQuantity));
            decimal sellQuantity = (balanceItem.balance - notShougeQuantity) / devide; // 暂定每次做空1/12

            if (sellQuantity * nowPrice > 10) // 一次做空不超过10usdt
            {
                sellQuantity = 10 / nowPrice;
            }
            sellQuantity = decimal.Round(sellQuantity, symbol.AmountPrecision);

            if (sellQuantity * nowPrice < 1)
            {
                sellQuantity = (balanceItem.balance - notShougeQuantity) / 10;
                if (sellQuantity * nowPrice < 1)
                {
                    sellQuantity = (balanceItem.balance - notShougeQuantity) / 5;
                    if (sellQuantity * nowPrice < 1)
                    {
                        sellQuantity = (balanceItem.balance - notShougeQuantity) / 3;
                        if (sellQuantity * nowPrice < (decimal)0.2)
                        {
                            LogNotBuy(symbol.BaseCurrency, $"收益不超过0.2usdt,, balance: {balanceItem.balance},  notShougeQuantity:{notShougeQuantity}, {nowPrice}, yu: {(balanceItem.balance - notShougeQuantity) * nowPrice}");
                            return;
                        }
                    }
                }
            }

            // 出售
            decimal sellPrice = decimal.Round(nowPrice * (decimal)0.985, symbol.PricePrecision);

            SellWhenDoEmpty(accountId, userName, symbol, sellQuantity, sellPrice);
        }

        public static string BuyWhenDoMoreAnalyze(CommonSymbol symbol, string userName, string accountId, decimal ladderBuyPercent)
        {
            AnalyzeResult analyzeResult = AnalyzeResult.GetAnalyzeResult(symbol);
            if (analyzeResult == null)
            {
                // 初始化数据, 再次拿去
                KlineUtils.InitMarketInDB(0, symbol, true);
                analyzeResult = AnalyzeResult.GetAnalyzeResult(symbol);
                if (analyzeResult == null)
                {
                    throw new ApplicationException("做多失败，分析出错");
                }
            }

            var historyKlines = analyzeResult.HistoryKlines;
            var nowPrice = analyzeResult.NowPrice;

            // 1.低于管控的购入价
            var controlCanBuy = JudgeBuyUtils.ControlCanBuy(symbol.BaseCurrency, symbol.QuoteCurrency, nowPrice);
            if (!controlCanBuy)
            {
                return $"判断 发现不适合 controlCanBuy:{controlCanBuy}";
            }

            try
            {
                BuyWhenDoMore(symbol, userName, accountId, analyzeResult, ladderBuyPercent);
            }
            catch (Exception ex)
            {
                //logger.Error($"{userName} {JsonConvert.SerializeObject(symbol)} {ex.Message}", ex);
                return ex.Message;
            }
            return "----";
        }

        #endregion

        #region 查询结果

        private static void QueryBuyDetailAndUpdate(string userName, long orderId)
        {
            try
            {
                PlatformApi api = PlatformApi.GetInstance(userName);

                var orderDetail = api.QueryOrderDetail(orderId);
                if (orderDetail.Status == "ok" && orderDetail.Data.state == "filled")
                {
                    var orderMatchResult = api.QueryOrderMatchResult(orderId);
                    decimal maxPrice = 0;
                    foreach (var item in orderMatchResult.Data)
                    {
                        if (maxPrice < item.price)
                        {
                            maxPrice = item.price;
                        }
                    }
                    if (orderMatchResult.Status == "ok")
                    {
                        new DogMoreBuyDao().UpdateDogMoreBuySuccess(orderId, orderDetail, orderMatchResult, maxPrice);
                    }
                }

                if (orderDetail.Status == "ok" && orderDetail.Data.state == StateConst.PartialCanceled)
                {
                    var orderMatchResult = api.QueryOrderMatchResult(orderId);
                    decimal maxPrice = 0;
                    decimal buyQuantity = 0;
                    foreach (var item in orderMatchResult.Data)
                    {
                        if (maxPrice < item.price)
                        {
                            maxPrice = item.price;
                        }
                        buyQuantity += item.FilledAmount;
                    }
                    if (orderMatchResult.Status == "ok")
                    {
                        new DogMoreBuyDao().UpdateDogMoreBuySuccess(orderId, buyQuantity, orderDetail, orderMatchResult, maxPrice);
                    }
                }

                if (orderDetail.Status == "ok" && orderDetail.Data.state == StateConst.Canceled)
                {
                    // 完成
                    new DogMoreBuyDao().UpdateDogMoreBuyWhenCancel(orderId);
                }
            }
            catch (Exception ex)
            {
                logger.Error("QueryBuyDetailAndUpdate  查询数据出错");
            }
        }

        private static void QuerySellDetailAndUpdate(string userName, long orderId)
        {
            try
            {
                PlatformApi api = PlatformApi.GetInstance(userName);

                var orderDetail = api.QueryOrderDetail(orderId);
                if (orderDetail.Status == "ok" && orderDetail.Data.state == "filled")
                {
                    var orderMatchResult = api.QueryOrderMatchResult(orderId);
                    decimal minPrice = 25000;
                    foreach (var item in orderMatchResult.Data)
                    {
                        if (minPrice > item.price)
                        {
                            minPrice = item.price;
                        }
                    }
                    if (orderMatchResult.Status == "ok")
                    {
                        // 完成
                        new DogMoreSellDao().UpdateDogMoreSellWhenSuccess(orderId, orderDetail, orderMatchResult, minPrice);
                    }
                }

                if (orderDetail.Status == "ok" && orderDetail.Data.state == StateConst.Canceled)
                {
                    // 完成
                    new DogMoreSellDao().UpdateDogMoreSellWhenCancel(orderId);
                }
            }
            catch (Exception ex)
            {
                logger.Error("QuerySellDetailAndUpdate 查询结果时候出错");
            }
        }

        public static void CheckBuyOrSellState()
        {
            try
            {
                var needChangeBuyStateDogMoreBuyList = new DogMoreBuyDao().ListNeedChangeBuyStateDogMoreBuy();
                foreach (var item in needChangeBuyStateDogMoreBuyList)
                {
                    // 如果长时间没有购买成功， 则取消订单。
                    if (item.BuyDate < DateTime.Now.AddMinutes(-30))
                    {
                        //api.
                    }
                    // TODO
                    QueryBuyDetailAndUpdate(item.UserName, item.BuyOrderId);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
            }

            try
            {
                var needChangeSellStateDogMoreSellList = new DogMoreSellDao().ListNeedChangeSellStateDogMoreSell();
                foreach (var item in needChangeSellStateDogMoreSellList)
                {
                    // 如果长时间没有出售成功， 则取消订单。
                    // TODO
                    QuerySellDetailAndUpdate(item.UserName, item.SellOrderId);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
            }

            // 空
            try
            {
                var needChangeSellStateDogEmptySellList = new DogEmptySellDao().ListNeedChangeSellStateDogEmptySell();
                foreach (var item in needChangeSellStateDogEmptySellList)
                {
                    // 如果长时间没有出售成功， 则取消订单。
                    // TODO
                    QueryEmptySellDetailAndUpdate(item.UserName, item.SellOrderId);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
            }

            try
            {
                var needChangeBuyStateDogEmptyBuyList = new DogEmptyBuyDao().ListNeedChangeBuyStateDogEmptyBuy();
                foreach (var item in needChangeBuyStateDogEmptyBuyList)
                {
                    // 如果长时间没有出售成功， 则取消订单。
                    // TODO
                    QueryEmptyBuyDetailAndUpdate(item.UserName, item.BuyOrderId);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
            }
        }

        private static void QueryEmptySellDetailAndUpdate(string userName, long orderId)
        {
            try
            {
                PlatformApi api = PlatformApi.GetInstance(userName);

                var orderDetail = api.QueryOrderDetail(orderId);
                if (orderDetail.Status == "ok" && orderDetail.Data.state == "filled")
                {
                    var orderMatchResult = api.QueryOrderMatchResult(orderId);
                    decimal minPrice = 25000;
                    foreach (var item in orderMatchResult.Data)
                    {
                        if (minPrice > item.price)
                        {
                            minPrice = item.price;
                        }
                    }
                    if (orderMatchResult.Status == "ok")
                    {
                        // 完成
                        new DogEmptySellDao().UpdateDogEmptySellWhenSuccess(orderId, orderDetail, orderMatchResult, minPrice);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
            }
        }

        private static void QueryEmptyBuyDetailAndUpdate(string userName, long orderId)
        {
            try
            {
                PlatformApi api = PlatformApi.GetInstance(userName);

                var orderDetail = api.QueryOrderDetail(orderId);
                if (orderDetail.Status == "ok" && orderDetail.Data.state == "filled")
                {
                    var orderMatchResult = api.QueryOrderMatchResult(orderId);
                    decimal maxPrice = 0;
                    foreach (var item in orderMatchResult.Data)
                    {
                        if (maxPrice < item.price)
                        {
                            maxPrice = item.price;
                        }
                    }
                    if (orderMatchResult.Status == "ok")
                    {
                        // 完成
                        new DogEmptyBuyDao().UpdateDogEmptyBuyWhenSuccess(orderId, orderDetail, orderMatchResult, maxPrice);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
            }
        }

        #endregion

        #region 记录日志

        private static Dictionary<string, DateTime> dic = new Dictionary<string, DateTime>();
        public static void LogNotBuy(string symbolName, string reason)
        {
            try
            {
                if (dic.ContainsKey(symbolName) && dic[symbolName] > DateTime.Now.AddMinutes(-60))
                {
                    return;
                }
                logger.Error(symbolName + " ------" + reason);
                if (dic.ContainsKey(symbolName))
                {
                    dic[symbolName] = DateTime.Now;
                }
                else
                {
                    dic.Add(symbolName, DateTime.Now);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
            }
        }

        #endregion
    }
}
