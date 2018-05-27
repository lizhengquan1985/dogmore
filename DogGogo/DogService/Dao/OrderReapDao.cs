﻿using DogService.DateTypes;
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
            if(Database.Query<OrderReap>(sql) != null)
            {

            }
            else
            {
                Database.Insert(orderReap);
            }
        }

        public List<OrderReap> List(ReapType reapType, bool isMore)
        {
            var sql = $"select * from t_order_reap where IsMore={isMore} and reapType={(int)reapType}";
            return Database.Query<OrderReap>(sql).ToList();
        }
    }
}
