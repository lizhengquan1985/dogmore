using DogPlatform.Model;
using DogService.DateTypes;
using DogService.DTO;
using log4net;
using Newtonsoft.Json;
using SharpDapper;
using SharpDapper.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DogService.Dao
{
    public class DogMoreBuyDao : BaseDao
    {
        static ILog logger = LogManager.GetLogger(typeof(DogMoreBuyDao));

        public DogMoreBuyDao() : base()
        {
        }

        public DogMoreBuy GetDogMoreBuyByBuyOrderId(long orderId)
        {
            var sql = $"select * from t_dog_more_buy where BuyOrderId={orderId}";
            return Database.Query<DogMoreBuy>(sql).FirstOrDefault();
        }

        /// <summary>
        /// TODO 计算方式考虑交叉交易
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="quoteCurrency"></param>
        /// <param name="symbolName"></param>
        /// <returns></returns>
        public decimal GetBuyQuantityNotShouge(string userName, string symbolName)
        {
            var states2 = GetStateStringIn(new List<string>() { StateConst.PartialCanceled, StateConst.Filled, StateConst.Canceled });
            var sql = $"select sum(BuyQuantity) BuyQuantity from t_dog_more_buy where IsFinished=0 and UserName=@userName and SymbolName=@symbolName";
            var res = Database.Query<decimal?>(sql, new { userName, symbolName }).FirstOrDefault();
            if (res == null)
            {
                return 0;
            }
            return (decimal)res;
        }

        public void CreateDogMoreBuy(DogMoreBuy dogMoreBuy)
        {
            try
            {
                using (var tx = Database.BeginTransaction())
                {
                    Database.Insert(dogMoreBuy);
                    tx.Commit();
                }
            }
            catch (Exception ex)
            {
                logger.Error($"严重 --------危险----- CreateDogMoreBuy ------ 防止出错时候, 无限购买. 业务上不能出错, {JsonConvert.SerializeObject(dogMoreBuy)}");
                logger.Error(ex.Message, ex);
                Thread.Sleep(1000 * 60 * 60);
            }
        }

        #region 先查找出需要查询购买或者出售结果的记录， 然后查询结果，最后修改数据库记录

        /// <summary>
        /// 列出需要改变购买状态的记录
        /// </summary>
        /// <param name="accountId"></param>
        /// <returns></returns>
        public List<DogMoreBuy> ListNeedChangeBuyStateDogMoreBuy()
        {
            var states = $"'{StateConst.PartialCanceled}','{StateConst.Filled}'";
            var sql = $"select * from t_dog_more_buy where BuyState not in({states})";
            return Database.Query<DogMoreBuy>(sql).ToList();
        }

        public void UpdateDogMoreBuySuccess(long buyOrderId, HBResponse<OrderDetail> orderDetail, HBResponse<List<OrderMatchResult>> orderMatchResult, decimal buyTradePrice)
        {
            using (var tx = Database.BeginTransaction())
            {
                var sql = $"update t_dog_more_buy set BuyTradePrice={buyTradePrice}, BuyState='{orderDetail.Data.state}' ,BuyOrderDetail='{JsonConvert.SerializeObject(orderDetail)}', BuyOrderMatchResults='{JsonConvert.SerializeObject(orderMatchResult)}' where BuyOrderId ='{buyOrderId}'";
                Database.Execute(sql);
                tx.Commit();
            }
        }

        public void UpdateDogMoreBuySuccess(long buyOrderId, decimal buyQuantity, HBResponse<OrderDetail> orderDetail, HBResponse<List<OrderMatchResult>> orderMatchResult, decimal buyTradePrice)
        {
            using (var tx = Database.BeginTransaction())
            {
                var sql = $"update t_dog_more_buy set BuyQuantity={buyQuantity}, BuyTradePrice={buyTradePrice}, BuyState='{orderDetail.Data.state}' ,BuyOrderDetail='{JsonConvert.SerializeObject(orderDetail)}', BuyOrderMatchResults='{JsonConvert.SerializeObject(orderMatchResult)}' where BuyOrderId ='{buyOrderId}'";
                Database.Execute(sql);
                tx.Commit();
            }
        }

        public void UpdateDogMoreBuyWhenCancel(long buyOrderId)
        {
            using (var tx = Database.BeginTransaction())
            {
                var sql = $"update t_dog_more_buy set BuyState='{StateConst.Canceled}' where BuyOrderId ='{buyOrderId}'";
                Database.Execute(sql);
                tx.Commit();
            }
        }

        #endregion

        public List<DogMoreBuy> GetNeedSellDogMoreBuy(string accountId, string userName, string quoteCurrency, string symbolName)
        {
            var states = GetStateStringIn(new List<string>() { StateConst.PartialCanceled, StateConst.Filled });
            var states2 = GetStateStringIn(new List<string>() { StateConst.PartialCanceled, StateConst.Filled, StateConst.Canceled });
            var sql = $"select * from t_dog_more_buy where AccountId='{accountId}' and QuoteCurrency = '{quoteCurrency}' and SymbolName = '{symbolName}' and BuyState in({states}) and IsFinished=0 " +
                $" and UserName='{userName}' and BuyOrderId not in(select BuyOrderId from t_dog_more_sell where AccountId='{accountId}' and UserName='{userName}' and QuoteCurrency = '{quoteCurrency}' and SellState not in({states})) " +
                $" order by BuyTradePrice asc limit 0,8";
            return Database.Query<DogMoreBuy>(sql).ToList();
        }

        /// <summary>
        /// 不分角色的，拉取最小的那个数据
        /// </summary>
        /// <param name="quoteCurrency"></param>
        /// <param name="baseCurrency"></param>
        /// <returns></returns>
        public DogMoreBuy GetSmallestDogMoreBuy(string quoteCurrency, string baseCurrency)
        {
            var states = GetStateStringIn(new List<string>() { StateConst.PartialCanceled, StateConst.Filled });
            var states2 = GetStateStringIn(new List<string>() { StateConst.PartialCanceled, StateConst.Filled, StateConst.Canceled });
            var sql = $"select * from t_dog_more_buy where SymbolName = '{baseCurrency}' and QuoteCurrency='{quoteCurrency}' and BuyState in({states}) and IsFinished=0 " +
                $" and BuyOrderId not in(select BuyOrderId from t_dog_more_sell where QuoteCurrency='{quoteCurrency}' and SellState not in({states})) " +
                $" order by BuyTradePrice asc limit 0,1";
            return Database.Query<DogMoreBuy>(sql).FirstOrDefault();
        }

        /// <summary>
        /// 获取最小的购买价格.
        /// </summary>
        /// <param name="accountId"></param>
        /// <param name="userName"></param>
        /// <param name="coin"></param>
        /// <returns></returns>
        public decimal GetMinBuyPriceOfNotSellFinished(string accountId, string userName, string quoteCurrency, string coin)
        {
            var sql = $"select * from t_dog_more_buy where AccountId='{accountId}' and QuoteCurrency = '{quoteCurrency}' and SymbolName = '{coin}' and BuyState!='({StateConst.Canceled.ToString()})' " +
                $" and IsFinished=0 and UserName='{userName}'";
            var list = Database.Query<DogMoreBuy>(sql).ToList();
            var minPrice = (decimal)25000;
            foreach (var item in list)
            {
                if (item.BuyTradePrice > 0 && item.BuyTradePrice < minPrice)
                {
                    minPrice = item.BuyTradePrice;
                }
                if (item.BuyTradePrice <= 0 && item.BuyOrderPrice < minPrice)
                {
                    minPrice = item.BuyOrderPrice;
                }
            }
            return minPrice;
        }

        public decimal GetMaxPriceOfNotSellFinished(string quoteCurrency, string symbolName)
        {
            var sql = $"select * from t_dog_more_buy where SymbolName=@SymbolName and QuoteCurrency=@quoteCurrency and BuyState!='({StateConst.Canceled.ToString()})' " +
                $" and IsFinished=0 ";
            var list = Database.Query<DogMoreBuy>(sql, new { SymbolName = symbolName, quoteCurrency }).ToList();
            var maxPrice = (decimal)0;
            foreach (var item in list)
            {
                if (item.BuyTradePrice > 0 && item.BuyTradePrice > maxPrice)
                {
                    maxPrice = item.BuyTradePrice;
                }
                if (item.BuyTradePrice <= 0 && item.BuyOrderPrice > maxPrice)
                {
                    maxPrice = item.BuyOrderPrice;
                }
            }
            return maxPrice;
        }

        public DogMoreBuy GetByBuyOrderId(long buyOrderId)
        {
            var sql = $"select * from t_dog_more_buy where BuyOrderId={buyOrderId}";
            return Database.Query<DogMoreBuy>(sql).FirstOrDefault();
        }

        public decimal GetBuyQuantityOfDogMoreBuyIsNotFinished(string userName, string symbolName)
        {
            var where = $" where IsFinished=0";
            if (!string.IsNullOrEmpty(userName))
            {
                where += $" and UserName = @userName ";
            }
            if (!string.IsNullOrEmpty(symbolName))
            {
                where += $" and SymbolName = @symbolName ";
            }
            var sql = $"select sum(BuyQuantity) from t_dog_more_buy {where} order by BuyTradePrice asc";
            decimal? q = Database.Query<decimal?>(sql, new { symbolName, userName }).FirstOrDefault();
            if (q == null)
            {
                q = (decimal)0;
            }
            return (decimal)q;
        }

        public List<DogMoreBuy> listMoreBuyIsNotFinished(string userName, string symbolName, string quoteCurrency)
        {

            var where = $" where IsFinished=0 and QuoteCurrency=@quoteCurrency ";
            if (!string.IsNullOrEmpty(userName))
            {
                where += $" and UserName = @userName ";
            }
            if (!string.IsNullOrEmpty(symbolName))
            {
                where += $" and SymbolName = @symbolName ";
            }
            var sql = $"select * from t_dog_more_buy {where} order by BuyTradePrice asc";
            return Database.Query<DogMoreBuy>(sql, new { symbolName, userName, quoteCurrency }).ToList();
        }

        public List<DogMoreBuy> listEveryMinPriceMoreBuyIsNotFinished(string userName, string quoteCurrency)
        {
            var sql = $"select * from t_dog_more_buy where BuyOrderId in(" +
                $" select BuyOrderId from ( " +
                $"  select max(BuyOrderId+0) BuyOrderId,SymbolName from t_dog_more_buy where quoteCurrency=@quoteCurrency and {(string.IsNullOrEmpty(userName) ? "" : $" UserName = @userName and ")} IsFinished=0 group by SymbolName) t)  ";
            return Database.Query<DogMoreBuy>(sql, new { userName, quoteCurrency }).ToList();
        }

        public List<DogMoreBuy> listDogMoreBuyIsFinished(string userName, string symbolName)
        {
            var where = $" where IsFinished=1 and SymbolName like @symbolName ";
            if (!string.IsNullOrEmpty(userName))
            {
                where += $" and UserName=@userName";
            }
            var sql = $"select * from t_dog_more_buy {where} order by BuyTradePrice asc limit 0,100";
            return Database.Query<DogMoreBuy>(sql, new { symbolName = LikeStr(symbolName), userName }).ToList();
        }

        public void Delete(long buyOrderId)
        {
            var dogMoreBuy = GetByBuyOrderId(buyOrderId);
            if (dogMoreBuy.BuyState != StateConst.PartialCanceled && dogMoreBuy.BuyState != StateConst.Filled && dogMoreBuy.BuyState != StateConst.PartialFilled)
            {
                throw new ApplicationException("未取消或者未完成的订单，不能删除");
            }

            var sql = $"delete from t_dog_more_sell where BuyOrderId={buyOrderId}";
            Database.Execute(sql);
            sql = $"delete from t_dog_more_buy where BuyOrderId={buyOrderId}";
            Database.Execute(sql);
        }

        public List<DogMoreBuyNotFinishedStatistics> ListDogMoreBuyNotFinishedStatistics(string userName, string quoteCurrency)
        {
            var where = " where IsFinished=0 and quoteCurrency=@quoteCurrency ";
            if (!string.IsNullOrEmpty(userName))
            {
                where += $" and UserName=@userName ";
            }
            var sql = $"select * from ( select SymbolName, min(BuyTradePrice) MinPrice, max(BuyTradePrice) MaxPrice, sum(BuyQuantity) TotalQuantity, sum(BuyQuantity*BuyTradePrice) TotalAmount, count(1) Count" +
                $" from t_dog_more_buy {where} group by SymbolName ) t order by SymbolName asc";
            return Database.Query<DogMoreBuyNotFinishedStatistics>(sql, new { userName, quoteCurrency }).ToList();
        }
    }
}
