/**
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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Text;
using Newtonsoft.Json.Linq;
using Windows.ApplicationModel.Core;
using Windows.UI;
using Windows.UI.Core;
using Worklight;
using System.Diagnostics;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace RememberMeWin8
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public static MainPage _this;
        public bool isLoggedOut { get; set; }
        UserLoginChallengeHandler userChallengeHandler;
        IWorklightClient _newClient;

        public MainPage()
        {
            this.InitializeComponent();
            _this = this;
            userChallengeHandler = new UserLoginChallengeHandler("UserLogin");
            userChallengeHandler.SetShouldSubmitChallenge(false);
            userChallengeHandler.SecurityCheck = "UserLogin";
            userChallengeHandler.SetSubmitFailure(false);

            _newClient = WorklightClient.CreateInstance();

            _newClient.RegisterChallengeHandler(userChallengeHandler);

            getAccessToken();

        }

        private async void getAccessToken()
        {
            WorklightAccessToken accessToken = await _newClient.AuthorizationManager.ObtainAccessToken(userChallengeHandler.SecurityCheck);

            if (accessToken.IsValidToken && accessToken.Value != null && accessToken.Value != "")
            {
                Debug.WriteLine("Success");
                _this.hideChallenge();
                userChallengeHandler.SetShouldSubmitChallenge(false);

            }
            else
            {
                Debug.WriteLine("Failure");
            }

        }

        private async void GetBalance_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                StringBuilder uriBuilder = new StringBuilder().Append("/adapters").Append("/ResourceAdapter").Append("/balance");

                Debug.WriteLine(new Uri(uriBuilder.ToString(), UriKind.Relative));

                WorklightResourceRequest rr = _newClient.ResourceRequest(new Uri(uriBuilder.ToString(), UriKind.Relative), "GET", "accessRestricted");

                WorklightResponse resp = await rr.Send();

                System.Diagnostics.Debug.WriteLine(resp.ResponseText);

                AddTextToConsole(resp.ResponseText);

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.StackTrace);
            }
        }

        public void AddTextToConsole(String consoleText)
        {
            CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                 async () =>
                 {
                     MainPage._this.Console.Text = "Balance: " + consoleText;

                 });
        }

        public void AddUserName(String userName)
        {
            CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                async () =>
                {
                    if (userName != "")
                    {
                        _this.UserName.Text = "Hello " + userName;
                    }

                    else
                    {
                        _this.UserName.Text = "";
                    }
                });

        }

        private void ClearConsole(object sender, DoubleTappedRoutedEventArgs e)
        {
            Console.Text = "";
        }

        private void ShowConsole(object sender, TappedRoutedEventArgs e)
        {
            MainPage._this.ConsolePanel.Visibility = Visibility.Visible;
            MainPage._this.ConsoleTab.Foreground = new SolidColorBrush(Colors.DodgerBlue);
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (username.Text != "" && password.Text != "")
            {
                JObject userJSON = new JObject();
                userJSON.Add("username", username.Text);
                userJSON.Add("password", password.Text);
                userJSON.Add("rememberMe", RememberMe.IsChecked);

                userChallengeHandler.SetShouldSubmitChallenge(true);
                userChallengeHandler.challengeAnswer = userJSON;
                UserLoginChallengeHandler.waitForPincode.Set();
                if (this.isLoggedOut)
                {
                    userChallengeHandler.login(userJSON);
                }

                hideChallenge();

                this.AddUserName(username.Text);

            }
            else
            {
                CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                 async () =>
                 {
                     _this.HintText.Text = "Username and password are required";

                 });
            }

        }

        public async void showChallenge(Object challenge)
        {
            String errorMsg = "";

            JObject challengeJSON = (JObject)challenge;

            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                 async () =>
                 {
                     Console.Text = "";
                     _this.HintText.Text = "";
                     _this.LoginGrid.Visibility = Visibility.Visible;

                     if (challengeJSON != null)
                     {
                         if (errorMsg != "")
                         {
                             _this.HintText.Text = errorMsg + "Remaining Attempts: " + challengeJSON.GetValue("remainingAttempts");
                         }
                         else
                         {
                             _this.HintText.Text = challengeJSON.GetValue("errorMsg") + "\n" + "Remaining Attempts: " + challengeJSON.GetValue("remainingAttempts");
                         }
                     }
                     else
                     {
                         _this.username.Text = "";
                         _this.password.Text = "";
                     }

                     _this.GetBalance.IsEnabled = false;
                     _this.Logout.IsEnabled = false;
                 });
        }

        public void hideChallenge()
        {

            CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                async () =>
                {
                    Console.Text = "";
                    _this.LoginGrid.Visibility = Visibility.Collapsed;
                    _this.GetBalance.IsEnabled = true;
                    _this.Logout.IsEnabled = true;
                });
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                async () =>
                {
                    Console.Text = "";
                });
            userChallengeHandler.SetSubmitFailure(true);
            userChallengeHandler.SetShouldSubmitChallenge(false);
            UserLoginChallengeHandler.waitForPincode.Set();

        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            userChallengeHandler.logout(userChallengeHandler.SecurityCheck);
        }
    }
}
