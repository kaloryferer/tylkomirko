﻿using Mirko_v2.Controls;
using Mirko_v2.Utils;
using Mirko_v2.ViewModel;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Mirko_v2.Pages
{
    public sealed partial class PivotPage : UserControl, IHaveAppBar
    {
        public ItemsPresenter ItemsPresenter;

        private Storyboard ShowPivotContent;

        private bool HasEntryAnimationPlayed = false;

        public PivotPage()
        {
            this.InitializeComponent();
        }

        private void MainPivot_Loaded(object sender, RoutedEventArgs e)
        {
            if (ItemsPresenter == null)
            {
                ItemsPresenter = MainPivot.GetDescendant<ItemsPresenter>();
                ShowPivotContent = ItemsPresenter.Resources["FadeIn"] as Storyboard;
            }

            if (!HasEntryAnimationPlayed)
                ItemsPresenter.Opacity = 0;

            /*
            if (pivot.SelectedIndex == 0)
                App.MainViewModel.ApiAddNewEntries();
            else if (pivot.SelectedIndex == 1 && PivotHeader.Opacity != 0)
                ShowHotPopup();
             * */
        }

        private void PivotPageGrid_Loaded(object sender, RoutedEventArgs e)
        {
            if (!HasEntryAnimationPlayed)
            {
                ShowPivotContent.Begin();
                HasEntryAnimationPlayed = true;
            }
        }

        private void ListView_ScrollingDown(object sender, EventArgs e)
        {
            AppBar.Hide();

            /*
            var CurrentPage = MainPivot.SelectedIndex;
            if (CurrentPage == 0)
                HideNewEntriesPopup();
            else if (CurrentPage == 1)
                HideHotPopup();
             * */
        }

        private void ListView_ScrollingUp(object sender, EventArgs e)
        {
            AppBar.Show();
            /*
            if (CurrentPage == 0 && App.MainViewModel.MirkoNewEntries.Count > 0)
                ShowNewEntriesPopup();
            else if (CurrentPage == 1)
                ShowHotPopup();
             * */
        }

        private void ListView_Loaded(object sender, RoutedEventArgs e)
        {
            var VM = this.DataContext as MainViewModel;
            var lv = sender as ListView;

            ObservableCollectionEx<EntryViewModel> items;
            if ((string)lv.Tag == "LV0")
                items = VM.MirkoEntries;
            else
                return;

            var idx = VM.IndexToScrollTo;
            if (idx != -1 && items.Count - 1 >= idx)
            {
                lv.ScrollIntoView(items[idx], ScrollIntoViewAlignment.Leading);
                VM.IndexToScrollTo = -1;
            }
        }

        #region AppBar
        private CommandBar AppBar = null;

        public CommandBar CreateCommandBar()
        {
            var c = new CommandBar() { IsOpen = true };
            var up = new AppBarButton()
            {
                Icon = new SymbolIcon(Symbol.Up),
                Label = "w górę",
            };
            up.Click += ScrollUpButton_Click;

            var add = new AppBarButton()
            {
                Icon = new SymbolIcon(Symbol.Add),
                Label = "nowy",
            };

            c.PrimaryCommands.Add(add);
            c.PrimaryCommands.Add(up);
            AppBar = c;

            return c;
        }

        private void ScrollUpButton_Click(object sender, RoutedEventArgs e)
        {
            ScrollViewer sv = null;
            var index = MainPivot.SelectedIndex;

            if (index == 0)
                sv = MirkoListView.GetDescendant<ScrollViewer>();
            else if (index == 1)
                sv = HotListView.GetDescendant<ScrollViewer>();

            if(sv != null)
                sv.ChangeView(null, 0.0, null);
        }
        #endregion
    }
}