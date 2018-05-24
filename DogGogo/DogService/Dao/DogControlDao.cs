using DogService.DateTypes;
using SharpDapper.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DogService.Dao
{
    public class DogControlDao : BaseDao
    {
        public async Task<DogControl> GetDogControl(string symbolName)
        {
            return await Database.GetAsync<DogControl>(new { SymbolName = symbolName, IsValid = true });
        }

        public async Task CreateDogControl(DogControl dogControl)
        {
            using (var tx = Database.BeginTransaction())
            {
                await Database.UpdateAsync<DogControl>(new { IsValid = false }, new { dogControl.SymbolName });
                dogControl.IsValid = true;
                dogControl.CreateTime = DateTime.Now;
                await Database.InsertAsync(dogControl);
                tx.Commit();
            }
        }

        public async Task<List<DogControl>> ListDogControl()
        {
            //var sql = $"select * from t_dog_control where IsValid=1";
            return (await Database.QueryAsync<DogControl>(new { IsValid = 1})).ToList();
        }
    }
}
