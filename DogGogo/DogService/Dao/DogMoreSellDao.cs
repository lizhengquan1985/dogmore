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
using System.Threading;
using System.Threading.Tasks;

namespace DogService.Dao
{
    public class DogMoreSellDao : BaseDao
    {
        static ILog logger = LogManager.GetLogger(typeof(DogMoreSellDao));

        public DogMoreSellDao() : base()
        {
        }

        public List<DogMoreSell> ListDogMoreSellByBuyOrderId(long buyOrderId)
        {
            var sql = $"select * from t_dog_more_sell where BuyOrderId={buyOrderId}";
            return Database.Query<DogMoreSell>(sql).ToList();
        }

        public void CreateDogMoreSell(DogMoreSell dogMoreSell)
        {
            try
            {
                using (var tx = Database.BeginTransaction())
                {
                    Database.Insert(dogMoreSell);
                    tx.Commit();
                }
            }
            catch (Exception ex)
            {
                logger.Error($"--------危险----- CreateDogMoreSell-----------{JsonConvert.SerializeObject(dogMoreSell)}");
                logger.Error(ex.Message, ex);
                Thread.Sleep(1000 * 60 * 60);
            }
        }

        public DogMoreSell GetDogMoreSellBySellOrderId(long sellOrderId)
        {
            return Database.Query<DogMoreSell>(new { SellOrderId = sellOrderId }).FirstOrDefault();
        }

        #region 先查找出需要查询购买或者出售结果的记录， 然后查询结果，最后修改数据库记录

        /// <summary>
        /// 列出需要改变出售状态的
        /// </summary>
        /// <returns></returns>
        public List<DogMoreSell> ListNeedChangeSellStateDogMoreSell()
        {
            var states = GetStateStringIn(new List<string>() { StateConst.PartialFilled, StateConst.Filled });
            var sql = $"select * from t_dog_more_sell where SellState not in({states})";
            return Database.Query<DogMoreSell>(sql).ToList();
        }

        public void UpdateDogMoreSellWhenSuccess(long sellOrderId, HBResponse<OrderDetail> orderDetail, HBResponse<List<OrderMatchResult>> orderMatchResult, decimal sellTradePrice)
        {
            var dogMoreSell = GetDogMoreSellBySellOrderId(sellOrderId);
            var results = JsonConvert.SerializeObject(orderMatchResult);
            if (results.Length > 8000)
            {
                logger.Error($"{results}");
                results = results.Substring(0, 8000);
            }
            using (var tx = Database.BeginTransaction())
            {
                var sqlBuy = $"update t_dog_more_buy set IsFinished=1 where BuyOrderId={dogMoreSell.BuyOrderId}";
                Database.Execute(sqlBuy);

                var sqlSell = $"update t_dog_more_sell set SellTradePrice={sellTradePrice}, SellState='{orderDetail.Data.state}' ,SellOrderDetail='{JsonConvert.SerializeObject(orderDetail)}'," +
                    $" SellOrderMatchResults='{JsonConvert.SerializeObject(results)}' where SellOrderId ='{sellOrderId}'";
                Database.Execute(sqlSell);
                tx.Commit();
            }
        }

        public void UpdateDogMoreSellWhenCancel(long sellOrderId)
        {
            using (var tx = Database.BeginTransaction())
            {
                var sqlSell = $"update t_dog_more_sell set SellState='{StateConst.Canceled}' where SellOrderId ='{sellOrderId}'";
                Database.Execute(sqlSell);
                tx.Commit();
            }
        }

        #endregion

        public List<long> listDogMoreSell(string userName, string symbolName, int pageIndex, int pageSize)
        {
            var where = $" where SymbolName like @symbolName ";
            if (!string.IsNullOrEmpty(userName))
            {
                where += $" and UserName=@userName";
            }
            var sql = $"select BuyOrderId from (select BuyOrderId,max(Id) Id from t_dog_more_sell {where} group by BuyOrderId order by Id Desc limit {pageIndex * pageSize},{pageSize}) t";
            return Database.Query<long>(sql, new { symbolName = LikeStr(symbolName), userName }).ToList();
        }
    }
}
