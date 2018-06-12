using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Alert
{
    public class OpenUrlUtils
    {
        private static DateTime dt = DateTime.Now;
        public static void Open()
        {
            if ((DateTime.Now - dt).TotalSeconds > 60)
            {
                System.Diagnostics.Process.Start("explorer.exe", "http://blog.csdn.net/testcs_dn");
                dt = DateTime.Now;
            }
        }
    }
}
