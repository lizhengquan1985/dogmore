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
    public class DogMoreBuyDao : BaseDao
    {
        static ILog logger = LogManager.GetLogger(typeof(DogMoreBuyDao));

        public DogMoreBuyDao() : base()
        {
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
            var sql = $"select * from t_pig_more_buy where BuyState not in({states})";
            return Database.Query<DogMoreBuy>(sql).ToList();
        }

        public void UpdatePigMoreBuySuccess(long buyOrderId, HBResponse<OrderDetail> orderDetail, HBResponse<List<OrderMatchResult>> orderMatchResult, decimal buyTradePrice)
        {
            using (var tx = Database.BeginTransaction())
            {
                var sql = $"update t_pig_more set BuyTradePrice={buyTradePrice}, BuyState='{orderDetail.Data.state}' ,BuyOrderDetail='{JsonConvert.SerializeObject(orderDetail)}', BuyOrderMatchResults='{JsonConvert.SerializeObject(orderMatchResult)}' where BuyOrderId ='{buyOrderId}'";
                Database.Execute(sql);
                tx.Commit();
            }
        }

        #endregion

        public List<DogMoreBuy> GetNeedSellDogMoreBuy(string accountId, string userName, string symbolName)
        {
            List<string> stateList = new List<string>() { StateConst.PartialCanceled, StateConst.Filled };
            var states = "";
            stateList.ForEach(it =>
            {
                if (states != "")
                {
                    states += ",";
                }
                states += $"'{it}'";
            });
            var sql = $"select * from t_dog_more_buy where AccountId='{accountId}' and SymbolName = '{symbolName}' and BState in({states}) and (SOrderId<=0 or SOrderId is null) and UserName='{userName}' order by BOrderP asc limit 0,5";
            return Database.Query<DogMoreBuy>(sql).ToList();
        }

        public decimal GetMinPriceOfNotSell(string accountId, string userName, string coin)
        {
            var sql = $"select case when min(BTradeP) is null then 99999 else min(BTRADEP) END from t_pig_more where AccountId='{accountId}' and Name = '{coin}' and BState!='({StateConst.Canceled.ToString()})' and (SOrderId<=0 or SOrderId is null) and UserName='{userName}'";
            return Database.Query<decimal>(sql).FirstOrDefault();
        }

        public DogMoreBuy GetByBuyOrderId(long buyOrderId)
        {
            var sql = $"select * from t_dog_more_buy where BuyOrderId={buyOrderId}";
            return Database.Query<DogMoreBuy>(sql).FirstOrDefault();
        }
    }
}
