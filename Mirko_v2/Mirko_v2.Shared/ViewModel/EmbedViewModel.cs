﻿using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Views;
using Mirko_v2.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Windows.UI.Xaml.Media.Imaging;
using WykopAPI.Models;

namespace Mirko_v2.ViewModel
{
    public class EmbedViewModel : ViewModelBase
    {
        public Embed EmbedData { get; set; }

        private MemoryStream _imageStream = null;
        public MemoryStream ImageStream
        {
            get { return _imageStream; }
            set
            {
                if (_imageStream != value)
                {
                    if (_imageStream != null)
                        _imageStream.Dispose();

                    _imageStream = value;
                }
            }
        }
        private BitmapImage _bitmap = null;
        public BitmapImage Bitmap
        {
            get { return _bitmap; }
            set { Set(() => Bitmap, ref _bitmap, value); }
        }

        private string _mediaElementSrc = null;
        public string MediaElementSrc
        {
            get { return _mediaElementSrc; }
            set { Set(() => MediaElementSrc, ref _mediaElementSrc, value); }
        }

        public EmbedViewModel(Embed e)
        {
            EmbedData = e;
        }

        public void GoToEmbedPage()
        {
            Messenger.Default.Send<EmbedViewModel>(this, "Embed UserControl");
            SimpleIoc.Default.GetInstance<INavigationService>().NavigateTo("EmbedPage");
        }

        private RelayCommand _loadImageCommand = null;
        public RelayCommand LoadImageCommand
        {
            get { return _loadImageCommand ?? (_loadImageCommand = new RelayCommand(ExecuteLoadImageCommand)); }
        }

        private async void ExecuteLoadImageCommand()
        {
            using (var stream = await App.ApiService.httpClient.GetStreamAsync(new Uri(EmbedData.URL)))
            {
                ImageStream = new MemoryStream();
                await stream.CopyToAsync(ImageStream);
                ImageStream.Position = 0;

                var sr = ImageStream.AsRandomAccessStream();
                Bitmap = new BitmapImage();
                Bitmap.SetSource(sr);
            }
        }

        private RelayCommand _openEmbedCommand = null;
        public RelayCommand OpenEmbedCommand
        {
            get { return _openEmbedCommand ?? (_openEmbedCommand = new RelayCommand(ExecuteOpenEmbedCommand)); }
        }

        private async void ExecuteOpenEmbedCommand()
        {
            var url = EmbedData.URL;
            if (url.EndsWith(".jpg") || url.EndsWith(".jpeg") || url.EndsWith(".png") || (url.Contains("imgwykop.pl") && !url.EndsWith("gif")))
            {
                GoToEmbedPage();
            }
            else if (url.EndsWith(".gif"))
            {
                await StatusBarManager.ShowTextAndProgress("Konwertuje GIF...");
                var mp4 = await Gfycat.Gfycat.GIFtoMP4(EmbedData.URL);

                if (!string.IsNullOrEmpty(mp4))
                    MediaElementSrc = mp4;
                else
                    await StatusBarManager.ShowText("Coś poszło nie tak...");
            }
            else if (url.Contains("gfycat.com"))
            {
                await StatusBarManager.ShowTextAndProgress("Otwieram GFY...");
                var mp4 = await Gfycat.Gfycat.GFYgetURL(url);

                if (!string.IsNullOrEmpty(mp4))
                    MediaElementSrc = mp4;
                else
                    await StatusBarManager.ShowText("Coś poszło nie tak...");
            }
        }
    }
}