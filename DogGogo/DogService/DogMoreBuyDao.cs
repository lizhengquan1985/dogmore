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

        public void CreatePigMore(PigMore pigMore)
        {
            using (var tx = Database.BeginTransaction())
            {
                Database.Insert(pigMore);
                tx.Commit();
            }
        }

        #region 先查找出需要查询购买或者出售结果的记录， 然后查询结果，最后修改数据库记录

        /// <summary>
        /// 列出需要改变购买状态的记录
        /// </summary>
        /// <param name="accountId"></param>
        /// <returns></returns>
        public List<PigMore> ListNeedChangeBuyStatePigMore()
        {
            var states = $"'{StateConst.PartialFilled}','{StateConst.Filled}'";
            var sql = $"select * from t_pig_more where BState not in({states})";
            return Database.Query<PigMore>(sql).ToList();
        }

        /// <summary>
        /// 列出需要改变出售状态的
        /// </summary>
        /// <returns></returns>
        public List<PigMore> ListNeedChangeSellStatePigMore()
        {
            var states = $"'{StateConst.PartialFilled}','{StateConst.Filled}'";
            var sql = $"select * from t_pig_more where SState not in({states}) and SOrderId>0";
            return Database.Query<PigMore>(sql).ToList();
        }

        public void UpdatePigMoreBuySuccess(long buyOrderId, HBResponse<OrderDetail> orderDetail, HBResponse<List<OrderMatchResult>> orderMatchResult, decimal buyTradePrice)
        {
            using (var tx = Database.BeginTransaction())
            {
                var sql = $"update t_pig_more set BTradeP={buyTradePrice}, BState='{orderDetail.Data.state}' ,BOrderDetail='{JsonConvert.SerializeObject(orderDetail)}', BOrderMatchResults='{JsonConvert.SerializeObject(orderMatchResult)}' where BOrderId ='{buyOrderId}'";
                Database.Execute(sql);
                tx.Commit();
            }
        }

        public void UpdateTradeRecordSellSuccess(long sellOrderId, HBResponse<OrderDetail> orderDetail, HBResponse<List<OrderMatchResult>> orderMatchResult, decimal sellTradePrice)
        {
            using (var tx = Database.BeginTransaction())
            {
                var sql = $"update t_pig_more set STradeP={sellTradePrice}, SState='{orderDetail.Data.state}' ,SOrderDetail='{JsonConvert.SerializeObject(orderDetail)}', SOrderMatchResults='{JsonConvert.SerializeObject(orderMatchResult)}' where SOrderId ='{sellOrderId}'";
                Database.Execute(sql);
                tx.Commit();
            }
        }

        #endregion

        public List<PigMore> GetNeedSellPigMore(string accountId, string userName, string coin)
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
            var sql = $"select * from t_pig_more where AccountId='{accountId}' and Name = '{coin}' and BState in({states}) and (SOrderId<=0 or SOrderId is null) and UserName='{userName}' order by BOrderP asc limit 0,5";
            return Database.Query<PigMore>(sql).ToList();
        }

        public decimal GetMinPriceOfNotSell(string accountId, string userName, string coin)
        {
            var sql = $"select case when min(BTradeP) is null then 99999 else min(BTRADEP) END from t_pig_more where AccountId='{accountId}' and Name = '{coin}' and BState!='({StateConst.Canceled.ToString()})' and (SOrderId<=0 or SOrderId is null) and UserName='{userName}'";
            return Database.Query<decimal>(sql).FirstOrDefault();
        }

        public void ChangeDataWhenSell(long id, decimal sellQuantity, decimal sellOrderPrice, string sellOrderResult, string sFlex, long sellOrderId)
        {
            using (var tx = Database.BeginTransaction())
            {
                var sql = $"update t_pig_more set SQuantity={sellQuantity}, SOrderP={sellOrderPrice}, SDate=now(), SFlex='{sFlex}', SOrderResult='{sellOrderResult}',SOrderId={sellOrderId} where Id = {id}";
                Database.Execute(sql);
                tx.Commit();
            }
        }

        public PigMore GetByBOrderId(long orderId)
        {
            var sql = $"select * from t_pig_more where BOrderId={orderId}";
            return Database.Query<PigMore>(sql).FirstOrDefault();
        }
    }
}
