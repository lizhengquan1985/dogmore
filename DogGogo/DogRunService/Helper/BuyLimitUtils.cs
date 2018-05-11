using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DogRunService.Helper
{
    public class BuyLimitUtils
    {
        private static List<BuyRecord> recordList = new List<BuyRecord>();

        public static bool Record(string userName, string name)
        {
            recordList.Add(new BuyRecord() { UserName = userName, Name = name, BuyDate = DateTime.Now });

            var count = recordList.Count(it => it.Name == name && it.UserName == userName && it.BuyDate > DateTime.Now.AddHours(2));
            return count > 6;
        }
    }

    public class BuyRecord
    {
        public string UserName { get; set; }
        public string Name { get; set; }
        public DateTime BuyDate { get; set; }
    }
}
