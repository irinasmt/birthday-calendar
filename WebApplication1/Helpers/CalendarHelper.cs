using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Http;
using Google.Apis.Services;
using WebApplication1.Controllers;

namespace WebApplication1.Helpers
{
    public class CalendarHelper:BaseClass
    {
        private EventsResource.ListRequest _request;
        private string _accessToken;
        private CalendarService _service;
        private DateTime _startDate;
        private DateTime _endDate;
        private string _userId;

        public CalendarHelper(DateTime startDate, DateTime endDate, string accessToken, string userId)
        {
            _startDate = startDate;
            _endDate = endDate;
            _accessToken = accessToken;
            _userId = userId;
        }

        private void IntializeService()
        {
            _service = new CalendarService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = new CustomUserCredential(_accessToken)
            });
        }

        private void CreateRequest(string calendarId)
        {
            _request = _service.Events.List(calendarId);
            _request.TimeMin = _startDate;
            _request.TimeMax = _endDate;
            _request.ShowDeleted = false;
            _request.SingleEvents = true;
            _request.MaxResults = 6;
            _request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;
        }

        private Dictionary<string, string> GetEvents(int numberOfBirthdays)
        {
            Dictionary<string, string>result= new Dictionary<string, string>();
            Events events = _request.Execute();
            var birthDaysEvents = events.Items?.Where(x => x.Summary.Contains("birthday")).ToList();
            if (birthDaysEvents != null && birthDaysEvents.Count > 0)
            {
                if (birthDaysEvents.Count > 5)
                {
                    result.Add("5", "In the period that you asked for, there are more than five birthdays. Here are the first five.");
                }

                var count = numberOfBirthdays;
                foreach (var eventItem in birthDaysEvents)
                {
                    count++;
                    if (count > 5)
                    {
                        break;
                    }

                    var when = eventItem.Start.DateTime != null 
                        ? Convert.ToDateTime(eventItem.Start.DateTime).ToUniversalTime().ToString("dd/MM/yyyy") 
                        :Convert.ToDateTime(eventItem.Start.Date).ToUniversalTime().ToString("dd/MM/yyyy");

                    if (result.ContainsKey(when))
                    {
                        result[when] = result[when].Replace("birthday", "") + "<s> and </s> " +
                                       eventItem.Summary.Replace("'s", "");
                    }
                    else
                    {
                        result.Add(when, eventItem.Summary.Replace("'s", ""));

                    }
                }
            }

            return result;
        }

        private string GetCalendarByString(string calendarName)
        {
            var requestCalendar = _service.CalendarList.List();
            var result = requestCalendar.Execute();
            if (result.Items != null && result.Items.Count > 0)
            {
                foreach (var eventItem in result.Items)
                {
                    if (eventItem.Summary.Contains(calendarName))
                    {
                        return eventItem.Id;
                    }

                    if (calendarName == "primary")
                    {
                        if (eventItem.Primary == true)
                        {
                            return eventItem.Id;
                        }
                    }

                }
            }

            return null;
        }

        public Dictionary<string, string> GetBirthdays()
        {
            IntializeService();
            Dictionary<string,string> result= new Dictionary<string, string>();

            var facebookCalendarId = GetCalendarByString("Friends");
            if (facebookCalendarId != null)
            {
                CreateRequest(facebookCalendarId);
                log.Info("The facebook calendar for user " + _userId + "has been found");
                result = GetEvents(0);

            }

            if (result != null && result.Count == 5)
            {
                return result;
            }

            var primaryCalendarId = GetCalendarByString("primary");
            log.Info("The google calendar for user "+_userId+" has been found");
            CreateRequest(primaryCalendarId);
            result = result?.Concat(GetEvents(result.Count)).ToDictionary(x => x.Key, x => x.Value) ?? GetEvents(0);

            return result;
        }
    }

    internal class CustomUserCredential : IHttpExecuteInterceptor, IConfigurableHttpClientInitializer
    {
        private string _accessToken;

        public CustomUserCredential(string accessToken)
        {
            _accessToken = accessToken;
        }

        public void Initialize(ConfigurableHttpClient httpClient)
        {
            httpClient.MessageHandler.ExecuteInterceptors.Add(this);
        }

        public async Task InterceptAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        }
    }
}