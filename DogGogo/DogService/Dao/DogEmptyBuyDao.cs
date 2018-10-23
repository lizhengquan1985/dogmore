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
    public class DogEmptyBuyDao : BaseDao
    {
        static ILog logger = LogManager.GetLogger(typeof(DogEmptyBuyDao));

        public DogEmptyBuyDao() : base()
        {
        }

        public void CreateDogEmptyBuy(DogEmptyBuy dogEmptyBuy)
        {
            try
            {
                using (var tx = Database.BeginTransaction())
                {
                    Database.Insert(dogEmptyBuy);
                    tx.Commit();
                }
            }
            catch (Exception ex)
            {
                logger.Error($"--------危险----- CreateDogEmptyBuy ------------ {JsonConvert.SerializeObject(dogEmptyBuy)}");
                logger.Error(ex.Message, ex);
                Thread.Sleep(1000 * 60 * 60);
            }
        }

        public List<DogEmptyBuy> ListDogEmptyBuyBySellOrderId(long sellOrderId)
        {
            var sql = $"select * from t_dog_empty_buy where SellOrderId={sellOrderId}";
            return Database.Query<DogEmptyBuy>(sql).ToList();
        }

        public List<DogEmptyBuy> ListNeedChangeBuyStateDogEmptyBuy()
        {
            var states = GetStateStringIn(new List<string>() { StateConst.PartialFilled, StateConst.Filled });
            var sql = $"select * from t_dog_empty_buy where BuyState not in({states})";
            return Database.Query<DogEmptyBuy>(sql).ToList();
        }

        public DogEmptyBuy GetDogEmptyBuyByBuyOrderId(long buyOrderId)
        {
            var sql = $"select * from t_dog_empty_buy where BuyOrderId={buyOrderId}";
            return Database.Query<DogEmptyBuy>(sql).FirstOrDefault();
        }

        public void UpdateDogEmptyBuyWhenSuccess(long buyOrderId, HBResponse<OrderDetail> orderDetail, HBResponse<List<OrderMatchResult>> orderMatchResult, decimal buyTradePrice)
        {
            var dogEmptyBuy = GetDogEmptyBuyByBuyOrderId(buyOrderId);

            using (var tx = Database.BeginTransaction())
            {
                var sqlBuy = $"update t_dog_empty_sell set IsFinished=1 where SellOrderId={dogEmptyBuy.SellOrderId}";
                Database.Execute(sqlBuy);

                var sqlSell = $"update t_dog_empty_buy set BuyTradePrice={buyTradePrice}, BuyState='{orderDetail.Data.state}' ,BuyOrderDetail='{JsonConvert.SerializeObject(orderDetail)}'," +
                    $" BuyOrderMatchResults='{JsonConvert.SerializeObject(orderMatchResult)}' where BuyOrderId ='{buyOrderId}'";
                Database.Execute(sqlSell);
                tx.Commit();
            }
        }

        public List<long> ListDogEmptyBuy(string userName, string symbolName, int pageIndex, int pageSize)
        {
            var where = $" where SymbolName like @symbolName ";
            if (!string.IsNullOrEmpty(userName))
            {
                where += $" and UserName=@userName";
            }
            var sql = $"select SellOrderId from (select SellOrderId,max(Id) Id from t_dog_empty_buy {where} group by SellOrderId order by Id Desc limit {pageIndex * pageSize},{pageSize}) t";
            return Database.Query<long>(sql, new { symbolName = LikeStr(symbolName), userName }).ToList();
        }
    }
}
