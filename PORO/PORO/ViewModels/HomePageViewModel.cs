using Newtonsoft.Json;
using Plugin.Media;
using Plugin.Media.Abstractions;
using PORO.Enums;
using PORO.Models;
using PORO.Services;
using PORO.Services.Database;
using PORO.Untilities;
using PORO.ViewModels.Base;
using PORO.Views.Popups;
using Prism.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;
using ZXing.Mobile;
using ZXing.Net.Mobile.Forms;

namespace PORO.ViewModels
{
    public class HomePageViewModel : BaseViewModel
    {
        #region Properties
        private ObservableCollection<PublishModel> _publishModel;
        public ObservableCollection<PublishModel> PublishModels
        {
            get => _publishModel;
            set => SetProperty(ref _publishModel, value);
        }
        private string _avatar;
        public string Avatar
        {
            get => _avatar;
            set => SetProperty(ref _avatar, value);
        }
        private DatabaseModel dataModel = new DatabaseModel();
        private UserModel userModel = new UserModel();
        IScanService scanService = DependencyService.Get<IScanService>();
        #endregion

        #region Contructors
        public HomePageViewModel(INavigationService navigationService) : base(navigationService)
        {
            PublishModels = new ObservableCollection<PublishModel>();
            ItemSelectedCommand = new Command(ListSelectedItem);
            AddCommand = new Command(TakePhoto);
            LogoutCommand = new Command(ExcuteLogout);
        }
        #endregion

        #region Navigation
        //public override void OnNavigatedNewTo(INavigationParameters parameters)
        //{
        //    if (parameters != null)
        //    {
        //        if (parameters.ContainsKey(ParamKeys.UserLogin.ToString()))
        //        {
        //            userModel = (UserModel)parameters[ParamKeys.UserLogin.ToString()];
        //            Preferences.Set("userId", userModel.Id);
        //            Avatar = userModel.Avatar;
        //        }
        //    }
        //    GetListTopic();
        //}
        public override void OnNavigatedTo(INavigationParameters parameters)
        {
            string userId = Preferences.Get("userId", null);
            if(userId != null)
            {
                Database database = new Database();
                var userModel = database.Get(userId);
                if(userModel != null)
                {
                    Avatar = userModel.Avatar;
                }
            }
            GetListTopic();
        }
        #endregion

        #region GetTopic
        public async void GetListTopic()
        {
            await LoadingPopup.Instance.Show();
            string userID = Preferences.Get("userId", null);
            var url = ApiUrl.UploadPhoto();
            HttpClientHandler clientHandler = new HttpClientHandler();
            clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };

            // Pass the handler to httpclient(from you are calling api)
            HttpClient client = new HttpClient(clientHandler);
            var response = await client.GetAsync(requestUri: url);
            if (response.IsSuccessStatusCode)
            {
                //PublishModels = new PublishModel();
                var content = await response.Content.ReadAsStringAsync();
                var list = JsonConvert.DeserializeObject<ObservableCollection<PublishModel>>(content);
                var n = list.Count();
                //UserModels = list.Result;
                for (int i = n - 1; i >= 0; i--)
                {
                    PublishModels.Add(new PublishModel
                    {
                        Id = list[i].Id,
                        User = list[i].User,
                        Description = list[i].Description,
                        Name = list[i].Name,
                        Image = list[i].Image,
                    });
                }
            }
            await LoadingPopup.Instance.Hide();
        }
        #endregion

        #region TopicSelected
        private PublishModel _listSelected { get; set; }
        public PublishModel SelectedItemList
        {
            get { return _listSelected; }
            set
            {
                _listSelected = value;
                RaisePropertyChanged();
            }
        }
        public ICommand ItemSelectedCommand { get; set; }
        public async void ListSelectedItem()
        {
            if (SelectedItemList != null)
            {
                var list = SelectedItemList;
                NavigationParameters param = new NavigationParameters
                {
                    {ParamKeys.Share.ToString(), list}
                };
                await Navigation.NavigateAsync(ManagerPage.SharePage, param);
                SelectedItemList = null;
            }
        }
        #endregion

        #region TakePhoto
        public ICommand AddCommand { get; set; }
        public async void TakePhoto()
        {
            await TakePhotoPopup.Instance.Show(cameraCommand: new Command(async () =>
            {
                var statusCamera = await Permissions.RequestAsync<Permissions.Camera>();
                if (statusCamera != PermissionStatus.Granted)
                {
                    return;
                }
                var statusStorage = await Permissions.RequestAsync<Permissions.StorageWrite>();
                if (statusStorage != PermissionStatus.Granted)
                {
                    return;
                }
                if (!CrossMedia.Current.IsCameraAvailable ||
                    !CrossMedia.Current.IsTakePhotoSupported)
                {
                    await MessagePopup.Instance.Show("No Camera");
                    return;
                }
                var image = await CrossMedia.Current.TakePhotoAsync(new StoreCameraMediaOptions
                {
                    CompressionQuality = 40,
                    CustomPhotoSize = 35,
                    PhotoSize = PhotoSize.MaxWidthHeight,
                    MaxWidthHeight = 1500,
                    DefaultCamera = CameraDevice.Rear,
                    SaveToAlbum = true
                });

                if (image == null)
                {
                    return;
                }
                var filepath = image.Path;
                if (filepath != null)
                {
                    Database database = new Database();
                    dataModel.filepath = filepath;
                }
                NavigationParameters param = new NavigationParameters
                {
                   {ParamKeys.DataModel.ToString(), dataModel}
                };

                await Navigation.NavigateAsync(ManagerPage.ReviewPage, param);
            }),
            galleryCommand: new Command(async () =>
            {
                var statusStorage = await Permissions.RequestAsync<Permissions.StorageWrite>();
                if (statusStorage != PermissionStatus.Granted)
                {
                    return;
                }
                var statusStorageRead = await Permissions.RequestAsync<Permissions.StorageRead>();
                if (statusStorageRead != PermissionStatus.Granted)
                {
                    return;
                }
                if (!CrossMedia.Current.IsPickPhotoSupported) { return; }
                var image = await CrossMedia.Current.PickPhotoAsync(new PickMediaOptions
                {
                    PhotoSize = PhotoSize.MaxWidthHeight,
                    MaxWidthHeight = 1500,
                    SaveMetaData = true,
                });

                if (image == null)
                {
                    return;
                }
                var filepath = image.Path;
                if (filepath != null)
                {
                    Database database = new Database();
                    dataModel.filepath = filepath;
                }
                NavigationParameters param = new NavigationParameters
                {
                   {ParamKeys.DataModel.ToString(), dataModel}
                };

                await Navigation.NavigateAsync(ManagerPage.ReviewPage, param);
            })
            );
        }
        #endregion

        #region Logout
        public ICommand LogoutCommand { get; set; }
        public async void ExcuteLogout()
        {
            await ConfirmPopup.Instance.Show(message: "Confirm LogOut",
                      acceptCommand: new Command(async () =>
                      {
                          Preferences.Set("email", null);
                          Preferences.Set("password", null);
                          Preferences.Set("userId", 0);
                          await Navigation.NavigateAsync(ManagerPage.LoginPage, animated: false);
                      }));
        }
        #endregion
    }
}
