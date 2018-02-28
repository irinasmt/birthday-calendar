using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using AlexaRules.Helpers;
using Microsoft.Ajax.Utilities;
using WebApplication1.Helpers;
using WebGrease.Css.Ast;

namespace WebApplication1.Controllers
{
    public class BaseAlexa
    {
        public string CardTitle;
        public string SkillName;
        public BaseAlexa( string cardTitle, string skillName)
        {
            CardTitle = cardTitle;
            SkillName = skillName;
        }
        public dynamic Index(AlexaRequest alexaRequest)
        {
            var totalSecons = (DateTime.UtcNow - alexaRequest.Request.Timestamp).TotalSeconds;
            //if (totalSecons < 0 || totalSecons > 150)
            //{
            //    throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.BadRequest));
            //}

            var request = new Request
            {
                MemberId = (alexaRequest.Session.Attributes == null) ? 0 : alexaRequest.Session.Attributes.MemberId,
                Timestamp = alexaRequest.Request.Timestamp,
                Intent = (alexaRequest.Request.Intent == null) ? "" : alexaRequest.Request.Intent.Name,
                AppId = alexaRequest.Session.Application.ApplicationId,
                RequestId = alexaRequest.Request.RequestId,
                SessionId = alexaRequest.Session.SessionId,
                UserId = alexaRequest.Session.User.UserId,
                IsNew = alexaRequest.Session.New,
                Version = alexaRequest.Version,
                Type = alexaRequest.Request.Type,
                Reason = alexaRequest.Request.Reason,
                SlotsList = alexaRequest.Request.Intent.GetSlots(),
                Slots = alexaRequest.Request.Intent.Slots,
                DateCreated = DateTime.UtcNow,
                CardTitle = CardTitle,
                Accesstoken = alexaRequest.Session.User.AccessToken
            };

            AlexaResponse response = new AlexaResponse();

            switch (request.Type)
            {
                case "LaunchRequest":
                    response = LaunchRequestHandler(request);
                    break;
                case "IntentRequest":
                    response = IntentRequestHandler(request);
                    break;
                case "SessionEndedRequest":
                    response = SessionEndedRequestHandler(request);
                    break;
            }

            return response;
        }

        public AlexaResponse IntentRequestHandler(Request request)
        {
            AlexaResponse response = null;

            switch (request.Intent)
            {
                case "BirthdayByDateIntent":
                    response = new IntentRequestHandler(request).GetBirthdaysByDate();
                    break;
                case "AMAZON.CancelIntent":
                case "AMAZON.StopIntent":
                    response = CancelOrStopIntentHandler(request);
                    break;
                case "AMAZON.HelpIntent":
                    response = HelpIntent(request);
                    break;
            }

            return response;
        }

        private AlexaResponse HelpIntent(Request request)
        {
            AlexaResponse response = new AlexaResponse();
            response.Response.Card.Content = "Each week Alexa asks 5 new quesions. Just say: 'Alexa start C sharp question' and then pick a category from beginner and inetermediate.";
            response.Response.ShouldEndSession = false;
            response.Response.OutputSpeech.Type = "SSML";
            response.Response.OutputSpeech.Ssml = "<speak>To use the " + SkillName + " skill, you can say, Alexa, ask " + SkillName + " for beginner, or you can say, Alexa, ask " + SkillName + " for intermmediate. To repeat a question you can say, repeat. You can also say, Alexa, stop or Alexa, cancel, at any time to exit the " + SkillName + " skill. For now, which one do you want to hear, beginner or intermediate questions?</speak>";
            return response;
        }

        private AlexaResponse CancelOrStopIntentHandler(Request request)
        {
            return new AlexaResponse("<speak>Thanks for listening, let's talk again soon.</speak>", true);
        }

