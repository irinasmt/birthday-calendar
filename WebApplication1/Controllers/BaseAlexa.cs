using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using AlexaRules.Helpers;
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
                case "BirthdayByPeriodIntent":
                    response = new IntentRequestHandler(request).GetBirthdaysByPeriod();
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

    public enum PeriodEnum
    {
        CominSoon =1,
        ThreeWeeks=2,
        TwoWeeks =3
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
            var startDate = Convert.ToDateTime(_request.SlotsList.First().Value);
            var calendarService = new CalendarHelper(startDate,startDate,_request.Accesstoken);
            var result = calendarService.GetBirthdays();

            var birthDayResponse = result.Count > 0 ? FormStringFromList(result) : "There are no birthdays in the period that you asked for.";

            response = CalculateResponse(_request, birthDayResponse);
            response.Response.Card.Content = "";
            response.Response.Card.Title = "";
            return response;
        }

        public AlexaResponse GetBirthdaysByPeriod()
        {
            return null;
        }

        public AlexaResponse CalculateResponse(Request request, string brthdayString)
        {
            AlexaResponse response = new AlexaResponse();
            response.Response.OutputSpeech.Ssml = "<speak>"+ brthdayString + "</speak>";
            response.Response.OutputSpeech.Type = "SSML";
            response.Response.ShouldEndSession = false;
            AlexaResponse.ResponseAttributes.DirectivesAttributes directive = CreateDirectiveWithSlot(request);
            response.Response.Directives.Add(directive);

            return response;
        }

        public string FormStringFromList(List<KeyValuePair<string, string>> list)
        {
            string birthdayResponse=""; 
            foreach (var birthday in list)
            {
                birthdayResponse += "on" + birthday.Value + "is" + birthday.Key;
            }

            return birthdayResponse;
        }

        public AlexaResponse.ResponseAttributes.DirectivesAttributes CreateDirectiveWithSlot(Request request)
        {
            var directive = new AlexaResponse.ResponseAttributes.DirectivesAttributes();
            directive.UpdatedIntentAttributes.Name = request.Intent;
            directive.UpdatedIntentAttributes.Slots = request.Slots;
            return directive;
        }
    }
}