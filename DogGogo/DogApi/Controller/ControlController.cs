using DogService.Dao;
using DogService.DateTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace DogApi.Controller
{
    public class ControlController : ApiController
    {
        public async Task Create([FromBody] DogControl dogControl)
        {
            await new DogControlDao().CreateDogControl(dogControl);
        }

        [HttpGet]
        [ActionName("list")]
        public async Task<object> List()
        {
            return await new DogControlDao().ListDogControl();
        }

    }
}
