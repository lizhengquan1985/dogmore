using DogService.DateTypes;
using Newtonsoft.Json;
using SharpDapper;
using SharpDapper.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DogService.Dao
{
    public class DogOrderStatDao : BaseDao
    {
        public void AddStatRecord()
        {
            try
            {
                var sql = "select QuoteCurrency, count(1) as Count from t_dog_more_buy group by QuoteCurrency";
                var res = (Database.Query<ItemResult>(sql)).ToList();

                var delSql = $"delete from t_dog_order_stat where Date='{DateTime.Now.ToString("yyyy-MM-dd HH")}'";
                Database.Execute(delSql);

                DogOrderStat item = new DogOrderStat()
                {
                    DataCount = JsonConvert.SerializeObject(res),
                    Date = DateTime.Now.ToString("yyyy-MM-dd HH")
                };
                Database.Insert(item);
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
            }
        }
    }

    public class ItemResult
    {
        public string QuoteCurrency { get; set; }
        public int Count { get; set; }
    }
}
