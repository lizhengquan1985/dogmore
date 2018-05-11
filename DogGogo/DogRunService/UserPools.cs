using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DogRunService
{
    public class UserPools
    {
        private static HashSet<string> userNames = new HashSet<string>();

        public static void Push(string userName)
        {
            if (!userNames.Contains(userName))
            {
                userNames.Add(userName);
            }
        }

        public static HashSet<string> GetAllUserName()
        {
            return userNames;
        }
    }
}
