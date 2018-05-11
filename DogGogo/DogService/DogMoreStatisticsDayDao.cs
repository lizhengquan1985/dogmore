using DogService.DateTypes;
using log4net;
using SharpDapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DogService
{
    public class DogMoreStatisticsDayDao : BaseDao
    {
        static ILog logger = LogManager.GetLogger(typeof(DogMoreStatisticsDayDao));

        public DogMoreStatisticsDayDao() : base()
        {
        }

        public async Task<List<DogMoreStatisticsDay>> ListStatisticsData(string userName)
        {
            var sql = $"select * from t_dog_more_statistics_day where UserName='{userName}' order by RecordDate desc";
            return (await Database.QueryAsync<DogMoreStatisticsDay>(sql)).ToList();
        }
    }
}
