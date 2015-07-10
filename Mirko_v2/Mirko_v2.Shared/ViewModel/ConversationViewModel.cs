﻿using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using WykopAPI.Models;
using Mirko_v2.Utils;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Views;
using System.Collections.ObjectModel;
using Windows.Storage;
using System.IO;
using Windows.Storage.Pickers;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;

namespace Mirko_v2.ViewModel
{
    public class ConversationViewModel : ViewModelBase, IFileOpenPickerContinuable
    {
        public Conversation Data { get; set; }
        private ObservableCollectionEx<PMViewModel> _messages = null;
        public ObservableCollectionEx<PMViewModel> Messages
        {
            get { return _messages ?? (_messages = new ObservableCollectionEx<PMViewModel>()); }
        }

        private NewEntry _newEntry = null;
        public NewEntry NewEntry
        {
            get { return _newEntry ?? (_newEntry = new NewEntry()); }
            set { Set(() => NewEntry, ref _newEntry, value); }
        }

        private bool _isOnline = false;
        public bool IsOnline
        {
            get { return _isOnline; }
            set { Set(() => IsOnline, ref _isOnline, value); }
        }

        public ConversationViewModel(Conversation d)
        {
            Data = d;
            if (Data.Messages != null)
            {
                foreach (var pm in Data.Messages)
                    this.Messages.Add(new PMViewModel(pm));

                ProcessMessages();
                Data.Messages = null;
            }

            d = null;
        }

        private RelayCommand _sendMessageCommand = null;
        public RelayCommand SendMessageCommand
        {
            get { return _sendMessageCommand ?? (_sendMessageCommand = new RelayCommand(ExecuteSendMessageCommand)); }
        }

        private async void ExecuteSendMessageCommand()
        {
            await StatusBarManager.ShowTextAndProgress("Wysyłam wiadomość...");
            bool success = await App.ApiService.sendPM(NewEntry, Data.AuthorName);
            if (success)
            {
                await StatusBarManager.ShowText("Wiadomość została wysłana.");
                await ExecuteUpdateMessagesCommand();
                Messenger.Default.Send<NotificationMessage>(new NotificationMessage("PM-Success"));
            }
            else
            {
                Messenger.Default.Send<NotificationMessage>(new NotificationMessage("PM-Fail"));
                await StatusBarManager.ShowText("Wiadomość nie została wysłana.");
            }
        }

        private RelayCommand _updateMessagesCommand = null;
        public RelayCommand UpdateMessagesCommand
        {
            get { return _updateMessagesCommand ?? (_updateMessagesCommand = new RelayCommand(async () => await ExecuteUpdateMessagesCommand())); }
        }

        private async Task ExecuteUpdateMessagesCommand()
        {
            DispatcherHelper.CheckBeginInvokeOnUI(() => Data.LastMessage = "Pobieram...");

            await StatusBarManager.ShowProgress();
            var pms = await App.ApiService.getPMs(Data.AuthorName);
            if (pms == null || pms.Count == 0)
            {
                DispatcherHelper.CheckBeginInvokeOnUI(() => Data.LastMessage = "");
                await StatusBarManager.HideProgress();
                return;
            }

            IEnumerable<PM> newMessages = null;
            if(this.Messages.Count > 0)
                newMessages = FilterMessages(pms);
            else
                newMessages = pms;

            await DispatcherHelper.RunAsync(() =>
            {
                foreach (var pm in newMessages)
                    this.Messages.Add(new PMViewModel(pm));

                Data.LastUpdate = this.Messages.Last().Data.Date;
                Data.LastMessage = HTMLUtils.HTMLtoTEXT(this.Messages.Last().Data.Text);
                ProcessMessages();
            });

            Messenger.Default.Send<NotificationMessage<string>>(new NotificationMessage<string>(Data.AuthorName, "Clear PM")); // clear PM notification

            await StatusBarManager.HideProgress();

            Messenger.Default.Send<NotificationMessage>(new NotificationMessage("Sort-Save"));
        }

        private RelayCommand _loadLastMessageCommand = null;
        public RelayCommand LoadLastMessageCommand
        {
            get { return _loadLastMessageCommand ?? (_loadLastMessageCommand = new RelayCommand(ExecuteLoadLastMessageCommand)); }
        }

        private async void ExecuteLoadLastMessageCommand()
        {
            if (!string.IsNullOrEmpty(Data.LastMessage)) return;

            await ExecuteUpdateMessagesCommand();

            if (this.Messages.Count > 0)
                Data.LastMessage = HTMLUtils.HTMLtoTEXT(this.Messages.Last().Data.Text);
            else
                Data.LastMessage = "";
        }

        private RelayCommand _deleteConversation = null;
        public RelayCommand DeleteConversation
        {
            get { return _deleteConversation ?? (_deleteConversation = new RelayCommand(ExecuteDeleteConversation)); }
        }

