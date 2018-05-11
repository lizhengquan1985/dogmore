using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DogAccount
{
    public class AccountConfig
    {
        public string UserName { get; set; }
        public string AccessKey { get; set; }
        public string SecretKey { get; set; }
        public string MainAccountId { get; set; }
    }

    public class AccountConfigUtils
    {
        public static string sqlConfig = "server=localhost;port=3306;user id=root; password=lyx123456; database=studyplan; pooling=true; charset=utf8mb4";

        public static AccountConfig GetAccountConfig(string userName)
        {
            AccountConfig accountConfig = new AccountConfig();
            accountConfig.UserName = userName;

            if (userName == "qq")
            {
                accountConfig.MainAccountId = "";
                accountConfig.AccessKey = "";
                accountConfig.SecretKey = "";
            }
            else if (userName == "xx")
            {
                accountConfig.MainAccountId = "";
                accountConfig.AccessKey = "";
                accountConfig.SecretKey = "";
            }

            return accountConfig;
        }
    }
}
