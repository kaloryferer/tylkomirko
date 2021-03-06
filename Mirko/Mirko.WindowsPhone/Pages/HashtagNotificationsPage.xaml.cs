﻿using GalaSoft.MvvmLight.Ioc;
using Mirko.ViewModel;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Mirko.Pages
{
    public sealed partial class HashtagNotificationsPage : Page
    {
        public HashtagNotificationsPage()
        {
            this.InitializeComponent();
        }

        private void DeleteSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            var VM = this.DataContext as NotificationsViewModel;
            var selectedItems = ListView.SelectedItems.Cast<NotificationViewModel>().Select(x => x.Data.ID);

            foreach (var item in selectedItems)
                VM.DeleteHashtagNotification.Execute(item);

            AppBar.ClosedDisplayMode = AppBarClosedDisplayMode.Minimal;
            DeleteSelectedButton.IsEnabled = false;
        }

        private void ListView_SelectionModeChanged(object sender, RoutedEventArgs e)
        {
            if(ListView.SelectionMode == ListViewSelectionMode.Multiple)
            {
                AppBar.ClosedDisplayMode = AppBarClosedDisplayMode.Compact;
                DeleteSelectedButton.IsEnabled = true;
            }
            else
            {
                AppBar.ClosedDisplayMode = AppBarClosedDisplayMode.Minimal;
                DeleteSelectedButton.IsEnabled = false;
            }
        }

        private void Grid_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            if (ListView.SelectionMode == ListViewSelectionMode.Multiple)
                return;

            var grid = sender as Grid;
            var item = grid.DataContext as NotificationViewModel;
            var VM = this.DataContext as NotificationsViewModel;

            VM.SelectedHashtagNotification = item;
            VM.GoToFlipPage.Execute(null);
        }

        private void ListView_Loaded(object sender, RoutedEventArgs e)
        {
            var height = SimpleIoc.Default.GetInstance<MainViewModel>().ListViewHeaderHeight + 49; // adjust for header
            // QKit bug strikes again
            this.Margin = new Thickness(0, -height, 0, 0);
            Header.Margin = new Thickness(0, height, 0, 0);
        }
    }
}
