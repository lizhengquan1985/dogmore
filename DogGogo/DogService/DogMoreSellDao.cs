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
    public class DogMoreSellDao : BaseDao
    {
        static ILog logger = LogManager.GetLogger(typeof(DogMoreSellDao));

        public DogMoreSellDao() : base()
        {
        }

        public void CreateDogMoreBuy(DogMoreSell dogMoreSell)
        {
            using (var tx = Database.BeginTransaction())
            {
                Database.Insert(dogMoreSell);
                tx.Commit();
            }
        }

        #region 先查找出需要查询购买或者出售结果的记录， 然后查询结果，最后修改数据库记录

        /// <summary>
        /// 列出需要改变出售状态的
        /// </summary>
        /// <returns></returns>
        public List<DogMoreSell> ListNeedChangeSellStateDogMoreSell()
        {
            var states = $"'{StateConst.PartialFilled}','{StateConst.Filled}'";
            var sql = $"select * from t_pig_more_sell where SellState not in({states})";
            return Database.Query<DogMoreSell>(sql).ToList();
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
    }
}
