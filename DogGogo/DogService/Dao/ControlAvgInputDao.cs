using DogService.DateTypes;
using SharpDapper.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DogService.Dao
{
    public class ControlAvgInputDao : BaseDao
    {
        public async Task CreateControlAvgInput(string symbolName, int inputAvgNum, DateTime expiredTime)
        {
            ControlAvgInput avgInput = new ControlAvgInput()
            {
                ExpiredTime = expiredTime,
                IsValid = true,
                SymbolName = symbolName,
                InputAvgNum = inputAvgNum
            };
            using (var tx = Database.BeginTransaction())
            {
                await Database.UpdateAsync<ControlAvgInput>(new { IsValid = false }, new { SymbolName = symbolName });
                await Database.InsertAsync(avgInput);
                tx.Commit();
            }
        }

        public async Task<ControlAvgInput> GetControlAvgInput(string symbolName)
        {
            return await Database.GetAsync<ControlAvgInput>(new { SymbolName = symbolName, IsValid = true });
        }
    }
}
