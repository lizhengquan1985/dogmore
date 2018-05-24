using DogService.DateTypes;
using log4net;
using SharpDapper.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DogService
{
    public class DogEmptyBuyDao:BaseDao
    {
        static ILog logger = LogManager.GetLogger(typeof(DogEmptyBuyDao));

        public DogEmptyBuyDao() : base()
        {
        }

        public void CreateDogEmptyBuy(DogEmptyBuy dogEmptyBuy)
        {
            using (var tx = Database.BeginTransaction())
            {
                Database.Insert(dogEmptyBuy);
                tx.Commit();
            }
        }
    }
}
