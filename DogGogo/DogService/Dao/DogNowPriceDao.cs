using DogService.DateTypes;
using SharpDapper;
using SharpDapper.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DogService.Dao
{
    public class DogNowPriceDao : BaseDao
    {
        public void CreateDogNowPrice(DogNowPrice dogNowPrice)
        {
            using (var tx = Database.BeginTransaction())
            {
                var delSql = $"delete from t_dog_now_price where SymbolName=@symbolName and QuoteCurrency=@quoteCurrency";
                Database.Execute(delSql, new { quoteCurrency = dogNowPrice.QuoteCurrency, symbolName = dogNowPrice.SymbolName });

                Database.Insert(dogNowPrice);
                tx.Commit();
            }
        }

        public List<DogNowPrice> ListDogNowPrice()
        {
            var sql = $"select * from t_dog_now_price";
            return (Database.Query<DogNowPrice>(sql)).ToList();
        }
    }
}
