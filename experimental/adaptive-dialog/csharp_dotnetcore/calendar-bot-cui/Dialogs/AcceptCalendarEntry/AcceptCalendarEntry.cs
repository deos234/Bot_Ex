﻿using System.Collections.Generic;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Input;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Steps;
using Microsoft.Bot.Builder.LanguageGeneration;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

/// <summary>
/// This dialog will accept all the calendar entris if they have the same subject
/// </summary>
namespace Microsoft.CalendarSample
{
    public class AcceptCalendarEntry : ComponentDialog
    {
        private static IConfiguration Configuration;
        public AcceptCalendarEntry(IConfiguration configuration)
            : base(nameof(AcceptCalendarEntry))
        {
            Configuration = configuration;
            // Create instance of adaptive dialog. 
            var acceptCalendarEntry = new AdaptiveDialog(nameof(AdaptiveDialog))
            {
                Generator = new ResourceMultiLanguageGenerator("AcceptCalendarEntry.lg"),
                Steps = new List<IDialog>()
                {
                    new SendActivity("[EmptyFocusedMeeting]"),
                    new SetProperty()
                    {
                        Property = "user.ShowAllMeetingDialog_pageIndex",// index must be set to zero
                        Value = "0" // in case we have not entered FindCalendarEntry from RootDialog
                    },
                    new BeginDialog("ShowAllMeetingDialog"),
                    new IfCondition()
                    {
                        Condition = "user.focusedMeeting == null",
                        Steps = new List<IDialog>(){
                            new SendActivity("[EmptyCalendar]"),
                            new EndDialog()
                        }
                    },
                    new ConfirmInput()
                    {
                        Property = "turn.AcceptCalendarEntry_ConfirmChoice",
                        Prompt = new ActivityTemplate("[ConfirmPrompt]"),
                        InvalidPrompt = new ActivityTemplate("[YesOrNo]"),
                    },
                    new IfCondition()
                    {
                        Condition = "turn.AcceptCalendarEntry_ConfirmChoice",
                        Steps = new List<IDialog>()
                        {
                            new IfCondition() // we cannot accept a entry if we are the origanizer
                            {
                                Condition = "user.focusedMeeting.isOrganizer != true",
                                Steps = new List<IDialog>()
                                {
                                    new HttpRequest()
                                    {
                                        Property = "user.acceptResponse",
                                        Method = HttpRequest.HttpMethod.POST,
                                        Url = "https://graph.microsoft.com/v1.0/me/events/{user.focusedMeeting.id}/accept",
                                        //Url = "https://graph.microsoft.com/v1.0/me/events/AAMkADY2MzM5M2UzLWQ0NmItNDU2My1hN2NjLTliMjRiYWE5YWQ4ZABGAAAAAADRv-cRMwIfQKntE9IXL-ciBwDVXUsVK2tOTK5RjTff3j-IAAAAAAENAADVXUsVK2tOTK5RjTff3j-IAAAfRHvTAAA=/accept",
                                        Headers =  new Dictionary<string, string>()
                                        {
                                            ["Authorization"] = "Bearer {user.token.Token}",
                                        },
                                        Body = JObject.Parse(@"{
                                          '1': '1'
                                        }") // this is a place holder issue
                                    },
                                    new SendActivity("[AcceptReadBack]")
                                },
                                ElseSteps = new List<IDialog>(){
                                    new SendActivity("[CannotAcceptOrganizer]")
                                }
                            }
                        }
                    },
                    // new SendActivity("finish http request"),
                    new SendActivity("[Welcome-Actions]"),
                    new EndDialog()
                 },
            };

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            AddDialog(acceptCalendarEntry);
            acceptCalendarEntry.AddDialog(
                new List<Dialog> {
                    new ShowAllMeetingDialog(Configuration)
                });


            // The initial child Dialog to run.
            InitialDialogId = nameof(AdaptiveDialog);
        }
    }
}
