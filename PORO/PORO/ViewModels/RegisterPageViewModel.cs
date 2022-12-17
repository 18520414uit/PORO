using PORO.Models;
using PORO.Services;
using PORO.Untilities;
using PORO.ViewModels.Base;
using PORO.Views.Popups;
using Prism.Navigation;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Windows.Input;
using Xamarin.Forms;

namespace PORO.ViewModels
{
    public class RegisterPageViewModel : BaseViewModel
    {

        #region Properties
        private string _userName;
        public string UserName
        {
            get => _userName;
            set => SetProperty(ref _userName, value);
        }
        private string _email;
        public string EmailAddress
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }
        private string _password;
        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }
        private string _confirmPassword;
        public string ConfirmPassword
        {
            get => _confirmPassword;
            set => SetProperty(ref _confirmPassword, value);
        }
        private UserModel _userModel;
        public UserModel UserModels
        {
            get => _userModel;
            set => SetProperty(ref _userModel, value);
        }
        #endregion

        #region Contructors
        public RegisterPageViewModel(INavigationService navigationService) : base(navigationService)
        {
            SignUpCommand = new Command(ExcuteSignUp);
            SignInCommand = new Command(ExcuteSignIn);
        }
        #endregion

        #region SignUp
        public ICommand SignUpCommand { get; set; }
        public async void ExcuteSignUp()
        {
            #region CheckEmpty
            if (string.IsNullOrEmpty(UserName))
            {
                await MessagePopup.Instance.Show("Please Enter UserName");
                return;
            }
            if (string.IsNullOrEmpty(EmailAddress))
            {
                await MessagePopup.Instance.Show("Please Enter Emaill Address");
                return;
            }
            if (string.IsNullOrEmpty(Password))
            {
                await MessagePopup.Instance.Show("Please Enter Password");
                return;
            }
            if (string.IsNullOrEmpty(ConfirmPassword))
            {
                await MessagePopup.Instance.Show("Please Enter Confirm Password");
                return;
            }
            else if(ConfirmPassword != Password)
            {
                await MessagePopup.Instance.Show("Confirm Password Wrong");
                return;
            }
            #endregion

            UserModels = new UserModel()
            {
                UserName = UserName,
                Email = EmailAddress,
                Password = Password
            };
            SignUp();
        }

        public async void SignUp()
        {
            await LoadingPopup.Instance.Show();
            var url = ApiUrl.Register();

            var param = UserModels;
            HttpClientHandler clientHandler = new HttpClientHandler();
            clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };

            HttpClient client = new HttpClient(clientHandler);
            var httpContent = param.ObjectToStringContent();
            if (httpContent != null)
            {
                var response = await client.PostAsync(requestUri: url, content: httpContent);
                await LoadingPopup.Instance.Hide();
                RegisterResponse(response);
            }
            await LoadingPopup.Instance.Hide();
        }
        private async void RegisterResponse(HttpResponseMessage response)
        {
            if (response == null)
            {
                await MessagePopup.Instance.Show("Check Internet");
            }
            else if (response.IsSuccessStatusCode)
            {
                await MessagePopup.Instance.Show(("Account successfully created"),
                    closeCommand: new Command(async () =>
                    {
                        await Navigation.NavigateAsync($"/{ManagerPage.LoginPage}", animated: false);
                    }));
            }
            else
            {
                await MessagePopup.Instance.Show("Account creation failed");
            }
        }
        #endregion


        #region SignIn
        public ICommand SignInCommand { get; set; }
        public async void ExcuteSignIn()
        {
            await Navigation.NavigateAsync($"/{ManagerPage.LoginPage}", animated: false);
        }
        #endregion
    }
}
