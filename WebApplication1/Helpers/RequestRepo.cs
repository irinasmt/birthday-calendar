using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Globalization;
using System.Linq;
using System.Web;
using AlexaRules.Helpers;

namespace WebApplication1.Helpers
{
    public class RequestRepo: BaseClass
    {
        private Request _request;
        public RequestRepo(Request request)
        {
            _request = request;

        }

        public void Insert()
        {
            
            using (BirthdayBookEntities db = new BirthdayBookEntities())
            {
               
                var userSessionsDetail = new UserSessionsDetail()
                {
                    MemberId = _request.MemberId.ToString(),
                    DateTime = _request.DateCreated,
                    Intent =_request.Intent,
                    AppId =_request.AppId,
                    UserId =_request.UserId,
                    IsNew = Convert.ToInt32(_request.IsNew),
                    RequestType=_request.Type,
                    Reason =_request.Reason,
                    SlotValue=_request.SlotsList.FirstOrDefault().Value
                 };
                db.UserSessionsDetails.Add(userSessionsDetail);
                try
                {
                    db.SaveChanges();
                    db.Dispose();
                }
                catch (Exception ex)
                {
                    log.Error(ex);
                }
            }

        }
        
    }
}