        private async void ExecuteDeleteConversation()
        {
            await StatusBarManager.ShowProgress();
            bool success = await App.ApiService.deleteConversation(Data.AuthorName);
            if(success)
            {
                Messenger.Default.Send<NotificationMessage<string>>(new NotificationMessage<string>(Data.AuthorName, "Remove"));
                await StatusBarManager.ShowText("Rozmowa została usunięta.");
            }
            else
            {
                await StatusBarManager.ShowText("Nie udało się usunąć rozmowy.");
            }
        }

        private RelayCommand _addAttachment = null;
        public RelayCommand AddAttachment
        {
            get { return _addAttachment ?? (_addAttachment = new RelayCommand(ExecuteAddAttachment)); }
        }

        private void ExecuteAddAttachment()
        {
            var navService = SimpleIoc.Default.GetInstance<INavigationService>();
            navService.NavigateTo("AddAttachmentPage", "PM");
        }

        private void ProcessMessages()
        {
            var maxIndex = this.Messages.Count - 1;
            var previousAuthor = "";
            var pms = this.Messages;

            for (int i = 0; i < this.Messages.Count; i++)
            {
                var pm = pms[i];
                var pmData = pm.Data;

                if (pmData.AuthorName == previousAuthor)
                {
                    if (pmData.Direction == MessageDirection.Received)
                    {
                        while (i < maxIndex && pms[i + 1].Data.AuthorName == pmData.AuthorName)
                            i++;
                    }
                    else
                    {
                        pms[i - 1].ShowArrow = false;
                        while (i < maxIndex && pms[i + 1].Data.AuthorName == pmData.AuthorName)
                            i++;

                        pms[i].ShowArrow = true;
                    }
                }
                else
                {
                    pms[i].ShowArrow = true;
                    previousAuthor = pmData.AuthorName;
                }

                if (pms[maxIndex].Data.Direction == MessageDirection.Sent)
                    pms[maxIndex].ShowArrow = false;
            }
        }

        private IEnumerable<PM> FilterMessages(List<PM> col)
        {
            var lastDate = this.Messages.Last().Data.Date;
            var newMsg = col.Where(x => x.Date >= lastDate).ToList();
            var ret = this.Messages.Select(x => x.Data).ToList();
            ret.AddRange(newMsg);

            // first find duplicates
            var duplicates = ret
                .GroupBy(i => i.Text)
                .Where(g => g.Count() > 1)
                .Select(g => g);

            // now group them
            foreach(var group in duplicates)
            {
                var key = group.Key;

                var firstMessage = group.First();
                var direction = firstMessage.Direction;
                var date = firstMessage.Date;

                var messagesToRemove = group.
                    Where(x => x.Direction == direction).
                    Where(x => Math.Abs(x.Date.Subtract(date).TotalSeconds) <= 120.0);

                foreach (var msg in messagesToRemove)
                    newMsg.Remove(msg);
            }

            return newMsg;
        }

        private RelayCommand _openPicker = null;
        public RelayCommand OpenPicker
        {
            get { return _openPicker ?? (_openPicker = new RelayCommand(ExecuteOpenPicker)); }
        }

        private void ExecuteOpenPicker()
        {
            var openPicker = new FileOpenPicker();
            openPicker.FileTypeFilter.Add(".jpg");
            openPicker.FileTypeFilter.Add(".jpeg");
            openPicker.FileTypeFilter.Add(".png");
            openPicker.FileTypeFilter.Add(".gif");
            openPicker.ViewMode = PickerViewMode.Thumbnail;
            openPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;

            openPicker.PickSingleFileAndContinue();
        }

        public async void ContinueFileOpenPicker(Windows.ApplicationModel.Activation.FileOpenPickerContinuationEventArgs args)
        {
            if (args.Files.Count() > 0)
            {
                StorageFile file = args.Files[0];
                var s = await file.OpenAsync(Windows.Storage.FileAccessMode.Read);
                NewEntry.FileStream = s.AsStreamForRead();
                NewEntry.FileName = file.Name;
                NewEntry.AttachmentName = file.DisplayName;

                SimpleIoc.Default.GetInstance<INavigationService>().GoBack();
            }
        }

        private RelayCommand _removeAttachment = null;
        public RelayCommand RemoveAttachment
        {
            get { return _removeAttachment ?? (_removeAttachment = new RelayCommand(() => NewEntry.RemoveAttachment())); }
        }

        private RelayCommand _acceptPressed = null;
        public RelayCommand AcceptPressed
        {
            get { return _acceptPressed ?? (_acceptPressed = new RelayCommand(ExecuteAcceptPressed)); }
        }

        private void ExecuteAcceptPressed()
        {
            SimpleIoc.Default.GetInstance<INavigationService>().GoBack();
        }

        private RelayCommand _checkIfOnline = null;
        public RelayCommand CheckIfOnline
        {
            get { return _checkIfOnline ?? (_checkIfOnline = new RelayCommand(ExecuteCheckIfOnline)); }
        }

        private async void ExecuteCheckIfOnline()
        {
            IsOnline = await App.ApiService.isUserOnline(Data.AuthorName);
        }
    }
}
