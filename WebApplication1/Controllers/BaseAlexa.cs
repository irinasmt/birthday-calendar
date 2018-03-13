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
using static AlexaRules.Helpers.AlexaResponse;

namespace WebApplication1.Controllers
{
    public class BaseAlexa:BaseClass
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
            //    log.Error("Someone tried to hack you");
            //    throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.BadRequest));
            //}

            var request = new Request
            {
                MemberId = alexaRequest.Session.Attributes?.MemberId ?? 0,
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
                SlotsList = alexaRequest.Request.Intent?.GetSlots(),
                Slots = alexaRequest.Request.Intent?.Slots,
                DateCreated = DateTime.UtcNow,
                CardTitle = CardTitle,
                Accesstoken = alexaRequest.Session.User.AccessToken
            };

            if (alexaRequest.Context.System.User.Permissions == null)
            {
                
                return AskTheUserToGiveConsent(alexaRequest);
            }

            var repo = new RequestRepo(request);
            repo.Insert();



            if (alexaRequest.Session.User.AccessToken == null)
            {
                log.Info("User " + request.UserId+ " Was not logged in with google");
                return AskTheUserToauthenticate(alexaRequest);
            }


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

        private AlexaResponse AskTheUserToGiveConsent(AlexaRequest request)
        {
            AlexaResponse response = new AlexaResponse();
            response.Response.OutputSpeech.Text = "If you want to receive your birthdays in your to do list please grant permissions";

            response.Response.OutputSpeech.Type = "PlainText";
            response.Response.Card.Type = "AskForPermissionsConsent";
            response.Response.Card.Permissions = new List<string> {"write::alexa:household:list" };

            return response;
        }

        private AlexaResponse AskTheUserToauthenticate(AlexaRequest request)
        {
            AlexaResponse response = new AlexaResponse();
            response.Response.OutputSpeech.Text ="You must have a Google account to use this skill. Please use the Alexa app to link your Amazon account with your Google Account.";

            response.Response.OutputSpeech.Type = "PlainText";
            response.Response.Card.Type = "LinkAccount";

            return response;
        }

