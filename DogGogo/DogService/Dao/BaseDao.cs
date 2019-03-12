using DogAccount;
using log4net;
using MySql.Data.MySqlClient;
using SharpDapper;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DogService.Dao
{
    public class BaseDao
    {
        protected ILog logger = LogManager.GetLogger(typeof(BaseDao));

        public string LikeStr(string str)
        {
            StringBuilder sb = new StringBuilder(str);
            sb.Replace("'", "''");
            sb.Insert(0, "%", 1);
            sb.Append("%");
            sb.Replace(@"\", @"\\");

            return sb.ToString();
        }

        protected IDapperConnection Database { get; private set; }
        public BaseDao()
        {
            string connectionString = AccountConfigUtils.sqlConfig;
            var connection = new MySqlConnection(connectionString);
            Database = new DapperConnection(connection);
        }

        public string GetStateStringIn(List<string> stateList)
        {
           // List<string> stateList = new List<string>() { StateConst.PartialCanceled, StateConst.Filled };
            var states = "";
            stateList.ForEach(it =>
            {
                if (states != "")
                {
                    states += ",";
                }
                states += $"'{it}'";
            });
            return states;
        }
    }
}
