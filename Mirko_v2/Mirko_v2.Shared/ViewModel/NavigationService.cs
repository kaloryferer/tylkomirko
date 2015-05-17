﻿using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Mirko_v2.ViewModel
{
    public class NavigationService : INavigationService, IDisposable
    {
#if WINDOWS_PHONE_APP
        public NavigationService()
        {
            Windows.Phone.UI.Input.HardwareButtons.BackPressed += HardwareButtons_BackPressed;
        }
        private void HardwareButtons_BackPressed(object sender, Windows.Phone.UI.Input.BackPressedEventArgs e)
        {
            if (CanGoBack())
            {
                if (CurrentPageKey == "HashtagFlipPage")
                {
                    var VM = SimpleIoc.Default.GetInstance<NotificationsViewModel>();
                    if (VM.CurrentHashtagNotifications.Count == 0)
                    {
                        var currentFrame = Window.Current.Content as Frame;
                        var backStack = currentFrame.BackStack;

                        var entry = backStack.FirstOrDefault(x => x.SourcePageType == typeof(HashtagNotificationsPage));
                        if(entry != null)
                            backStack.Remove(entry);
                    }
                }

                GoBack();
                e.Handled = true;
            }
        }
#endif

        private readonly Dictionary<String, Type> _framesDictionary = new Dictionary<string, Type>();
        public object CurrentData { get; set; }

        public void RegisterPage(string key, Type type)
        {
            _framesDictionary.Add(key, type);
        }

        public void NavigateTo(string key)
        {
            var currentFrame = Window.Current.Content as Frame;
            var nextFrameType = _framesDictionary[key];
            if (currentFrame != null && (currentFrame.CurrentSourcePageType == null || currentFrame.CurrentSourcePageType != nextFrameType))
            {
                CurrentData = null;
                CurrentPageKey = key;

                currentFrame.Navigate(nextFrameType);
            }
        }

        public void NavigateTo(string key, object data)
        {
            var currentFrame = Window.Current.Content as Frame;
            var nextFrameType = _framesDictionary[key];
            if (currentFrame != null && (currentFrame.CurrentSourcePageType == null || currentFrame.CurrentSourcePageType != nextFrameType))
            {
                CurrentData = data;
                CurrentPageKey = key;

                currentFrame.Navigate(nextFrameType, data);
            }
        }

        public bool CanGoBack()
        {
            var currentFrame = Window.Current.Content as Frame;
            return currentFrame != null && currentFrame.CanGoBack;
        }

        public void GoBack()
        {
            if (!CanGoBack()) return;
            var frame = Window.Current.Content as Frame;
            if (frame != null)
            {
                frame.GoBack();

                CurrentPageKey = _framesDictionary.Single(x => x.Value == frame.CurrentSourcePageType).Key;
            }
        }

        public void Dispose()
        {
#if WINDOWS_PHONE_APP
            Windows.Phone.UI.Input.HardwareButtons.BackPressed -= HardwareButtons_BackPressed;
#endif
        }

        private string _currentPageKey = null;
        public string CurrentPageKey
        {
            get { return _currentPageKey; }
            set { _currentPageKey = value; }
        }
    }
}
