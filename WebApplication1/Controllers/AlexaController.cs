using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Http;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using WebApplication1.Helpers;

namespace WebApplication1.Controllers
{
    public class AlexaController : ApiController
    {
        private const string ApplicationID = "amzn1.ask.skill.f23cdce2-a0bb-401d-9b89-5bb1e0ae83d4";

        [HttpPost, Route("api/alexa")]
        public dynamic Index(AlexaRequest alexaRequest)
        {

            if (alexaRequest.Session.Application.ApplicationId != ApplicationID)
            {
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.BadRequest));
            }

            string cardTitle = "Birthday Book";
            string skillName = "Birthday Book";
            var alexa = new BaseAlexa( cardTitle, skillName);
            return alexa.Index(alexaRequest);
        }
        
    }

   
}
