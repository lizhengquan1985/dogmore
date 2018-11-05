using DogPlatform.Model;
using MySql.Data.MySqlClient;
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

        public void CheckTableExistsAndCreate(string quoteCurrency, string baseCurrency)
        {
            try
            {
                var createTableSql = $"CREATE TABLE IF NOT EXISTS `t_{quoteCurrency}_{baseCurrency}` ( `RecordId` bigint(20) NOT NULL AUTO_INCREMENT,  " +
                    $" `Id` bigint(20) NOT NULL, " +
                    $" `Open` decimal(18, 10) NOT NULL, " +
                    $" `Close` decimal(18, 10) NOT NULL, " +
                    $" `Low` decimal(18, 10) NOT NULL, " +
                    $" `High` decimal(18, 10) NOT NULL, " +
                    $" `Vol` decimal(18, 10) NOT NULL, " +
                    $" `Count` decimal(18, 10) NOT NULL, " +
                    $" `CreateTime` datetime NOT NULL, " +
                    $" PRIMARY KEY(`RecordId`))" +
                    $" ENGINE = InnoDB DEFAULT CHARSET = utf8mb4; ";
                Database.Execute(createTableSql);
            }
            catch (Exception ex)
            {

            }
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

        public List<HistoryKline> ListTodayKline(string symbolName, string quoteCurrency, DateTime begin, DateTime end)
        {
            var sql = $"select * from t_{quoteCurrency}_{symbolName} where CreateTime>=@Begin and CreateTime<=@End";
            return Database.Query<HistoryKline>(sql, new { Begin = begin, End = end }).ToList(); ;
        }

        public List<HistoryKline> List24HourKline(string quoteCurrency, string baseCurrency)
        {
            var date = DateTime.Now.AddDays(-1);
            var sql = $"select * from t_{quoteCurrency}_{baseCurrency} where CreateTime>=@date order by Id desc";
            return Database.Query<HistoryKline>(sql, new { date }).ToList();
        }

        public decimal? GetMaxClosePrice(string quoteCurrency, string baseCurrency)
        {
            var date = DateTime.Now.AddMinutes(-60);
            var sql = $"select max(Close) from t_{quoteCurrency}_{baseCurrency} where Id>=@id";
            return Database.Query<decimal?>(sql, new { id = Utils.GetIdByDate(date) }).FirstOrDefault();
        }

        public List<HistoryKline> List20Kline(string quoteCurrency, string baseCurrency)
        {
            var date = DateTime.Now.AddMinutes(-60);
            var sql = $"select * from t_{quoteCurrency}_{baseCurrency} where CreateTime>=@date order by Id desc limit 0,20";
            return Database.Query<HistoryKline>(sql, new { date }).ToList();
        }

        public List<HistoryKline> ListTodayKline(string quoteCurrency, string baseCurrency)
        {
            var sql = $"select * from t_{quoteCurrency}_{baseCurrency} where Id>@Id order by Id desc";
            return Database.Query<HistoryKline>(sql, new { Id = Utils.GetIdByDate(DateTime.Now.Date) }).ToList();
        }

        public List<HistoryKline> List30MinutesKline(string quoteCurrency, string baseCurrency)
        {
            var sql = $"select * from t_{quoteCurrency}_{baseCurrency} where Id>@Id order by Id desc";
            return Database.Query<HistoryKline>(sql, new { Id = Utils.GetIdByDate(DateTime.Now.AddMinutes(-30)) }).ToList();
        }

        public void DeleteAndRecordKlines(string symbolName, HistoryKline line)
        {
            long id = line.Id;
            using (var tx = Database.BeginTransaction())
            {
                var sql = $"delete from t_coin_{symbolName} where id={id}";
                Database.Execute(sql);

                sql = $"insert into t_coin_{symbolName}(Id, Open, Close, Low, High, Vol, Count, CreateTime) values({line.Id},{line.Open},{line.Close},{line.Low},{line.High},{line.Vol},{line.Count}, now())";
                Database.Execute(sql);

                tx.Commit();
            }
        }

        public void DeleteAndRecordKlines(string quoteCurrency, string baseCurrency, HistoryKline line)
        {
            long id = line.Id;
            using (var tx = Database.BeginTransaction())
            {
                var sql = $"delete from t_{quoteCurrency}_{baseCurrency} where id={id}";
                Database.Execute(sql);

                sql = $"insert into t_{quoteCurrency}_{baseCurrency}(Id, Open, Close, Low, High, Vol, Count, CreateTime) " +
                    $"values({line.Id},{line.Open},{line.Close},{line.Low},{line.High},{line.Vol},{line.Count}, now())";
                Database.Execute(sql);

                tx.Commit();
            }
        }
    }
}