        public AlexaResponse IntentRequestHandler(Request request)
        {
            AlexaResponse response = null;

            switch (request.Intent)
            {
                case "BirthdayByDateIntent":
                    response = new IntentRequestHandler(request, CardTitle).GetBirthdaysByDate();
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
            response.Response.Card.Content = "Alexa, ask Birthday Book for brithdays today / tomorrow / this week ";
            response.Response.ShouldEndSession = false;
            response.Response.OutputSpeech.Type = "SSML";
            response.Response.OutputSpeech.Ssml = "<speak>" +
                                                  "<s> If you want to hear birthdays from Facebook friends, follow the short tutorial in the skill description.</s> " +
                                                  "<s>To use the skill you can say, Alexa, ask " + SkillName + " for birthdays today </s> <s>Or you can say, Alexa, ask "
                                                  + SkillName + " for birthdays tomorrow  </s>" +
                                                  "<s>You can also say Alexa, ask "+ SkillName + "for birthdays this week </s>" +
                                                  "<s>For now, which one do you want to hear, today, Tomorrow, Or this weeks birthdays?</s></speak>";
            return response;
        }

        private AlexaResponse CancelOrStopIntentHandler(Request request)
        {
            return new AlexaResponse("<speak>Thanks for listening, let's talk again soon.</speak>", true);
        }

        public AlexaResponse LaunchRequestHandler(Request request)
        {
            AlexaResponse response = new AlexaResponse();
            response.Response.OutputSpeech.Ssml = response.Response.OutputSpeech.Ssml = "<speak>" +
                                                                                        "<s> If you want to hear birthdays from Facebook friends, follow the short tutorial in the skill description.</s> " +
                                                                                        "<s>To use the skill you can say, Alexa, ask " + SkillName + " for birthdays today </s> <s>Or you can say, Alexa, ask "
                                                                                        + SkillName + " for birthdays tomorrow  </s>" +
                                                                                        "<s>You can also say Alexa, ask " + SkillName + "for birthdays this week </s>" +
                                                                                        "<s>For now, which one do you want to hear, today, Tomorrow, Or this weeks birthdays?</s></speak>";
            response.Response.OutputSpeech.Type = "SSML";
            response.Session.MemberId = request.MemberId;
            response.Response.Reprompt.OutputSpeech.Ssml = "<speak>Which one do you want to hear: today, tomorrow or this week birthdays?</speak>";
            response.Response.Card.Content = "Alexa, ask Birthday Book birthdays today/tomorrow/this week";
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
        private string _cardTitle;

        public IntentRequestHandler(Request request, string cardTitle)
        { 
            _request = request;
            _cardTitle = cardTitle;
        }

        public AlexaResponse GetBirthdaysByDate()
        {
            AlexaResponse response= new AlexaResponse();
            DateTime startDate;
            DateTime endDate;
            var userInput = _request.SlotsList.First().Value;
            if (userInput == null)
            {
                response.Response.OutputSpeech.Ssml = "<speak>Sorry I didn't get that. On which period or date would you like me to tell your birthdays for.</speak>";
                response.Response.OutputSpeech.Type = "SSML";
                response.Response.ShouldEndSession = false;
                ResponseAttributes.DirectivesAttributes directive = CreateDirectiveWithSolicitSlot(_request);
                response.Response.Directives.Add(directive);
                response.Response.Card.Content =
                    "Sorry I didn't get that. On which period or date would you like me to tell your birthdays for.\n You can ask for:\n 1) a specifc date(i.e. on 5th of March);\n 2) a period like today, tomorrow, this month, next month etc...";
                response.Response.Card.Title = _cardTitle + ". Examples of periods you can ask for";
                return response;
            }
            switch (userInput.Length)
            {
                // this is by month
                case 7:
                    startDate = Convert.ToDateTime(_request.SlotsList.First().Value);
                    endDate = startDate.AddMonths(1);
                    break;
                // this has the week number
                case 8:
                    var tempDate = FirstDateOfWeekISO8601(DateTime.Now.Year, Convert.ToInt32(userInput.Substring(6, 2)));
                    startDate = Convert.ToDateTime(tempDate);
                    endDate = startDate.AddDays(7);
                    break;
                case 4:
                    var year = Convert.ToInt32(_request.SlotsList.First().Value);
                    startDate = new DateTime(year,1,1);
                    endDate = startDate.AddYears(1);
                    break;
                default:
                    startDate = Convert.ToDateTime(_request.SlotsList.First().Value);
                    endDate = startDate.AddDays(1);
                    break;
            }

            var calendarService = new CalendarHelper(startDate, endDate, _request.Accesstoken, _request.UserId);
            var result = calendarService.GetBirthdays();

            var birthDayResponse = BirthDayResponse(result);
            var cardAttributes = CreateCardContent(result);

            response = CalculateResponse(_request, birthDayResponse, cardAttributes);
            return response;
        }

        private string BirthDayResponse(Dictionary<string, string> result)
        {
            var birthDayResponse = result !=null && result.Count > 0
                ? FormStringFromList(result)
                : "There are no birthdays in the period that you asked for.";
            return birthDayResponse;
        }

        public AlexaResponse CalculateResponse(Request request, string brthdayString, ResponseAttributes.CardAttributes card)
        {
            AlexaResponse response = new AlexaResponse();
            response.Response.OutputSpeech.Ssml = "<speak>" + brthdayString + "</speak>";
            response.Response.OutputSpeech.Type = "SSML";
            response.Response.Card = card;

            return response;
        }

        public ResponseAttributes.CardAttributes CreateCardContent(Dictionary<string, string> list)
        {
            var cardAttributes = new ResponseAttributes.CardAttributes();

            cardAttributes.Title = _cardTitle;
            if (list.Count == 0)
            {
                cardAttributes.Content = "There are no birthdays in the period that you asked for.";
            }

            if (list.ContainsKey("5"))
            {
                cardAttributes.Content += list["5"];
            }

            foreach (var birthday in list)
            {
                if (birthday.Key == "5")
                {
                    continue;
                }
                cardAttributes.Content += "\n"+ " On " + birthday.Key + " is " + birthday.Value.Replace("<s>","").Replace("</s>", "");
            }

            

            return cardAttributes;
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

        public ResponseAttributes.DirectivesAttributes CreateDirectiveWithSlot(Request request)
        {
            var directive = new ResponseAttributes.DirectivesAttributes
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

        public ResponseAttributes.DirectivesAttributes CreateDirectiveWithSolicitSlot(Request request)
        {
            var directive = new ResponseAttributes.DirectivesAttributes();
            directive.SlotToElicit = "date";
            directive.Type = "Dialog.ElicitSlot";
            directive.UpdatedIntentAttributes.Name = request.Intent;
            directive.UpdatedIntentAttributes.Slots = request.Slots;
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