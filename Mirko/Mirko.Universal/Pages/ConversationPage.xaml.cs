﻿using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Messaging;
using Mirko.Utils;
using Mirko.ViewModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using WykopSDK.API.Models;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Mirko.Pages
{
    public class PMTemplateSelector : DataTemplateSelector
    {
        public DataTemplate ReceivedTemplate { get; set; }
        public DataTemplate SentTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item)
        {
            var pm = item as PMViewModel;

            if (pm.Data.Direction == MessageDirection.Received)
                return ReceivedTemplate;
            else
                return SentTemplate;
        }
    }

    public sealed partial class ConversationPage : Page, IReceiveRTBClicks
    {
        private MessagesViewModel VM
        {
            get { return DataContext as MessagesViewModel; }
        }

        public ConversationPage()
        {
            this.InitializeComponent();

            this.Loaded += (s, args) =>
            {
                InputPane.GetForCurrentView().Showing += InputPane_Showing;
                InputPane.GetForCurrentView().Hiding += InputPane_Hiding;
            };

            this.Unloaded += (s, args) =>
            {
                InputPane.GetForCurrentView().Showing -= InputPane_Showing;
                InputPane.GetForCurrentView().Hiding -= InputPane_Hiding;
            };

            this.ListView.Loaded += async (s, args) =>
            {
                var items = ListView.ItemsSource as ObservableCollectionEx<PMViewModel>;
                if (items != null)
                {
                    if (items.Count > 0)
                    {
                        ListView.ScrollIntoView(items.Last(), ScrollIntoViewAlignment.Leading);
                        await Task.Delay(500);
                        ListView.ScrollIntoView(items.Last(), ScrollIntoViewAlignment.Leading);
                    }
                    else // happens sometimes with push notifications
                        items.CollectionChanged += Messages_CollectionChanged;
                }
            };

            Messenger.Default.Register<NotificationMessage>(this, ReadMessage);
        }

        private void InputPane_Showing(InputPane sender, InputPaneVisibilityEventArgs args)
        {
            args.EnsuredFocusedElementInView = true;
            CommandBarTransform.Y = -args.OccludedRect.Height;
            ListViewTransform.Y = -args.OccludedRect.Height;
        }

        private void InputPane_Hiding(InputPane sender, InputPaneVisibilityEventArgs args)
        {
            args.EnsuredFocusedElementInView = true;
            CommandBarTransform.Y = 0;
            ListViewTransform.Y = 0;
        }

        private void Messages_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                var col = sender as ObservableCollectionEx<PMViewModel>;
                ListView.ScrollIntoView(col.Last(), ScrollIntoViewAlignment.Leading);
                col.CollectionChanged -= Messages_CollectionChanged;
            }
        }

        private void ReadMessage(NotificationMessage obj)
        {
            if (obj.Notification == "PM-Success")
            {
                TextBox.Text = "";
            }
            else if (obj.Notification == "PM-Fail")
            {
                SendButton.IsEnabled = true;
            }
        }

        private void LennyChooser_LennySelected(object sender, StringEventArgs e)
        {
            var txt = e.String + " ";
            this.TextBox.Text += txt;
            this.TextBox.SelectionStart = this.TextBox.Text.Length;

            LennyFlyout.Hide();
        }

        private void AttachmentSymbol_OpenFlyout(object sender, RoutedEventArgs e)
        {
            var mf = Resources["DeleteAttachmentFlyout"] as MenuFlyout;
            mf.ShowAt(AttachmentSymbol);
        }

        private void JumpToBottom()
        {
            var sv = ListView.GetDescendant<ScrollViewer>();
            if (sv != null)
            {
                sv.UpdateLayout();
                if (sv != null)
                    sv.ChangeView(null, sv.ScrollableHeight, null, true);
                sv.UpdateLayout();
            }
        }

        private void ContentRoot_Loaded(object sender, RoutedEventArgs e)
        {
            var height = SimpleIoc.Default.GetInstance<MainViewModel>().ListViewHeaderHeight + 49; // adjust for header
            //ContentRoot.Margin = new Thickness(10, -height, 10, 0);

            var header = ListView.Header as FrameworkElement;
            header.Height = height;
        }

        private void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            VM.CurrentConversation?.NewEntry.RemoveAttachment();
        }

        public void HashtagTapped(string tag, TextBlock tb)
        {
            if (string.IsNullOrEmpty(tag) || tb == null) return;

            var mf = Resources["HashtagFlyout"] as MenuFlyout;
            InjectedRTBHelper.PrepareHashtagFlyout(ref mf, tag);
            VM.CurrentConversation.TappedHashtag = tag;

            mf.ShowAt(tb);
        }

        public void ProfileTapped(string username)
        {
            if (string.IsNullOrEmpty(username)) return;
            InjectedRTBHelper.GoToProfilePage(username);
        }
    }
}
