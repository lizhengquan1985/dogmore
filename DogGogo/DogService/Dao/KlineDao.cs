using DogPlatform.Model;
using SharpDapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DogService.Dao
{
    public class KlineDao : BaseDao
    {
        public KlineDao() : base()
        {
        }
        public void CheckTable(string coin)
        {
            try
            {
                var createTableSql = $"CREATE TABLE `t_coin_{coin}` ( `RecordId` bigint(20) NOT NULL AUTO_INCREMENT,  " +
                    $" `Id` bigint(20) NOT NULL, " +
                    $" `Open` decimal(14, 6) NOT NULL, " +
                    $" `Close` decimal(14, 6) NOT NULL, " +
                    $" `Low` decimal(14, 6) NOT NULL, " +
                    $" `High` decimal(14, 6) NOT NULL, " +
                    $" `Vol` decimal(14, 6) NOT NULL, " +
                    $" `Count` decimal(14, 6) NOT NULL, " +
                    $" `CreateTime` datetime NOT NULL, " +
                    $" PRIMARY KEY(`RecordId`))" +
                    $" ENGINE = InnoDB DEFAULT CHARSET = utf8mb4; ";
                Database.Execute(createTableSql);
            }
            catch (Exception ex)
            {

            }
        }

        public void Record(string name, HistoryKline line)
        {
            try
            {
                var sql = $"insert into t_coin_{name}(Id, Open, Close, Low, High, Vol, Count, CreateTime) values({line.Id},{line.Open},{line.Close},{line.Low},{line.High},{line.Vol},{line.Count}, now())";
                Database.Execute(sql);
            }
            catch (Exception ex)
            {

            }
        }

        public List<HistoryKline> ListKline(string name, DateTime begin, DateTime end)
        {
            var sql = $"select * from t_coin_{name} where CreateTime>=@Begin and CreateTime<=@End";
            return Database.Query<HistoryKline>(sql, new { Begin = begin, End = end }).ToList(); ;
        }

        public List<HistoryKline> List24HourKline(string symbolName)
        {
            var date = DateTime.Now.AddDays(-1);
            var sql = $"select * from t_coin_{symbolName} where CreateTime>=@date order by Id desc";
            return Database.Query<HistoryKline>(sql, new { date }).ToList();
        }
    }
}
