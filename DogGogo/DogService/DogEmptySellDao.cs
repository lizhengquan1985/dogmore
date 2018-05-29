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

namespace DogService
{
    public class DogEmptySellDao : BaseDao
    {
        static ILog logger = LogManager.GetLogger(typeof(DogEmptySellDao));

        public DogEmptySellDao() : base()
        {
        }

        public DogEmptySell GetDogEmptySellBySellOrderId(long orderId)
        {
            var sql = $"select * from t_dog_empty_sell where SellOrderId={orderId}";
            return Database.Query<DogEmptySell>(sql).FirstOrDefault();
        }

        public List<DogEmptySell> GetNeedBuyDogEmptySell(string accountId, string userName, string symbolName)
        {
            var states = GetStateStringIn(new List<string>() { StateConst.PartialCanceled, StateConst.Filled });
            var states2 = GetStateStringIn(new List<string>() { StateConst.PartialCanceled, StateConst.Filled, StateConst.Canceled });
            var sql = $"select * from t_dog_empty_sell where AccountId='{accountId}' and SymbolName = '{symbolName}' and SellState in({states}) and IsFinished=0 " +
                $" and UserName='{userName}' and SellOrderId not in(select SellOrderId from t_dog_empty_buy where AccountId='{accountId}' and UserName='{userName}' and BuyState not in({states})) " +
                $" order by SellOrderPrice desc limit 0,5";
            return Database.Query<DogEmptySell>(sql).ToList();
        }

        public void CreateDogEmptySell(DogEmptySell dogEmptySell)
        {
            using (var tx = Database.BeginTransaction())
            {
                Database.Insert(dogEmptySell);
                tx.Commit();
            }
        }

        /// <summary>
        /// 列出需要改变出售状态的
        /// </summary>
        /// <returns></returns>
        public List<DogEmptySell> ListNeedChangeSellStateDogEmptySell()
        {
            var states = GetStateStringIn(new List<string>() { StateConst.PartialFilled, StateConst.Filled });
            var sql = $"select * from t_dog_empty_sell where SellState not in({states})";
            return Database.Query<DogEmptySell>(sql).ToList();
        }

        public void UpdateDogEmptySellWhenSuccess(long sellOrderId, HBResponse<OrderDetail> orderDetail, HBResponse<List<OrderMatchResult>> orderMatchResult, decimal sellTradePrice)
        {
            using (var tx = Database.BeginTransaction())
            {
                var sqlSell = $"update t_dog_empty_sell set SellTradePrice={sellTradePrice}, SellState='{orderDetail.Data.state}' ,SellOrderDetail='{JsonConvert.SerializeObject(orderDetail)}'," +
                    $" SellOrderMatchResults='{JsonConvert.SerializeObject(orderMatchResult)}' where SellOrderId ='{sellOrderId}'";
                Database.Execute(sqlSell);
                tx.Commit();
            }
        }

        /// <summary>
        /// 列出需要改变出售状态的
        /// </summary>
        /// <returns></returns>
        public List<DogEmptySell> ListDogEmptySellNotFinished(string symbolName)
        {
            var sql = $"select * from t_dog_empty_sell where IsFinished=0 and SymbolName=@symbolName order by SellTradePrice desc";
            return Database.Query<DogEmptySell>(sql, new { symbolName }).ToList();
        }
        public void Delete(long sellOrderId)
        {
            var dogMoreSell = GetDogEmptySellBySellOrderId(sellOrderId);
            if (dogMoreSell.SellState != StateConst.Canceled && dogMoreSell.SellState != StateConst.Filled && dogMoreSell.SellState != StateConst.PartialFilled)
            {
                throw new ApplicationException("未取消或者未完成的订单，不能删除");
            }

            var sql = $"delete from t_dog_empty_buy where SellOrderId={sellOrderId}";
            Database.Execute(sql);
            sql = $"delete from t_dog_empty_sell where SellOrderId={sellOrderId}";
            Database.Execute(sql);
        }
    }
}
