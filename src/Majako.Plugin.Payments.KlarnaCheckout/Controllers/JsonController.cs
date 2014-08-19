using System;
using System.Net;
using System.Web.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Nop.Web.Controllers;
using Nop.Web.Framework.Controllers;

namespace Majako.Plugin.Payments.KlarnaCheckout.Controllers
{
    public class JsonController : BasePluginController
    {
        protected new ActionResult Json(object data, JsonRequestBehavior behavior = JsonRequestBehavior.DenyGet)
        {
            var jsonSerializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };

            if(Request.RequestType == WebRequestMethods.Http.Get && behavior == JsonRequestBehavior.DenyGet)
                throw new InvalidOperationException("GET is not permitted for this request");

            var jsonResult = new ContentResult
            {
                Content = JsonConvert.SerializeObject(data, jsonSerializerSettings),
                ContentType = "application/json"
            };
            return jsonResult;
        }
    }
}
