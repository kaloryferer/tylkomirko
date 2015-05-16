﻿using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WykopAPI.Models;
using System.Linq;
using System.Threading;
using GalaSoft.MvvmLight.Threading;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Views;
using Mirko_v2.Utils;
using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.Storage;

namespace Mirko_v2.ViewModel
{
    public class HashtagInfoContainer : INotifyPropertyChanged
    {
        public string Name { get; set; }

        private uint _count;
        public uint Count
        {
            get { return _count; }
            set 
            { 
                _count = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) 
                handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class NotificationsViewModel : ViewModelBase
    {
        private Timer Timer = null;

        public NotificationsViewModel()
        {
            Timer = new Timer(TimerCallback, null, 100, 60*1000);
            _updateHashtagDic = new SemaphoreSlim(1);
        }

        private async void TimerCallback(object state)
        {
            await CheckHashtagNotifications();
            await CheckNotifications();
        }

        #region AppHeader commands
        private RelayCommand _logoTappedCommand = null;
        public RelayCommand LogoTappedCommand
        {
            get { return _logoTappedCommand ?? (_logoTappedCommand = new RelayCommand(ExecuteCommandLogoTapped)); }
        }

        private void ExecuteCommandLogoTapped()
        {
            var navService = SimpleIoc.Default.GetInstance<INavigationService>();
            navService.NavigateTo("MainPage");
        }

        private RelayCommand _hashtagTappedCommand = null;
        public RelayCommand HashtagTappedCommand
        {
            get { return _hashtagTappedCommand ?? (_hashtagTappedCommand = new RelayCommand(ExecuteHashtagTappedCommand)); }
        }

        private async void ExecuteHashtagTappedCommand()
        {
            var navService = SimpleIoc.Default.GetInstance<INavigationService>();

            if (HashtagsCollection.Count == 0)
                await UpdateHashtagDictionary();

            if (HashtagNotificationsCount == 0)
            {
                navService.NavigateTo("HashtagSelectionPage");
            }
            else if (HashtagNotificationsCount == 1)
            {
                NotificationViewModel n = null;
                string hashtag = null;

                foreach (var tmp in HashtagsDictionary)
                {
                    var collection = tmp.Value;
                    if (collection.Count == 1)
                    {
                        n = collection[0];
                        hashtag = tmp.Key;
                    }
                }

                // FIXME
                //var json = EntryNavigationParameterExtensions.fromNotification(n, hashtag);
                //NavigateTo(this, new PageNavigationEventArgs(typeof(FullscreenEntry), json));
            }
            else if (HashtagNotificationsCount > 1)
            {
                // check if all notifications relate to the same hashtag

                int nonZeroesFound = 0;
                string hashtag = null;
                foreach (var item in HashtagsCollection)
                {
                    if (item.Count != 0)
                    {
                        nonZeroesFound++;
                        hashtag = item.Name;

                        if (nonZeroesFound >= 2)
                            break;
                    }
                }

                if (nonZeroesFound <= 1) // all notifications belong to one tag
                {
                    //NavigateTo(this, new PageNavigationEventArgs(typeof(HashtagNotificationsPage), hashtag));
                    // FIXME
                }
                else
                {
                    navService.NavigateTo("HashtagSelectionPage");
                }

            }
        }

        private RelayCommand _atTappedCommand = null;
        public RelayCommand AtTappedCommand
        {
            get { return _atTappedCommand ?? (_atTappedCommand = new RelayCommand(ExecuteAtTappedCommand)); }
        }

        private void ExecuteAtTappedCommand()
        {
            var navService = SimpleIoc.Default.GetInstance<INavigationService>();
            navService.NavigateTo("AtNotificationsPage");
        }

        private RelayCommand _pmTappedCommand = null;
        public RelayCommand PMTappedCommand
        {
            get { return _pmTappedCommand ?? (_pmTappedCommand = new RelayCommand(ExecutePMTappedCommand)); }
        }

        private void ExecutePMTappedCommand()
        {
            var navService = SimpleIoc.Default.GetInstance<INavigationService>();

            if (PMNotifications.Count == 1)
            {
                var conversation = PMNotifications.First();
                /*
                var param = new PMNavigationParameter()
                {
                    UserName = conversation.author,
                    Sex = conversation.author_sex,
                    Group = conversation.author_group,
                };

                NavigateTo(this, new PageNavigationEventArgs(typeof(PMPage), param.toString()));*/
            }
            else
            {
                navService.NavigateTo("ConversationsPage");
            }
        }
        #endregion

        #region Hashtag
        private SemaphoreSlim _updateHashtagDic = null;

        private uint NewestHashtagNotificationID;

        private uint _hashtagNotificationsCount;
        public uint HashtagNotificationsCount
        {
            get { return _hashtagNotificationsCount; }
            set { Set(() => HashtagNotificationsCount, ref _hashtagNotificationsCount, value); }
        }

        private HashtagInfoContainer _currentHashtag = null;
        public HashtagInfoContainer CurrentHashtag
        {
            get { return _currentHashtag; }
            set { Set(() => CurrentHashtag, ref _currentHashtag, value); }
        }

        private ObservableCollectionEx<NotificationViewModel> _currentHashtagNotifications = null;
        public ObservableCollectionEx<NotificationViewModel> CurrentHashtagNotifications
        {
            get { return _currentHashtagNotifications; }
            set { Set(() => CurrentHashtagNotifications, ref _currentHashtagNotifications, value); }
        }

        private NotificationViewModel _selectedHashtagNotification = null;
        public NotificationViewModel SelectedHashtagNotification
        {
            get { return _selectedHashtagNotification; }
            set { Set(() => SelectedHashtagNotification, ref _selectedHashtagNotification, value); }
        }

        private ObservableDictionary<string, ObservableCollectionEx<NotificationViewModel>> _hashtagsDictionary;
        public ObservableDictionary<string, ObservableCollectionEx<NotificationViewModel>> HashtagsDictionary
        {
            get { return _hashtagsDictionary ?? (_hashtagsDictionary = new ObservableDictionary<string,ObservableCollectionEx<NotificationViewModel>>()); }
        }

        private ObservableCollectionEx<HashtagInfoContainer> _hashtagsCollection;
        public ObservableCollectionEx<HashtagInfoContainer> HashtagsCollection
        {
            get { return _hashtagsCollection ?? (_hashtagsCollection = new ObservableCollectionEx<HashtagInfoContainer>()); }
        }

        private ObservableCollectionEx<EntryViewModel> _hashtagFlipEntries = null;
        public ObservableCollectionEx<EntryViewModel> HashtagFlipEntries
        {
            get { return _hashtagFlipEntries ?? (_hashtagFlipEntries = new ObservableCollectionEx<EntryViewModel>()); }
        }

        private EntryViewModel _hashtagFlipCurrentEntry = null;
        public EntryViewModel HashtagFlipCurrentEntry
        {
            get { return _hashtagFlipCurrentEntry; }
            set { Set(() => HashtagFlipCurrentEntry, ref _hashtagFlipCurrentEntry, value); }
        }

        private List<string> _observedHashtags;
        public List<string> ObservedHashtags // this VM is not the best for this, I know. but I can't think of a better one right now.
        {
            get { return _observedHashtags ?? (_observedHashtags = new List<string>()); }
        }

        private async Task DownloadObservedHashtags()
        {
            var folder = Windows.Storage.ApplicationData.Current.TemporaryFolder;
            var needToDownload = false;
            try
            {
                var file = await folder.GetFileAsync("ObservedTags");
                var props = await file.GetBasicPropertiesAsync();
                if (DateTime.Now - props.DateModified > new TimeSpan(12, 0, 0))
                {
                    needToDownload = true;
                }
                else
                {
                    var fileContent = await FileIO.ReadLinesAsync(file);
                    ObservedHashtags.Clear();
                    ObservedHashtags.AddRange(fileContent);
                }
            }
            catch (Exception)
            {
                needToDownload = true;
            }

            if (needToDownload)
            {
                var data = await App.ApiService.getUserObservedTags();
                if (data != null)
                {
                    ObservedHashtags.Clear();
                    ObservedHashtags.AddRange(data);
                    data = null;
                }
            }

            if(ObservedHashtags.Count > 0)
            {
                var file = await folder.CreateFileAsync("ObservedTags", CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteLinesAsync(file, ObservedHashtags);
            }
        }

        private RelayCommand<uint> _deleteHashtagNotification = null;
        public RelayCommand<uint> DeleteHashtagNotification
        {
            get { return _deleteHashtagNotification ?? (_deleteHashtagNotification = new RelayCommand<uint>(async (id) => await ExecuteDeleteHashtagNotification(id))); }
        }

        private async Task ExecuteDeleteHashtagNotification(uint ID)
        {
            try
            {
                ObservableCollectionEx<NotificationViewModel> collection = null;
                NotificationViewModel notification = null;

                foreach(var col in HashtagsDictionary.Values)
                {
                    var tmp = col.SingleOrDefault(x => x.Data.ID == ID);
                    if(tmp != null)
                    {
                        notification = tmp;
                        collection = col;
                        break;
                    }
                }

                if (notification != null && notification.Data.IsNew)
                {
                    await App.ApiService.markAsReadNotification(ID);
                    await DispatcherHelper.RunAsync(() => collection.Remove(notification));
                    await UpdateHashtagDictionary();
                }
            }
            catch (Exception) { }
        }

        private RelayCommand<string> _deleteHashtagNotifications = null;
        public RelayCommand<string> DeleteHashtagNotifications
        {
            get { return _deleteHashtagNotifications ?? (_deleteHashtagNotifications = new RelayCommand<string>(ExecuteDeleteHashtagNotifications)); }
        }

        private async void ExecuteDeleteHashtagNotifications(string hashtag)
        {
            if (!HashtagsDictionary.ContainsKey(hashtag)) return;

            var notifications = HashtagsDictionary[hashtag];
            var IDs = notifications.Select(x => x.Data.ID);

            foreach (var id in IDs)
                await App.ApiService.markAsReadNotification(id);

            HashtagsDictionary.Remove(hashtag);
            await UpdateHashtagDictionary();

            await StatusBarManager.ShowText("Powiadomienia zostały usunięte.");
        }

        private RelayCommand _deleteAllHashtagNotifications = null;
        public RelayCommand DeleteAllHashtagNotifications
        {
            get { return _deleteAllHashtagNotifications ?? (_deleteAllHashtagNotifications = new RelayCommand(ExecuteDeleteAllHashtagNotifications)); }
        }

        private async void ExecuteDeleteAllHashtagNotifications()
        {
            await App.ApiService.readHashtagNotifications();
        }

        private RelayCommand _goToHashtagNotificationsPage = null;
        public RelayCommand GoToHashtagNotificationsPage
        {
            get { return _goToHashtagNotificationsPage ?? (_goToHashtagNotificationsPage = new RelayCommand(ExecuteGoToHashtagNotificationsPage)); }
        }

        private async void ExecuteGoToHashtagNotificationsPage()
        {
            CurrentHashtagNotifications = HashtagsDictionary[CurrentHashtag.Name];
            if (CurrentHashtagNotifications.Count > 1)
            {
                SimpleIoc.Default.GetInstance<INavigationService>().NavigateTo("HashtagNotificationsPage");
            }
            else if (CurrentHashtagNotifications.Count == 1)
            {
                var mainVM = SimpleIoc.Default.GetInstance<MainViewModel>();
                await DispatcherHelper.RunAsync(() => mainVM.SelectedEntry = null);

                SimpleIoc.Default.GetInstance<INavigationService>().NavigateTo("EntryPage");

                var notification = CurrentHashtagNotifications[0].Data;

                await StatusBarManager.ShowTextAndProgress("Pobieram wpis...");
                var entry = await App.ApiService.getEntry(notification.Entry.ID);
                if (entry != null)
                {
                    var entryVM = new EntryViewModel(entry);
                    await DispatcherHelper.RunAsync(() =>
                    {
                        mainVM.OtherEntries.Add(entryVM);
                        mainVM.SelectedEntry = entryVM;
                    });

                    await StatusBarManager.HideProgress();
                    await ExecuteDeleteHashtagNotification(notification.ID);
                }
                else
                {
                    await StatusBarManager.ShowText("Nie udało się pobrać wpisu.");
                }
            }
            else
            {
                // TODO.
            }
        }

        private RelayCommand _goToFlipPage = null;
        public RelayCommand GoToFlipPage
        {
            get { return _goToFlipPage ?? (_goToFlipPage = new RelayCommand(ExecuteGoToFlipPage)); }
        }

        private async void ExecuteGoToFlipPage()
        {
            if (SelectedHashtagNotification == null) return;
            var index = CurrentHashtagNotifications.GetIndex(SelectedHashtagNotification);

            await DispatcherHelper.RunAsync(() =>
            {
                HashtagFlipEntries.Clear();
                for (int i = 0; i < CurrentHashtagNotifications.Count; i++)
                    HashtagFlipEntries.Add(null);
            });

            SimpleIoc.Default.GetInstance<INavigationService>().NavigateTo("HashtagFlipPage");

            await ExecuteHashtagFlipSelectionChanged(index);
        }

        private RelayCommand<int> _hashtagFlipSelectionChanged = null;
        public RelayCommand<int> HashtagFlipSelectionChanged
        {
            get { return _hashtagFlipSelectionChanged ?? (_hashtagFlipSelectionChanged = new RelayCommand<int>(async (i) => await ExecuteHashtagFlipSelectionChanged(i))); }
        }

        private async Task ExecuteHashtagFlipSelectionChanged(int currentIndex)
        {
            if (currentIndex == -1 || HashtagFlipEntries[currentIndex] != null) return;
            await StatusBarManager.ShowTextAndProgress("Pobieram wpis...");

            Notification notification = null;
            if (currentIndex > CurrentHashtagNotifications.Count)
                notification = CurrentHashtagNotifications.Last().Data;
            else
                notification = CurrentHashtagNotifications[currentIndex].Data;

            var entry = await App.ApiService.getEntry(notification.Entry.ID);
            if (entry != null)
            {
                var entryVM = new EntryViewModel(entry);
                await DispatcherHelper.RunAsync(() =>
                {
                    HashtagFlipEntries.Replace(currentIndex, entryVM);
                    HashtagFlipCurrentEntry = entryVM;
                });
                await StatusBarManager.HideProgress();

                await ExecuteDeleteHashtagNotification(notification.ID);
            }
            else
            {
                await StatusBarManager.ShowText("Nie udało się pobrać wpisu.");
            }
        }

        public async Task CheckHashtagNotifications()
        {
            uint pageIndex = 1;
            var notificationsList = new List<Notification>(50);

            while (true)
            {
                var notifications = await App.ApiService.getHashtagNotifications(pageIndex++);

                if (notifications == null || notifications.Count == 0)
                    break;

                var newNotifications = notifications.Where(x => x.IsNew && x.ID > this.NewestHashtagNotificationID);
                notificationsList.AddRange(newNotifications);

                if (!notifications.Last().IsNew)
                    break;
            }

            if (notificationsList.Count > 0)
                this.NewestHashtagNotificationID = notificationsList.First().ID;

            var dic = this.HashtagsDictionary;
            foreach (var item in notificationsList)
            {
                var body = item.Text;
                var index = body.IndexOf('#');
                var nextIndex = body.IndexOf(' ', index);

                var tagName = body.Substring(index, nextIndex - index);

                if (dic.ContainsKey(tagName))
                    await DispatcherHelper.RunAsync(() => dic[tagName].Add(new NotificationViewModel(item)));
                else
                    await DispatcherHelper.RunAsync(() => dic[tagName] = new ObservableCollectionEx<NotificationViewModel>() { new NotificationViewModel(item) });
            }

            await UpdateHashtagDictionary();
        }

        private async Task UpdateHashtagDictionary(bool forcedSorting = false)
        {
            await _updateHashtagDic.WaitAsync();

            try
            {
                var dic = this.HashtagsDictionary;
                var orderedNames = dic.Keys.OrderByDescending(x => dic[x].Count).ToList();

                uint count = 0;

                var tmp = new List<HashtagInfoContainer>();
                foreach (var name in orderedNames)
                {
                    var c = (uint)dic[name].Count;
                    if (c > 0)
                    {
                        count += c;
                        tmp.Add(new HashtagInfoContainer
                        {
                            Name = name,
                            Count = c,
                        });
                    }
                }

                await DispatcherHelper.RunAsync(() =>
                {
                    this.HashtagsCollection.Clear();
                    this.HashtagsCollection.AddRange(tmp);
                    this.HashtagNotificationsCount = count;

                    if (CurrentHashtag != null)
                    {
                        if (this.HashtagsDictionary.ContainsKey(this.CurrentHashtag.Name))
                            this.CurrentHashtag.Count = (uint)HashtagsDictionary[this.CurrentHashtag.Name].Count;
                        else
                            this.CurrentHashtag.Count = 0;
                    }
                });

                tmp.Clear();

                // now add observed hashtags without notifications
                if (ObservedHashtags.Count == 0)
                    await DownloadObservedHashtags();

                foreach (var tag in ObservedHashtags)
                {
                    bool itemFound = false;

                    foreach (var addedTag in this.HashtagsCollection)
                    {
                        if (addedTag.Name == tag)
                        {
                            itemFound = true;
                            break;
                        }
                    }

                    if (!itemFound)
                        tmp.Add(new HashtagInfoContainer() { Name = tag, Count = 0 });
                }

                await DispatcherHelper.RunAsync(() => this.HashtagsCollection.AddRange(tmp));
                tmp = null;

                // now sort
                if (count > 0 || forcedSorting)
                {
                    var groups = this.HashtagsCollection.GroupBy(x => x.Count);
                    int itemsToRemove = this.HashtagsCollection.Count();

                    var sortedGroups = new List<HashtagInfoContainer>();

                    foreach (var group in groups)
                    {
                        var sortedGroup = group.OrderBy(x => x.Name);
                        sortedGroups.AddRange(sortedGroup);
                    }

                    await DispatcherHelper.RunAsync(() =>
                    {
                        this.HashtagsCollection.Clear();
                        this.HashtagsCollection.AddRange(sortedGroups);
                    });

                    sortedGroups = null;
                }
            }
            finally
            {
                _updateHashtagDic.Release();
            }
        }
        #endregion

        #region At
        private uint _atNotificationsCount;
        public uint AtNotificationsCount
        {
            get { return _atNotificationsCount; }
            set { Set(() => AtNotificationsCount, ref _atNotificationsCount, value); }
        }

        private NotificationViewModel _selectedAtNotification = null;
        public NotificationViewModel SelectedAtNotification
        {
            get { return _selectedAtNotification; }
            set { Set(() => SelectedAtNotification, ref _selectedAtNotification, value); }
        }

        private IncrementalLoadingCollection<AtNotificationsSource, NotificationViewModel> _atNotifications = null;
        public IncrementalLoadingCollection<AtNotificationsSource, NotificationViewModel> AtNotifications
        {
            get { return _atNotifications ?? (_atNotifications = new IncrementalLoadingCollection<AtNotificationsSource, NotificationViewModel>()); }
        }

        private RelayCommand _goToNotification = null;
        public RelayCommand GoToNotification
        {
            get { return _goToNotification ?? (_goToNotification = new RelayCommand(ExecuteGoToNotification)); }
        }

        private async void ExecuteGoToNotification()
        {
            var notification = SelectedAtNotification.Data;
            if (notification.Type != NotificationType.EntryDirected && notification.Type != NotificationType.CommentDirected)
                return;

            var entryID = notification.Entry.ID;
            var mainVM = SimpleIoc.Default.GetInstance<MainViewModel>();
            var otherEntries = mainVM.OtherEntries;
            var entryVM = otherEntries.SingleOrDefault(x => x.Data.ID == entryID);

            if(entryVM != null)
            {
                mainVM.SelectedEntry = entryVM;
            }
            else
            {
                await StatusBarManager.ShowTextAndProgress("Pobieram wpis...");
                var entryData = await App.ApiService.getEntry(entryID);
                if(entryData == null)
                {
                    await StatusBarManager.ShowText("Nie udało się pobrać wpisu.");
                }
                else
                {
                    await StatusBarManager.HideProgress();
                    entryVM = new EntryViewModel(entryData);
                    await DispatcherHelper.RunAsync(() => otherEntries.Add(entryVM));
                    mainVM.SelectedEntry = entryVM;
                }
            }

            if (notification.Type == NotificationType.CommentDirected)
                mainVM.CommentToScrollInto = entryVM.Comments.SingleOrDefault(x => x.Data.ID == notification.Comment.CommentID);

            SimpleIoc.Default.GetInstance<INavigationService>().NavigateTo("EntryPage");
            SelectedAtNotification.MarkAsReadCommand.Execute(null);
        }

        private RelayCommand _deleteAllAtNotifications = null;
        public RelayCommand DeleteAllAtNotifications
        {
            get { return _deleteAllAtNotifications ?? (_deleteAllAtNotifications = new RelayCommand(ExecuteDeleteAllAtNotifications)); }
        }

        private async void ExecuteDeleteAllAtNotifications()
        {
            var success = await App.ApiService.readNotifications();
            if (!success) return;

            foreach (var notificationVM in AtNotifications)
                notificationVM.Data.IsNew = false;
        }
        #endregion

        #region PM
        private uint _pmNotificationsCount = 0;
        public uint PMNotificationsCount
        {
            get { return _pmNotificationsCount; }
            set { Set(() => PMNotificationsCount, ref _pmNotificationsCount, value); }
        }

        private ObservableCollectionEx<Notification> _pmNotifications = null;
        public ObservableCollectionEx<Notification> PMNotifications
        {
            get { return _pmNotifications ?? (_pmNotifications = new ObservableCollectionEx<Notification>()); }
        }       
        #endregion

        private async Task CheckNotifications()
        {
            uint pageIndex = 1;
            IEnumerable<Notification> notificationsTemp = null;
            var newNotifications = new List<Notification>();

            // download new notifications
            do
            {
                var notificationsDL = await App.ApiService.getNotifications(pageIndex++);
                if (notificationsDL == null || notificationsDL.Count == 0)
                    break;

                var supportedTypes = new NotificationType[] 
                { 
                    NotificationType.Observe, NotificationType.Unobserve, 
                    NotificationType.EntryDirected, NotificationType.CommentDirected, 
                    NotificationType.System, NotificationType.Badge, NotificationType.PM 
                };
                notificationsTemp = notificationsDL.Where(x => x.IsNew).Where(x => supportedTypes.Contains(x.Type));

                newNotifications.AddRange(notificationsTemp);

                if (notificationsTemp.Count() == 0 || !notificationsDL.Last().IsNew)
                    break;

            } while (true);

            // now parse them. first PM
            var pmVM = SimpleIoc.Default.GetInstance<MessagesViewModel>();
            var pmNotifications = newNotifications.Where(x => x.Type == NotificationType.PM);

            if (pmVM.ConversationsList.Count == 0)
            {
                var conversations = await App.ApiService.getConversations();
                if (conversations != null)
                {
                    var tmp = new List<ConversationViewModel>(conversations.Count);
                    foreach(var item in conversations)
                        tmp.Add(new ConversationViewModel(item));

                    await DispatcherHelper.RunAsync(() => pmVM.ConversationsList.AddRange(tmp));

                    tmp = null;
                    conversations = null;
                }
            }

            var tempPMnotifications = new List<Notification>();
            foreach (var item in pmNotifications)
            {
                var userName = item.AuthorName;
                //if (userName == App.NotificationsViewModel.CurrentUserName)
                //    continue;
                // FIXME?

                var conversation = pmVM.ConversationsList.First(x => x.Data.AuthorName == userName);

                if (conversation != null)
                {
                    if (conversation.Data.Status != ConversationStatus.New)
                        conversation.Data.Status = ConversationStatus.New;
                }
                else
                {
                    var conv = new Conversation();
                    conv.AuthorName = userName;
                    conv.Status = ConversationStatus.New;
                    pmVM.ConversationsList.Insert(0, new ConversationViewModel(conv));
                }

                tempPMnotifications.Add(item);
            }

            await DispatcherHelper.RunAsync(() =>
            {
                this.PMNotifications.Clear();
                this.PMNotifications.AddRange(tempPMnotifications);
                tempPMnotifications = null;
            });

            // now the rest
            var atNotifications = newNotifications.Where(x => x.Type != NotificationType.PM);
            this.AtNotificationsCount = (uint)atNotifications.Count();

            if (atNotifications.Count() > 0)
            {
                var VMs = new List<NotificationViewModel>();
                foreach (var n in atNotifications)
                    VMs.Add(new NotificationViewModel(n));

                await DispatcherHelper.RunAsync(() => this.AtNotifications.PrependRange(VMs));
                VMs = null;
            }
        }
    }
}
