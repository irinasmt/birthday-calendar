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
    public class CalendarHelper
    {
        private EventsResource.ListRequest _request;
        private string _accessToken;
        private CalendarService _service;
        private DateTime _startDate;
        private DateTime _endDate;

        public CalendarHelper(DateTime startDate, DateTime endDate, string accessToken)
        {
            _startDate = startDate;
            _endDate = endDate;
            _accessToken = accessToken;
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
            _request.MaxResults = 5;
            _request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;
        }

        private List<KeyValuePair<string, string>> GetEvents()
        {
            List<KeyValuePair<string, string> >result= new List<KeyValuePair<string, string>>();
            Events events = _request.Execute();
            if (events.Items != null && events.Items.Count > 0)
            {
                foreach (var eventItem in events.Items)
                {
                    string when = eventItem.Start.DateTime.ToString();
                    if (String.IsNullOrEmpty(when))
                    {
                        when = eventItem.Start.Date;
                    }
                    result.Add(new KeyValuePair<string, string>(eventItem.Summary, when));
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

        public List<KeyValuePair<string, string>> GetBirthdays()
        {
            IntializeService();
            List<KeyValuePair<string,string> > result = new List<KeyValuePair<string, string>>();

            var facebookCalendarId = GetCalendarByString("Friends");
            CreateRequest(facebookCalendarId);
            result.AddRange(GetEvents());

            var primaryCalendarId = GetCalendarByString("primary");
            CreateRequest(primaryCalendarId);
            result.AddRange(GetEvents());


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