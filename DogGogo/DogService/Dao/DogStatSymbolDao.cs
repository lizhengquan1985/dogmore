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
    public class DogStatSymbolDao : BaseDao
    {
        public void CreateDogStatSymbol(DogStatSymbol dogStatSymbol)
        {
            using (var tx = Database.BeginTransaction())
            {
                var delSql = $"delete from t_dog_stat_symbol where SymbolName=@symbolName and StatDate=@StatDate";
                Database.Execute(delSql, new { symbolName = dogStatSymbol.SymbolName, StatDate = dogStatSymbol.StatDate });

                Database.Insert(dogStatSymbol);
                tx.Commit();
            }
        }

        public List<DogStatSymbol> ListDogStatSymbol(List<string> dateList)
        {
            var sql = $"select * from t_dog_stat_symbol where StatDate in ({GetStateStringIn(dateList)})";
            return (Database.Query<DogStatSymbol>(sql)).ToList();
        }
    }
}
