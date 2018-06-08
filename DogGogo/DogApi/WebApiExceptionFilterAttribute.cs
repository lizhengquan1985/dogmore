using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http.Filters;

namespace DogApi
{
    public class WebApiExceptionFilterAttribute : ExceptionFilterAttribute
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(WebApiExceptionFilterAttribute));
        public override void OnException(HttpActionExecutedContext actionExecutedContext)
        {
            if (!(actionExecutedContext.Exception is ApplicationException))
            {
                logger.Error(actionExecutedContext.Exception.GetType() + "：" + actionExecutedContext.Exception.Message + "——堆栈信息：" +
                             actionExecutedContext.Exception.StackTrace);
            }

            logger.Error(actionExecutedContext.Exception.Message + actionExecutedContext.Exception.StackTrace);

            actionExecutedContext.Response = new HttpResponseMessage(HttpStatusCode.InternalServerError);

            if (actionExecutedContext.Exception is ApplicationException)
            {
                actionExecutedContext.Response.Content = new StringContent(actionExecutedContext.Exception.Message, Encoding.UTF8);
            }
            else
            {
                actionExecutedContext.Response.Content = new StringContent("服务器内部错误", Encoding.UTF8);
            }
        }
    }
}
