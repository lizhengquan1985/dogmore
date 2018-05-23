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
                await Database.UpdateAsync<ControlAvgInput>(new { IsValid = false }, new { dogControl.SymbolName });
                await Database.InsertAsync(dogControl);
                tx.Commit();
            }
        }
    }
}
