using PORO.Enums;
using PORO.Models;
using PORO.ViewModels.Base;
using Prism.Navigation;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Xamarin.Forms;
using PORO.Views.Popups;
using System.Windows.Input;
using Xamarin.Essentials;
using PORO.Services;
using System.Net.Http;
using PORO.Untilities;

namespace PORO.ViewModels
{
    public class PublishPageViewModel : BaseViewModel
    {
        #region Properties
        public ImageSource _imageReview;
        public ImageSource ImageReview
        {
            get => _imageReview;
            set => SetProperty(ref _imageReview, value);
        }
        public byte[] ImageFromFile { get; set; }
        public string path { get; set; }
        private PublishModel _publishModdel;
        public PublishModel PublishModels
        {
            get => _publishModdel;
            set => SetProperty(ref _publishModdel, value);
        }
        private string _itemDescription;
        public string Description
        {
            get => _itemDescription;
            set => SetProperty(ref _itemDescription, value);
        }
        private DatabaseModel dataModel = new DatabaseModel();
        #endregion

        #region Constructors
        public PublishPageViewModel(INavigationService navigationService)
            : base(navigationService)
        {
            PublishModels = new PublishModel();
            PostCommand = new Command(ExcutePost);
        }
        #endregion

        #region Navigation
        public async override void OnNavigatedTo(INavigationParameters parameters)
        {
            if (parameters != null)
            {
                if (parameters.ContainsKey(ParamKeys.ImageToMint.ToString()))
                {
                    dataModel = (DatabaseModel)parameters[ParamKeys.ImageToMint.ToString()];
                    path = dataModel.filepath;
                    ImageFromFile = File.ReadAllBytes(path);
                    ImageReview = ImageSource.FromFile(path);
                }
            }
        }
        #endregion
        #region PostImage
        public ICommand PostCommand { get; set; }
        public async void ExcutePost()
        {
            var userId = Preferences.Get("userId", 0);
            #region CheckEmpty
            if (string.IsNullOrEmpty(Description))
            {
                await MessagePopup.Instance.Show("Please Enter Description");
                return;
            }
            #endregion
            PublishModels = new PublishModel()
            {
                Id = "1",
                //UserId = userId,
                Description = Description,
                Image = path,
                Name = DateTime.Now.ToString()
            };
            await ConfirmPopup.Instance.Show(message: "Confirm Pulish",
                      acceptCommand: new Command(async () =>
                      {
                          PublishPhoto();
                      }));
        }
        private async void PublishPhoto()
        {
            await LoadingPopup.Instance.Show();
            var url = ApiUrl.UploadPhoto();

            var param = PublishModels;
            HttpClientHandler clientHandler = new HttpClientHandler();
            clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };

            HttpClient client = new HttpClient(clientHandler);
            var httpContent = param.ObjectToStringContent();
            if (httpContent != null)
            {
                var response = await client.PostAsync(requestUri: url, content: httpContent);
                await LoadingPopup.Instance.Hide();
                PublishResponse(response);
            }
            await LoadingPopup.Instance.Hide();
        }
        private async void PublishResponse(HttpResponseMessage response)
        {
            if (response == null)
            {
                await MessagePopup.Instance.Show("Check Internet");
            }
            else if (response.IsSuccessStatusCode)
            {
                //Database database = new Database();
                //dataModel.sellLink = response.Result;
                //database.Update(dataModel);
                await MessagePopup.Instance.Show(("Publish Successfully"),
                    closeCommand: new Command(async () =>
                    {
                        await Navigation.NavigateAsync($"/{ManagerPage.HomePage}", animated: false);
                    }));
            }
            else
            {
                await MessagePopup.Instance.Show("Publish Fail");
            }
        }
        #endregion
    }
}
