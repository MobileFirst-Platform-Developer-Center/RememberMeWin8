﻿/**
* Copyright 2016 IBM Corp.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Worklight;

namespace RememberMeWin8
{
    class UserLoginChallengeHandler : Worklight.WorklightChallengeHandler
    {
        public JObject challengeAnswer { get; set; }

        public override string SecurityCheck
        {
            get; set;
        }

        private bool shouldsubmitchallenge = false;
        private bool shouldsubmitfailure = false;
        private bool isChallenged = false;
        private string Realm;
        private MainPage mpage;

        public static ManualResetEvent waitForPincode = new ManualResetEvent(false);

        public UserLoginChallengeHandler(String securityCheck)
        {
            Realm = securityCheck;
        }

        public void SetMainPage(MainPage mpage)
        {
            this.mpage = mpage;
        }

        public override JObject GetChallengeAnswer()
        {
            return this.challengeAnswer;
        }

        public override void HandleChallenge(Object challenge)
        {
            isChallenged = true;
            MainPage._this.isLoggedOut = false;
            waitForPincode.Reset();
            MainPage._this.showChallenge(challenge);
            shouldsubmitchallenge = true;
            waitForPincode.WaitOne();
        }

        public override bool ShouldSubmitFailure()
        {
            return shouldsubmitfailure;
        }

        public override WorklightResponse GetSubmitFailureResponse()
        {
            JObject respJSON = new JObject();
            respJSON.Add("Respose", "Cancelled Request");

            WorklightResponse response = new WorklightResponse(false, "User cancelled the request", respJSON, "User cancelled the request", (int)HttpStatusCode.InternalServerError);
            return response;
        }

        public async void login(JObject credentials)
        {
            WorklightResponse response = null;
            if (isChallenged)
            {
                this.challengeAnswer = credentials;
                waitForPincode.Reset();
                shouldsubmitchallenge = true;
                UserLoginChallengeHandler.waitForPincode.Set();
            }

            else
            {
                response = await WorklightClient.CreateInstance().AuthorizationManager.Login(this.SecurityCheck, this.challengeAnswer);
            }


            if (response.Success)
            {
                Debug.WriteLine(response.ResponseText);
                MainPage._this.hideChallenge();
                Debug.WriteLine(response.ResponseJSON["successes"]["UserLogin"]["user"]["id"]);
                string userName = response.ResponseJSON["successes"]["UserLogin"]["user"]["id"].ToString();
                var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                localSettings.Values["useridentity"] = userName;

                MainPage._this.AddUserName(userName);

            }

        }

        public async void logout(String SecurityCheck)
        {
            WorklightResponse response = await WorklightClient.CreateInstance().AuthorizationManager.Logout(SecurityCheck);

            if (response.Success)
            {
                MainPage._this.showChallenge(null);
                this.isChallenged = false;
                this.shouldsubmitchallenge = true;
                MainPage._this.AddUserName("");
                MainPage._this.isLoggedOut = true;
            }

        }


        public override bool ShouldSubmitChallengeAnswer()
        {
            return this.shouldsubmitchallenge;
        }


        public void SetShouldSubmitChallenge(bool shouldsubmitchallenge)
        {
            this.shouldsubmitchallenge = shouldsubmitchallenge;
        }

        public void SetSubmitFailure(bool shouldsubmitfailure)
        {
            this.shouldsubmitfailure = shouldsubmitfailure;
        }

        public override void HandleFailure(JObject error)
        {
            Debug.WriteLine("Error");
        }

        public override void HandleSuccess(JObject identity)
        {
            Debug.WriteLine("Success");
            Debug.WriteLine(identity.GetValue("user"));

            this.shouldsubmitchallenge = false;

            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            localSettings.Values["useridentity"] = identity.GetValue("user");

        }

        public override void SubmitChallengeAnswer(object answer)
        {
            challengeAnswer = (JObject)answer;
        }


    }
}