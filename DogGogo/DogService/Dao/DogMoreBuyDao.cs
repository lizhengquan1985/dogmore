using DogPlatform.Model;
using DogService.DateTypes;
using log4net;
using Newtonsoft.Json;
using SharpDapper;
using SharpDapper.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public decimal GetBuyQuantityNotShouge(string symbolName)
        {
            var states2 = GetStateStringIn(new List<string>() { StateConst.PartialCanceled, StateConst.Filled, StateConst.Canceled });
            var sql = $"select sum(BuyQuantity) from t_dog_more_buy where SymbolName=@symbolName and BuyStat not in(states2)";
            return Database.Query<decimal>(sql).FirstOrDefault();
        }

        public void CreateDogMoreBuy(DogMoreBuy dogMoreBuy)
        {
            using (var tx = Database.BeginTransaction())
            {
                Database.Insert(dogMoreBuy);
                tx.Commit();
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
            var states = $"'{StateConst.PartialFilled}','{StateConst.Filled}'";
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

        public List<DogMoreBuy> GetNeedSellDogMoreBuy(string accountId, string userName, string symbolName)
        {
            var states = GetStateStringIn(new List<string>() { StateConst.PartialCanceled, StateConst.Filled });
            var states2 = GetStateStringIn(new List<string>() { StateConst.PartialCanceled, StateConst.Filled, StateConst.Canceled });
            var sql = $"select * from t_dog_more_buy where AccountId='{accountId}' and SymbolName = '{symbolName}' and BuyState in({states}) and IsFinished=0 " +
                $" and UserName='{userName}' and BuyOrderId not in(select BuyOrderId from t_dog_more_sell where AccountId='{accountId}' and UserName='{userName}' and SellState not in({states})) " +
                $" order by BuyTradePrice asc limit 0,8";
            return Database.Query<DogMoreBuy>(sql).ToList();
        }

        /// <summary>
        /// 为了下一笔购买做好判断的.
        /// </summary>
        /// <param name="accountId"></param>
        /// <param name="userName"></param>
        /// <param name="coin"></param>
        /// <returns></returns>
        public decimal GetMinPriceOfNotSellFinished(string accountId, string userName, string coin)
        {
            //var sql = $"select case when min(BuyTradePrice) is null then 25000 else min(BuyTradePrice) END from t_dog_more_buy where AccountId='{accountId}' and SymbolName = '{coin}' " +
            //    $" and BuyState!='({StateConst.Canceled.ToString()})' and IsFinished=0 and UserName='{userName}' ";
            //return Database.Query<decimal>(sql).FirstOrDefault();

            var sql = $"select * from t_dog_more_buy where AccountId='{accountId}' and SymbolName = '{coin}' and BuyState!='({StateConst.Canceled.ToString()})' " +
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

        public DogMoreBuy GetByBuyOrderId(long buyOrderId)
        {
            var sql = $"select * from t_dog_more_buy where BuyOrderId={buyOrderId}";
            return Database.Query<DogMoreBuy>(sql).FirstOrDefault();
        }

        public List<DogMoreBuy> listMoreBuyIsNotFinished(string symbolName)
        {
            var sql = $"select * from t_dog_more_buy where IsFinished=0 and SymbolName=@symbolName order by BuyTradePrice asc";
            return Database.Query<DogMoreBuy>(sql, new { symbolName }).ToList();
        }

        public List<DogMoreBuy> listErvryMinPriceMoreBuyIsNotFinished()
        {
            var sql = $"select * from t_dog_more_buy where BuyOrderId in( select BuyOrderId from  ( select max(BuyOrderId) BuyOrderId,SymbolName from t_dog_more_buy where IsFinished=0 group by SymbolName) t)  ";
            return Database.Query<DogMoreBuy>(sql).ToList();
        }

        public List<DogMoreBuy> listMoreBuyIsNotFinished(string symbolName, string userName)
        {
            var sql = $"select * from t_dog_more_buy where IsFinished=0 and SymbolName=@symbolName and UserName=@userName order by BuyTradePrice asc";
            return Database.Query<DogMoreBuy>(sql, new { symbolName, userName }).ToList();
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
            if (dogMoreBuy.BuyState != StateConst.Canceled && dogMoreBuy.BuyState != StateConst.Filled && dogMoreBuy.BuyState != StateConst.PartialFilled)
            {
                throw new ApplicationException("未取消或者未完成的订单，不能删除");
            }

            var sql = $"delete from t_dog_more_sell where BuyOrderId={buyOrderId}";
            Database.Execute(sql);
            sql = $"delete from t_dog_more_buy where BuyOrderId={buyOrderId}";
            Database.Execute(sql);
        }
    }
}
