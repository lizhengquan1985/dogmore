using DogService.Dao;
using DogService.DateTypes;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace DogApi.Controller
{
    public class OrderReapController : ApiController
    {
        static ILog logger = LogManager.GetLogger(typeof(OrderReapController));

        [HttpPost]
        [ActionName("new")]
        public async Task Create([FromBody] OrderReap orderReap)
        {
            try
            {
                new OrderReapDao().Create(orderReap);

            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
            }
        }
    }
}