        public AlexaResponse LaunchRequestHandler(Request request)
        {
            AlexaResponse response = new AlexaResponse();
            response.Response.OutputSpeech.Ssml = "<speak>To use the " + SkillName + " skill, you can say, Alexa, ask " + SkillName + " for beginner, or you can say, Alexa, ask " + SkillName + " for intermmediate. To repeat a question you can say, repeat. You can also say, Alexa, stop or Alexa, cancel, at any time to exit the " + SkillName + " skill. For now, which one do you want to hear, beginner or intermediate questions?</speak>";
            response.Response.OutputSpeech.Type = "SSML";
            response.Session.MemberId = request.MemberId;
            response.Response.Reprompt.OutputSpeech.Ssml = "<speak>Please select one, beginner or intermediate?</speak>";
            response.Response.Card.Content = "Each week Alexa asks 5 new quesions. Just say: 'Alexa, start " + SkillName + "' and then pick a category from beginner and inetermediate.";
            response.Response.ShouldEndSession = false;

            return response;
        }

        private AlexaResponse SessionEndedRequestHandler(Request request)
        {
            return null;
        }


    }


    public class IntentRequestHandler
    {
        private Request _request;

        public IntentRequestHandler(Request request)
        { 
            _request = request;
        }

        public AlexaResponse GetBirthdaysByDate()
        {
            AlexaResponse response;
            DateTime startDate;
            DateTime endDate;
            var userInput = _request.SlotsList.First().Value;
            switch (userInput.Length)
            {
                // this is by month
                case 7:
                    startDate = Convert.ToDateTime(_request.SlotsList.First().Value);
                    endDate = startDate.AddMonths(1);
                    break;
                // this has the week number
                case 8:
                case 4:
                    var tempDate = FirstDateOfWeekISO8601(DateTime.Now.Year, Convert.ToInt32(userInput.Substring(6, 2)));
                    startDate = Convert.ToDateTime(tempDate);
                    endDate = startDate.AddDays(7);
                    break;
               
                default:
                    startDate = Convert.ToDateTime(_request.SlotsList.First().Value);
                    endDate = startDate;
                    break;
            }

            
            var birthDayResponse = BirthDayResponse(startDate, endDate);

            response = CalculateResponse(_request, birthDayResponse);
            response.Response.Card.Content = "";
            response.Response.Card.Title = "";
            return response;
        }

        private string BirthDayResponse(DateTime startDate, DateTime endDate)
        {
            var calendarService = new CalendarHelper(startDate, endDate, _request.Accesstoken);
            var result = calendarService.GetBirthdays();

            var birthDayResponse = result.Count > 0
                ? FormStringFromList(result)
                : "There are no birthdays in the period that you asked for.";
            return birthDayResponse;
        }

        public AlexaResponse CalculateResponse(Request request, string brthdayString)
        {
            AlexaResponse response = new AlexaResponse();

            response.Response.OutputSpeech.Ssml = "<speak>" + brthdayString + "</speak>";
            response.Response.OutputSpeech.Type = "SSML";

            return response;
        }

        public string FormStringFromList(Dictionary<string, string> list)
        {
            string birthdayResponse="";
            if (list.ContainsKey("5"))
            {
                birthdayResponse += list["5"];
            }
            foreach (var birthday in list)
            {
                if (birthday.Key=="5")
                {
                    continue;
                }
                birthdayResponse += "<break time=\'1s\'/> On " + birthday.Key + " is " + birthday.Value;
            }

            return "<emphasis level=\"moderate\">" + birthdayResponse+ "</emphasis>";
        }

        public AlexaResponse.ResponseAttributes.DirectivesAttributes CreateDirectiveWithSlot(Request request)
        {
            var directive = new AlexaResponse.ResponseAttributes.DirectivesAttributes
            {
                Type = "Dialog.Delegate",
                UpdatedIntentAttributes =
                {
                    Name = request.Intent,
                    Slots = request.Slots
                }
            };
            return directive;
        }

        public static DateTime FirstDateOfWeekISO8601(int year, int weekOfYear)
        {
            DateTime jan1 = new DateTime(year, 1, 1);
            int daysOffset = DayOfWeek.Thursday - jan1.DayOfWeek;

            DateTime firstThursday = jan1.AddDays(daysOffset);
            var cal = CultureInfo.CurrentCulture.Calendar;
            int firstWeek = cal.GetWeekOfYear(firstThursday, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);

            var weekNum = weekOfYear;
            if (firstWeek <= 1)
            {
                weekNum -= 1;
            }
            var result = firstThursday.AddDays(weekNum * 7);
            return result.AddDays(-3);
        }
    }

}