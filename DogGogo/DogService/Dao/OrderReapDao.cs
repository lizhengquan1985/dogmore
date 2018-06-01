using DogService.DateTypes;
using log4net;
using SharpDapper;
using SharpDapper.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DogService.Dao
{
    public class OrderReapDao : BaseDao
    {
        static ILog logger = LogManager.GetLogger(typeof(OrderReapDao));

        public OrderReapDao() : base()
        {
        }

        public void Create(OrderReap orderReap)
        {
            var sql = $"select * from t_order_reap where OrderId={orderReap.OrderId}";
            if(Database.Query<OrderReap>(sql).FirstOrDefault() != null)
            {
                logger.Error("---------------------------------");
                sql = $"update t_order_reap set ReapType={(int)orderReap.ReapType} where OrderId={orderReap.OrderId}";
                Database.Execute(sql);
            }
            else
            {
                using (var tx = Database.BeginTransaction())
                {
                    Database.Insert(orderReap);
                    tx.Commit();
                }
            }
        }

        public List<OrderReap> List(ReapType reapType)
        {
            var sql = $"select * from t_order_reap where IsFinished=0 and ReapType={(int)reapType}";
            return Database.Query<OrderReap>(sql).ToList();
        }
    }
}
