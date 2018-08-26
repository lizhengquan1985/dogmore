﻿using DogAccount;
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
    public class AnalyzeResult
    {
        /// <summary>
        /// 拐点判断标准
        /// </summary>
        public decimal FlexPercent { get; set; }
        /// <summary>
        /// 分析后的拐点数据
        /// </summary>
        public List<FlexPoint> FlexPointList { get; set; }
        public decimal NowPrice { get; set; }
        /// <summary>
        /// 原始数据
        /// </summary>
        public List<HistoryKline> HistoryKlines { get; set; }

        static ILog logger = LogManager.GetLogger(typeof(AnalyzeResult));

        /// <summary>
        /// 当购买或者出售时候,需要一个分析结果, 供判断是否做多,或者做空
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="isBuy"></param>
        /// <returns></returns>
        public static AnalyzeResult GetAnalyzeResult(CommonSymbols symbol, bool isBuy)
        {
            var key = HistoryKlinePools.GetKey(symbol, "1min");
            var historyKlineData = HistoryKlinePools.Get(key);
            if (historyKlineData == null
                || historyKlineData.Data == null
                || historyKlineData.Data.Count == 0
                || historyKlineData.Date < DateTime.Now.AddMinutes(-1))
            {
                logger.Error($"GetAnalyzeResult 数据还未准备好：{symbol.BaseCurrency}");
                return null;
            }
            var historyKlines = historyKlineData.Data;
            if (historyKlines == null
                || historyKlines.Count < 60)
            {
                return null;
            }

            // 获取最近行情
            decimal flexPercent = (decimal)1.050;
            var flexPointList = CoinAnalyze.Analyze(historyKlines, flexPercent);
            if (flexPointList == null || flexPointList.Count == 0 || (flexPointList.Count == 1 && ((isBuy && flexPointList[0].isHigh) || (!isBuy && !flexPointList[0].isHigh))))
            {
                flexPercent = (decimal)1.045;
                flexPointList = CoinAnalyze.Analyze(historyKlines, flexPercent);
            }
            if (flexPointList == null || flexPointList.Count == 0 || (flexPointList.Count == 1 && ((isBuy && flexPointList[0].isHigh) || (!isBuy && !flexPointList[0].isHigh))))
            {
                flexPercent = (decimal)1.040;
                flexPointList = CoinAnalyze.Analyze(historyKlines, flexPercent);
            }
            if (flexPointList == null || flexPointList.Count == 0 || (flexPointList.Count == 1 && ((isBuy && flexPointList[0].isHigh) || (!isBuy && !flexPointList[0].isHigh))))
            {
                flexPercent = (decimal)1.035;
                flexPointList = CoinAnalyze.Analyze(historyKlines, flexPercent);
            }
            if (flexPointList == null || flexPointList.Count == 0 || (flexPointList.Count == 1 && ((isBuy && flexPointList[0].isHigh) || (!isBuy && !flexPointList[0].isHigh))))
            {
                flexPercent = (decimal)1.03;
                flexPointList = CoinAnalyze.Analyze(historyKlines, flexPercent);
            }
            if (flexPointList == null || flexPointList.Count == 0 || (flexPointList.Count == 1 && ((isBuy && flexPointList[0].isHigh) || (!isBuy && !flexPointList[0].isHigh))))
            {
                flexPercent = (decimal)1.025;
                flexPointList = CoinAnalyze.Analyze(historyKlines, flexPercent);
            }
            if (flexPointList == null || flexPointList.Count == 0 || (flexPointList.Count == 1 && ((isBuy && flexPointList[0].isHigh) || (!isBuy && !flexPointList[0].isHigh))))
            {
                flexPercent = (decimal)1.02;
                flexPointList = CoinAnalyze.Analyze(historyKlines, flexPercent);
            }
            if (flexPointList == null || flexPointList.Count == 0 || (flexPointList.Count == 1 && ((isBuy && flexPointList[0].isHigh) || (!isBuy && !flexPointList[0].isHigh))))
            {
                flexPercent = (decimal)1.015;
                flexPointList = CoinAnalyze.Analyze(historyKlines, flexPercent);
            }
            if (flexPointList == null || flexPointList.Count == 0)
            {
                //logger.Error($"--------------> 分析{symbol.BaseCurrency}的flexPoint结果数量为0 ");
                return null;
            }

            AnalyzeResult analyzeResult = new AnalyzeResult()
            {
                FlexPointList = flexPointList,
                NowPrice = historyKlines[0].Close,
                HistoryKlines = historyKlines,
                FlexPercent = flexPercent
            };
            return analyzeResult;
        }
    }

    public class CoinTrade
    {
        static ILog logger = LogManager.GetLogger(typeof(CoinTrade));

        public static void Run(CommonSymbols symbol)
        {
            try
            {
                AnalyzeResult analyzeResult = AnalyzeResult.GetAnalyzeResult(symbol, true);
                if (analyzeResult != null)
                {
                    // 计算是否适合购买
                    RunBuy(symbol, analyzeResult);
                }
            }
            catch (Exception ex)
            {
                logger.Error("---> 购买异常: " + ex.Message, ex);
            }

            try
            {
                AnalyzeResult analyzeResult = AnalyzeResult.GetAnalyzeResult(symbol, false);
                if (analyzeResult != null)
                {
                    // 计算是否适合出售
                    RunSell(symbol, analyzeResult);
                }
            }
            catch (Exception ex)
            {
                logger.Error("---> 出售异常: " + ex.Message, ex);
            }
        }

        private static void RunBuy(CommonSymbols symbol, AnalyzeResult analyzeResult)
        {
            var flexPointList = analyzeResult.FlexPointList;
            var historyKlines = analyzeResult.HistoryKlines;
            var nowPrice = analyzeResult.NowPrice;
            var flexPercent = analyzeResult.FlexPercent;

            // 1.最近一次是最高点
            // 2.不是快速拉升的.
            // 3.低于管控的购入价
            if (flexPointList[0].isHigh
                || JudgeBuyUtils.IsQuickRise(symbol.BaseCurrency, historyKlines)
                || !JudgeBuyUtils.ControlCanBuy(symbol.BaseCurrency, nowPrice))
            {
                return;
            }

            var userNames = UserPools.GetAllUserName();

            // 自动波动做多
            foreach (var userName in userNames)
            {
                AccountConfig accountConfig = AccountConfigUtils.GetAccountConfig(userName);
                var accountId = accountConfig.MainAccountId;

                try
                {
                    BuyWhenDoMore(symbol, userName, accountId, analyzeResult);
                }
                catch (Exception ex)
                {
                    continue;
                }
            }

            // 空单的自动波动收割
            foreach (var userName in userNames)
            {
                AccountConfig accountConfig = AccountConfigUtils.GetAccountConfig(userName);
                var accountId = accountConfig.MainAccountId;

                var needBuyDogEmptySellList = new DogEmptySellDao().GetNeedBuyDogEmptySell(accountId, userName, symbol.BaseCurrency);
                Console.WriteLine(symbol.BaseCurrency + $", nowPrice:{nowPrice} 空单数量：" + needBuyDogEmptySellList.Count + "--");
                foreach (var item in needBuyDogEmptySellList)
                {
                    Console.WriteLine(item.SellTradePrice);
                }

                var canBuy = JudgeBuyUtils.CheckCanBuyForHuiDiao(nowPrice, flexPointList[0].close);
                if (!canBuy)
                {
                    continue;
                }

                var ladderBuyPercent = DogControlUtils.GetLadderBuy(symbol.BaseCurrency, nowPrice);
                PlatformApi api = PlatformApi.GetInstance(userName);

                foreach (var needBuyDogEmptySellItem in needBuyDogEmptySellList)
                {
                    ShouGeEmpty(needBuyDogEmptySellItem, symbol, analyzeResult, ladderBuyPercent);
                }
            }
        }

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

        public static void ShouGeEmpty(DogEmptySell dogEmptySell, CommonSymbols symbol, AnalyzeResult analyzeResult, decimal percent = (decimal)1.02)
        {
            var nowPrice = analyzeResult.NowPrice;
            if (nowPrice * percent > dogEmptySell.SellTradePrice)
            {
                return;
            }

            decimal buyQuantity = CommonHelper.CalcBuyQuantityForEmptyShouge(dogEmptySell.SellQuantity, dogEmptySell.SellTradePrice, nowPrice, symbol.AmountPrecision);
            decimal orderPrice = decimal.Round(nowPrice * (decimal)1.005, symbol.PricePrecision);

            OrderPlaceRequest req = new OrderPlaceRequest();
            req.account_id = dogEmptySell.AccountId;
            req.amount = buyQuantity.ToString();
            req.price = orderPrice.ToString();
            req.source = "api";
            req.symbol = symbol.BaseCurrency + symbol.QuoteCurrency;
            req.type = "buy-limit";

            if (BuyLimitUtils.Record(dogEmptySell.UserName, symbol.BaseCurrency))
            {
                logger.Error(" --------------------- 两个小时内购买次数太多，暂停一会 --------------------- ");
                logger.Error(" --------------------- 两个小时内购买次数太多，暂停一会 --------------------- ");
                logger.Error(" --------------------- 两个小时内购买次数太多，暂停一会 --------------------- ");
                Thread.Sleep(1000 * 5);
                return;
            }

            PlatformApi api = PlatformApi.GetInstance(dogEmptySell.UserName);
            HBResponse<long> order = api.OrderPlace(req);
            if (order.Status == "ok")
            {
                new DogEmptyBuyDao().CreateDogEmptyBuy(new DogEmptyBuy()
                {
                    SymbolName = symbol.BaseCurrency,
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
                    BuyFlex = JsonConvert.SerializeObject(analyzeResult.FlexPointList),
                    BuyMemo = "",
                    BuyOrderDetail = "",
                    BuyOrderMatchResults = "",
                });
                // 下单成功马上去查一次
                QueryEmptyBuyDetailAndUpdate(dogEmptySell.UserName, order.Data);
            }
            logger.Error($"下单 --> 空单收割-下单购买结果 {JsonConvert.SerializeObject(req)}, order：{JsonConvert.SerializeObject(order)}, ,nowPrice：{nowPrice}, accountId：{dogEmptySell.AccountId}");
            logger.Error($"下单 --> 空单收割-下单购买结果 分析 {JsonConvert.SerializeObject(analyzeResult.FlexPointList)}");
        }

        public static void ShouGeMore(DogMoreBuy dogMoreBuy, decimal percent)
        {
            var symbols = CoinUtils.GetAllCommonSymbols();
            CommonSymbols symbol = symbols.Find(it => it.BaseCurrency == dogMoreBuy.SymbolName);

            AnalyzeResult analyzeResult = AnalyzeResult.GetAnalyzeResult(symbol, true);
            if (analyzeResult == null)
            {
                return;
            }

            var flexPointList = analyzeResult.FlexPointList;
            var nowPrice = analyzeResult.NowPrice;

            // 没有大于预期, 也不能收割
            if (nowPrice < dogMoreBuy.BuyTradePrice * percent || nowPrice < dogMoreBuy.BuyTradePrice * (decimal)1.03)
            {
                return;
            }

            // 判断是否有回调,以及是否大于percent
            if (!JudgeSellUtils.CheckCanSellForHuiDiao(nowPrice, flexPointList[0].close))
            {
                return;
            }

            decimal sellQuantity = JudgeSellUtils.CalcSellQuantityForMoreShouge(dogMoreBuy.BuyQuantity, dogMoreBuy.BuyTradePrice, nowPrice, symbol);
            if (sellQuantity >= dogMoreBuy.BuyQuantity)
            {
                // 一定要赚才能出售
                logger.Error($"{dogMoreBuy.SymbolName} sellQuantity:{sellQuantity}, BuyQuantity:{dogMoreBuy.BuyQuantity}");
                return;
            }

            // 出售
            decimal sellPrice = decimal.Round(nowPrice * (decimal)0.985, symbol.PricePrecision);
            OrderPlaceRequest req = new OrderPlaceRequest();
            req.account_id = dogMoreBuy.AccountId;
            req.amount = sellQuantity.ToString();
            req.price = sellPrice.ToString();
            req.source = "api";
            req.symbol = symbol.BaseCurrency + symbol.QuoteCurrency; ;
            req.type = "sell-limit";
            PlatformApi api = PlatformApi.GetInstance(dogMoreBuy.UserName);
            HBResponse<long> order = api.OrderPlace(req);
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
                    SellFlex = JsonConvert.SerializeObject(flexPointList),
                    SellQuantity = sellQuantity,
                    SellOrderPrice = sellPrice,
                    SellState = StateConst.Submitted,
                    SellTradePrice = 0,
                    SymbolName = symbol.BaseCurrency,
                    SellMemo = "",
                    SellOrderDetail = "",
                    SellOrderMatchResults = ""
                };
                new DogMoreSellDao().CreateDogMoreSell(dogMoreSell);

                // 下单成功马上去查一次
                QuerySellDetailAndUpdate(dogMoreBuy.UserName, order.Data);
            }
            logger.Error($"下单出售 --> 下单出售结果 {JsonConvert.SerializeObject(req)}, order：{JsonConvert.SerializeObject(order)},nowPrice：{nowPrice}，accountId：{dogMoreBuy.AccountId}");
        }

        private static void QueryBuyDetailAndUpdate(string userName, long orderId)
        {
            try
            {
                PlatformApi api = PlatformApi.GetInstance(userName);

                var orderDetail = api.QueryOrderDetail(orderId);
                if (orderDetail.Status == "ok" && orderDetail.Data.state == "filled")
                {
                    logger.Error(JsonConvert.SerializeObject(orderDetail));
                    var orderMatchResult = api.QueryOrderMatchResult(orderId);
                    logger.Error("------------> " + JsonConvert.SerializeObject(orderMatchResult));
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
                    logger.Error(JsonConvert.SerializeObject(orderDetail));
                    var orderMatchResult = api.QueryOrderMatchResult(orderId);
                    logger.Error("------------> " + JsonConvert.SerializeObject(orderMatchResult));
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
                logger.Error(ex.Message, ex);
            }
        }

        private static void RunSell(CommonSymbols symbol, AnalyzeResult analyzeResult)
        {
            var flexPointList = analyzeResult.FlexPointList;
            var historyKlines = analyzeResult.HistoryKlines;
            var nowPrice = analyzeResult.NowPrice;
            var flexPercent = analyzeResult.FlexPercent;

            if (!flexPointList[0].isHigh)
            {
                // 最低点 不适合出售
                return;
            }

            // 多单的自动波动收割
            var userNames = UserPools.GetAllUserName();
            foreach (var userName in userNames)
            {
                var accountConfig = AccountConfigUtils.GetAccountConfig(userName);
                var accountId = accountConfig.MainAccountId;
                var needSellDogMoreBuyList = new DogMoreBuyDao().GetNeedSellDogMoreBuy(accountId, userName, symbol.BaseCurrency);

                foreach (var needSellDogMoreBuyItem in needSellDogMoreBuyList)
                {
                    decimal gaoyuPercentSell = DogControlUtils.GetLadderSell(needSellDogMoreBuyItem.SymbolName, nowPrice); //(decimal)1.035;

                    try
                    {
                        ShouGeMore(needSellDogMoreBuyItem, gaoyuPercentSell);
                    }
                    catch (Exception ex)
                    {
                        continue;
                    }
                }
            }

            // 自动做空
            // 要求  1. 进入拐点区域, 2. 受管控未过期
            var control = new DogControlDao().GetDogControl(symbol.BaseCurrency);
            if (nowPrice * (decimal)1.02 > flexPointList[0].close && nowPrice * (decimal)1.005 < flexPointList[0].close
                && control != null && nowPrice >= control.EmptyPrice && control.EmptyExpiredTime > DateTime.Now
                && nowPrice >= control.HistoryMin * (decimal)1.4 && control.HistoryMin > 0 && nowPrice >= (control.HistoryMax - control.HistoryMin) * (decimal)0.2 + control.HistoryMin)
            {
                foreach (var userName in userNames)
                {
                    try
                    {
                        // 和上次做空价格要相差8%
                        var maxSellTradePrice = new DogEmptySellDao().GetMaxSellTradePrice(userName, symbol.BaseCurrency);
                        if (maxSellTradePrice != null && nowPrice < maxSellTradePrice * (decimal)1.10)
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
                            logger.Error($"未收割得数量大于余额，有些不合理，  {symbol.BaseCurrency},, {userName},, {notShougeQuantity}, {balanceItem.balance}");
                            continue;
                        }

                        var devide = DogControlUtils.GetEmptySellDivide(symbol.BaseCurrency, nowPrice);
                        decimal sellQuantity = (balanceItem.balance - notShougeQuantity) / devide; // 暂定每次做空1/12
                        if (sellQuantity * nowPrice > 10)
                        {
                            sellQuantity = 10 / nowPrice;
                        }
                        sellQuantity = decimal.Round(sellQuantity, symbol.AmountPrecision);
                        if (sellQuantity * nowPrice < 2)
                        {
                            LogNotBuy(symbol.BaseCurrency, $"收益不超过2usdt,, balance: {balanceItem.balance},  notShougeQuantity:{notShougeQuantity}, {nowPrice}, yu: {(balanceItem.balance - notShougeQuantity) * nowPrice}");
                            continue;
                        }

                        // 出售
                        decimal sellPrice = decimal.Round(nowPrice * (decimal)0.985, symbol.PricePrecision);
                        EmtpyTrade(accountId, userName, symbol, sellQuantity, sellPrice, flexPointList, $"device:{devide}");

                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex.Message, ex);
                    }
                }
            }
        }

        private static void EmtpyTrade(string accountId, string userName, CommonSymbols symbol, decimal sellQuantity, decimal sellPrice, List<FlexPoint> flexPointList, string sellMemo = "")
        {
            OrderPlaceRequest req = new OrderPlaceRequest();
            try
            {
                if (sellQuantity < symbol.AmountPrecision)
                {
                    logger.Error($"sell 出错,{symbol.BaseCurrency} 的精度为 {symbol.AmountPrecision}, 但是却要出售{sellQuantity}  ");
                    return;
                }

                PlatformApi api = PlatformApi.GetInstance(userName);
                req.account_id = accountId;
                req.amount = sellQuantity.ToString();
                req.price = sellPrice.ToString();
                req.source = "api";
                req.symbol = symbol.BaseCurrency + symbol.QuoteCurrency; ;
                req.type = "sell-limit";
                HBResponse<long> order = api.OrderPlace(req);
                logger.Error("下单 --> 下单出售结果：" + JsonConvert.SerializeObject(order));
                if (order.Status == "ok")
                {
                    try
                    {
                        DogEmptySell dogEmptySell = new DogEmptySell()
                        {
                            AccountId = accountId,
                            UserName = userName,
                            SellOrderId = order.Data,
                            SellOrderResult = JsonConvert.SerializeObject(order),
                            SellDate = DateTime.Now,
                            SellFlex = JsonConvert.SerializeObject(flexPointList),
                            SellQuantity = sellQuantity,
                            SellOrderPrice = sellPrice,
                            SellState = StateConst.Submitted,
                            SellTradePrice = 0,
                            SymbolName = symbol.BaseCurrency,
                            SellMemo = sellMemo,
                            SellOrderDetail = "",
                            SellOrderMatchResults = "",
                            FlexPercent = (decimal)1.04,
                            IsFinished = false
                        };
                        new DogEmptySellDao().CreateDogEmptySell(dogEmptySell);
                    }
                    catch (Exception ex)
                    {
                        logger.Error("------RunSell----危险-----------");
                        logger.Error(ex.Message, ex);
                    }

                    // 下单成功马上去查一次
                    QueryEmptySellDetailAndUpdate(userName, order.Data);
                }
            }
            catch (Exception ex)
            {
                logger.Error("sell下单出错：" + JsonConvert.SerializeObject(req) + "--> " + ex.Message);
            }
        }

        public static void DoEmpty(CommonSymbols symbol, string userName, string accountId)
        {
            AnalyzeResult analyzeResult = AnalyzeResult.GetAnalyzeResult(symbol, false);
            if (analyzeResult == null)
            {
                throw new ApplicationException("做空失败，分析出错");
            }
            var flexPointList = analyzeResult.FlexPointList;
            var nowPrice = analyzeResult.NowPrice;
            if (!flexPointList[0].isHigh)
            {
                // 最低点 不适合出售
                throw new ApplicationException("做空失败， 最近不是最高");
            }

            if (nowPrice * (decimal)1.03 < flexPointList[0].close)
            {
                throw new ApplicationException("已经降低了3%， 不要做空，谨慎起见");
            }

            var maxSellTradePrice = new DogEmptySellDao().GetMaxSellTradePrice(userName, symbol.BaseCurrency);
            if (maxSellTradePrice != null && nowPrice < maxSellTradePrice * (decimal)1.02)
            {
                throw new ApplicationException("有价格比这个更高得还没有收割。不能重新做空。");
            }

            PlatformApi api = PlatformApi.GetInstance(userName);

            var accountInfo = api.GetAccountBalance(AccountConfigUtils.GetAccountConfig(userName).MainAccountId);
            var balanceItem = accountInfo.Data.list.Find(it => it.currency == symbol.BaseCurrency);
            var amount = balanceItem.balance * nowPrice;
            var sellAmout = Math.Max(amount / 20, 5);
            sellAmout = Math.Min(sellAmout, 10);

            decimal sellQuantity = sellAmout / nowPrice; // 暂定每次做空5美元
            sellQuantity = decimal.Round(sellQuantity, symbol.AmountPrecision);
            if (symbol.BaseCurrency == "xrp" && sellQuantity < 1)
            {
                sellQuantity = 1;
            }

            // 出售
            decimal sellPrice = decimal.Round(nowPrice * (decimal)0.985, symbol.PricePrecision);
            EmtpyTrade(accountId, userName, symbol, sellQuantity, sellPrice, flexPointList);
        }

        public static void BuyWhenDoMoreAnalyze(CommonSymbols symbol, string userName, string accountId)
        {
            AnalyzeResult analyzeResult = AnalyzeResult.GetAnalyzeResult(symbol, true);
            if (analyzeResult == null)
            {
                throw new ApplicationException("做多失败，分析出错");
            }

            var flexPointList = analyzeResult.FlexPointList;
            var historyKlines = analyzeResult.HistoryKlines;
            var nowPrice = analyzeResult.NowPrice;
            var flexPercent = analyzeResult.FlexPercent;

            // 1.最近一次是最高点
            // 2.不是快速拉升的.
            // 3.低于管控的购入价
            if (flexPointList[0].isHigh
                || JudgeBuyUtils.IsQuickRise(symbol.BaseCurrency, historyKlines)
                || !JudgeBuyUtils.ControlCanBuy(symbol.BaseCurrency, nowPrice))
            {
                return;
            }

            BuyWhenDoMore(symbol, userName, accountId, analyzeResult);
        }

        /// <summary>
        /// 购买,做多的时候
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="userName"></param>
        /// <param name="accountId"></param>
        public static void BuyWhenDoMore(CommonSymbols symbol, string userName, string accountId, AnalyzeResult analyzeResult)
        {
            var flexPointList = analyzeResult.FlexPointList;
            var flexPercent = analyzeResult.FlexPercent;
            var nowPrice = analyzeResult.NowPrice;

            // 判断购入阶梯
            var ladderBuyPercent = DogControlUtils.GetLadderBuy(symbol.BaseCurrency, nowPrice);
            var minBuyTradePrice = new DogMoreBuyDao().GetMinBuyPriceOfNotSellFinished(accountId, userName, symbol.BaseCurrency);
            if (minBuyTradePrice <= 0)
            {
                if (symbol.BaseCurrency == "新的币")
                {
                    minBuyTradePrice = 25000;
                }
                else
                {
                    logger.Error("获取上一次最小购入价位出错");
                    return;
                }
            }
            if (nowPrice * ladderBuyPercent > minBuyTradePrice || nowPrice * (decimal)1.05 >= minBuyTradePrice)
            {
                throw new ApplicationException("有价格比这个更低得还没有收割。不能重新做多。");
            }

            // 判断是否回调0.5%
            if (!JudgeBuyUtils.CheckCanBuyForHuiDiao(nowPrice, flexPointList[0].close))
            {
                return;
            }

            PlatformApi api = PlatformApi.GetInstance(userName);
            var accountInfo = api.GetAccountBalance(accountId);
            var usdt = accountInfo.Data.list.Find(it => it.currency == "usdt");
            // 要减去空单未收割得额度总和
            var notShougeEmptySellAmount = new DogEmptySellDao().GetSumNotShougeDogEmptySell(userName);
            if (notShougeEmptySellAmount >= usdt.balance)
            {
                // 余额不足
                LogNotBuy(symbol.BaseCurrency, $"余额不足,  checkNotShougeEmptySellAmount -> notShougeEmptySellAmount:{notShougeEmptySellAmount},usdt.balance:{usdt.balance}");
                return;
            }
            decimal recommendAmount = (usdt.balance - notShougeEmptySellAmount) / DogControlUtils.GetRecommendDivide(symbol.BaseCurrency, nowPrice);

            if (recommendAmount < (decimal)1.1)
            {
                recommendAmount = (decimal)1.1;
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
            if (BuyLimitUtils.Record(userName, symbol.BaseCurrency))
            {
                logger.Error(" --------------------- 两个小时内购买次数太多，暂停一会 --------------------- ");
                logger.Error(" --------------------- 两个小时内购买次数太多，暂停一会 --------------------- ");
                logger.Error(" --------------------- 两个小时内购买次数太多，暂停一会 --------------------- ");
                Thread.Sleep(1000 * 10);
                return;
            }

            HBResponse<long> order = api.OrderPlace(req);
            if (order.Status == "ok")
            {
                new DogMoreBuyDao().CreateDogMoreBuy(new DogMoreBuy()
                {
                    SymbolName = symbol.BaseCurrency,
                    AccountId = accountId,
                    UserName = userName,
                    FlexPercent = flexPercent,

                    BuyQuantity = buyQuantity,
                    BuyOrderPrice = orderPrice,
                    BuyDate = DateTime.Now,
                    BuyOrderResult = JsonConvert.SerializeObject(order),
                    BuyState = StateConst.PreSubmitted,
                    BuyTradePrice = 0,
                    BuyOrderId = order.Data,
                    BuyFlex = JsonConvert.SerializeObject(flexPointList),
                    BuyMemo = "",
                    BuyOrderDetail = "",
                    BuyOrderMatchResults = "",
                    IsFinished = false
                });

                // 下单成功马上去查一次
                QueryBuyDetailAndUpdate(userName, order.Data);
            }
            logger.Error($"下单 --> 下单购买结果 {JsonConvert.SerializeObject(req)}, notShougeEmptySellAmount:{notShougeEmptySellAmount}, order：{JsonConvert.SerializeObject(order)}, 上一次最低购入价位：{minBuyTradePrice},nowPrice：{nowPrice}, accountId：{accountId},分析 {JsonConvert.SerializeObject(flexPointList)}");
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
                logger.Error(ex.Message, ex);
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

        private static void QueryEmptyBuyDetailAndUpdate(string userName, long orderId)
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
    }
}
