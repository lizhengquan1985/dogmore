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

        /// <summary>
        /// 获取某个用户未收割得空单总额度
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        public decimal GetSumNotShougeDogEmptySell(string userName, string quoteCurrency)
        {
            decimal sum = 0;
            var sql = $"select sum(SellQuantity*SellTradePrice) Total from t_dog_empty_sell where IsFinished=0 and UserName=@userName and QuoteCurrency='{quoteCurrency}'";
            var res = Database.Query<decimal?>(sql, new { userName }).FirstOrDefault();
            if (res != null)
            {
                sum += (decimal)res;
            }

            sql = $"select sum(BuyQuantity) Total from t_dog_more_buy where IsFinished=0 and UserName=@userName and SymbolName = '{quoteCurrency}'";
            res = Database.Query<decimal?>(sql, new { userName }).FirstOrDefault();
            if (res != null)
            {
                sum += (decimal)res;
            }
            return sum;
        }

        public List<DogEmptySell> GetNeedShougeDogEmptySell(string userName, string symbolName, string quoteCurrency)
        {
            var states = GetStateStringIn(new List<string>() { StateConst.PartialCanceled, StateConst.Filled });
            var states2 = GetStateStringIn(new List<string>() { StateConst.Canceled });
            var sql = $"select * from t_dog_empty_sell where UserName='{userName}' and SymbolName = '{symbolName}' and QuoteCurrency='{quoteCurrency}' and SellState in({states}) and IsFinished=0 " +
                $" and SellOrderId not in(select SellOrderId from t_dog_empty_buy where UserName='{userName}' and SymbolName = '{symbolName}' and QuoteCurrency='{quoteCurrency}' and BuyState not in({states})) " +
                $" order by SellOrderPrice desc limit 0,5";
            return Database.Query<DogEmptySell>(sql).ToList();
        }

        public DogEmptySell GetBiggestDogEmptySell(string quoteCurrency, string baseCurrency)
        {
            var states = GetStateStringIn(new List<string>() { StateConst.PartialCanceled, StateConst.Filled });
            var states2 = GetStateStringIn(new List<string>() { StateConst.PartialCanceled, StateConst.Filled, StateConst.Canceled });
            var sql = $"select * from t_dog_empty_sell where QuoteCurrency='{quoteCurrency}' and SymbolName = '{baseCurrency}' and SellState in({states}) and IsFinished=0 " +
                $" and SellOrderId not in(select SellOrderId from t_dog_empty_buy where QuoteCurrency='{quoteCurrency}' and SymbolName='{baseCurrency}' and BuyState not in({states})) " +
                $" order by SellOrderPrice desc limit 0,1";
            return Database.Query<DogEmptySell>(sql).FirstOrDefault();
        }

        public void CreateDogEmptySell(DogEmptySell dogEmptySell)
        {
            try
            {
                using (var tx = Database.BeginTransaction())
                {
                    Database.Insert(dogEmptySell);
                    tx.Commit();
                }
            }
            catch (Exception ex)
            {
                logger.Error($"严重 --------危险----- CreateDogEmptySell----------{JsonConvert.SerializeObject(dogEmptySell)}");
                logger.Error(ex.Message, ex);
                Thread.Sleep(1000 * 60 * 60);
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
        public List<DogEmptySell> ListDogEmptySellNotFinished(string symbolName, string userName)
        {
            var sql = $"select * from t_dog_empty_sell where IsFinished=0 and SymbolName=@symbolName and UserName=@userName order by SellTradePrice desc";
            return Database.Query<DogEmptySell>(sql, new { symbolName, userName }).ToList();
        }

        public List<DogEmptySell> ListDogEmptySellNotFinished(string symbolName, string userName, string quoteCurrency)
        {
            var sql = $"select * from t_dog_empty_sell where IsFinished=0 and SymbolName=@symbolName and QuoteCurrency=@quoteCurrency and UserName=@userName order by SellTradePrice desc";
            return Database.Query<DogEmptySell>(sql, new { symbolName, userName, quoteCurrency }).ToList();
        }

        public decimal? GetMaxSellTradePrice(string userName, string symbolName, string quoteCurrency)
        {
            var sql = $"select max(SellTradePrice) MaxPrice from t_dog_empty_sell where IsFinished=0 and SymbolName=@symbolName and QuoteCurrency=@quoteCurrency and UserName=@userName";
            var res = Database.Query<decimal?>(sql, new { symbolName, userName, quoteCurrency }).FirstOrDefault();
            return res;
        }

        public DogEmptySell GetDogEmptySellOfLastNotFinished(string userName, string symbolName, string quoteCurrency)
        {
            var sql = $"select * from t_dog_empty_sell where IsFinished=0 and SymbolName=@symbolName and QuoteCurrency=@quoteCurrency and UserName=@userName order by SellOrderPrice desc";
            return Database.Query<DogEmptySell>(sql, new { symbolName, userName, quoteCurrency }).FirstOrDefault();
        }

        public List<DogEmptySell> ListDogEmptySellFinished(string userName, string symbolName)
        {
            var sql = $"select * from t_dog_empty_sell where IsFinished=1 and SymbolName like @symbolName ";
            if (!string.IsNullOrEmpty(userName))
            {
                sql += $" and UserName=@userName";
            }
            return Database.Query<DogEmptySell>(sql, new { symbolName = LikeStr(symbolName), userName }).ToList();
        }


        public void Delete(long sellOrderId)
        {
            var dogEmptySell = GetDogEmptySellBySellOrderId(sellOrderId);
            if (dogEmptySell.SellState != StateConst.Canceled && dogEmptySell.SellState != StateConst.Filled && dogEmptySell.SellState != StateConst.PartialFilled)
            {
                throw new ApplicationException("未取消或者未完成的订单，不能删除");
            }

            var sql = $"delete from t_dog_empty_buy where SellOrderId={sellOrderId}";
            Database.Execute(sql);
            sql = $"delete from t_dog_empty_sell where SellOrderId={sellOrderId}";
            Database.Execute(sql);
        }

        public List<DogEmptySell> listEveryMaxPriceEmptySellIsNotFinished(string userName, string quoteCurrency)
        {
            var sql = $"select * from t_dog_empty_sell where UserName=@UserName and SellOrderId in( select SellOrderId from  ( select max(SellOrderId) SellOrderId,SymbolName from t_dog_empty_sell where UserName=@UserName and QuoteCurrency=@quoteCurrency and IsFinished=0 group by SymbolName) t)  ";
            return Database.Query<DogEmptySell>(sql, new { UserName = userName, quoteCurrency }).ToList();
        }
    }
}
