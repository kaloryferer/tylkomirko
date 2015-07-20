﻿using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Text;
using WykopAPI.Models;
using System.Linq;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Views;
using GalaSoft.MvvmLight.Ioc;
using Windows.Storage;
using GalaSoft.MvvmLight.Threading;

namespace Mirko_v2.ViewModel
{
    public class MessagesViewModel : ViewModelBase
    {
        private NavigationService NavService = null;

        public MessagesViewModel(NavigationService nav)
        {
            NavService = nav;

            Messenger.Default.Register<NotificationMessage>(this, ReadMessage);
            Messenger.Default.Register<NotificationMessage<string>>(this, ReadMessage);
            Messenger.Default.Register<NotificationMessage<Profile>>(this, ReadMessage);
        }

        private void ReadMessage(NotificationMessage obj)
        {
            if(obj.Notification == "Update")
            {
                var currentPage = NavService.CurrentPageKey;
                if (currentPage == "ConversationPage" && CurrentConversation != null)
                    CurrentConversation.UpdateMessagesCommand.Execute(null);
            }
            else if(obj.Notification == "Sort-Save")
            {
                SortConversationList();
                SaveCommand.Execute(null);
            } 
        }

        private async void ReadMessage(NotificationMessage<string> obj)
        {
            if(obj.Notification == "Remove")
            {
                var username = obj.Content;
                var conv = ConversationsList.SingleOrDefault(x => x.Data.AuthorName == username);
                if (conv != null)
                    ConversationsList.Remove(conv);

                SaveCommand.Execute(null);
            } 
            else if(obj.Notification == "Go to")
            {
                // there is a large chance this piece of code will run very shortly after the app is started.
                if(ConversationsList.Count == 0)
                {
                    var tmp = await App.ApiService.getConversations();
                    if (tmp == null) return;
                    foreach (var item in tmp)
                        ConversationsList.Add(new ConversationViewModel(item));
                }

                var conversation = ConversationsList.SingleOrDefault(x => x.Data.AuthorName == obj.Content);
                if (conversation != null)
                {
                    CurrentConversation = conversation;
                    GoToConversationPageCommand.Execute(null);
                }
            }
        }

        private void ReadMessage(NotificationMessage<Profile> obj)
        {
            if(obj.Notification == "Go to")
            {
                var p = obj.Content;

                var conv = new Conversation()
                {
                    AuthorName = p.Login,
                    AuthorAvatarURL = p.AvatarURL,
                    AuthorGroup = p.Group,
                    AuthorSex = p.Sex,
                };

                var convVM = new ConversationViewModel(conv);
                CurrentConversation = convVM;
                GoToConversationPageCommand.Execute(null);
            }
        }

        private ObservableCollectionEx<ConversationViewModel> _conversationsList = null;
        public ObservableCollectionEx<ConversationViewModel> ConversationsList
        {
            get { return _conversationsList ?? (_conversationsList = new ObservableCollectionEx<ConversationViewModel>()); }
        }

        private ConversationViewModel _currentConversation = null;
        public ConversationViewModel CurrentConversation
        {
            get { return _currentConversation; }
            set { Set(() => CurrentConversation, ref _currentConversation, value); }
        }

        private NewEntry _newMessage = null;
        public NewEntry NewMessage
        {
            get { return _newMessage ?? (_newMessage = new NewEntry()); }
            set { Set(() => NewMessage, ref _newMessage, value); }
        }

        private RelayCommand _goToConversationPageCommand = null;
        public RelayCommand GoToConversationPageCommand
        {
            get { return _goToConversationPageCommand ?? (_goToConversationPageCommand = new RelayCommand(ExecuteGoToConversationPage)); }
        }

        private void ExecuteGoToConversationPage()
        {
            if (CurrentConversation.Messages.Count == 0)
                CurrentConversation.UpdateMessagesCommand.Execute(null);

            NavService.NavigateTo("ConversationPage");
        }

        private RelayCommand _saveCommand = null;
        public RelayCommand SaveCommand
        {
            get { return _saveCommand ?? (_saveCommand = new RelayCommand(ExecuteSaveCommand)); }
        }

        private async void ExecuteSaveCommand()
        {
            var list = new List<Conversation>(ConversationsList.Count);
            foreach(var conv in ConversationsList)
            {
                Conversation tmp = conv.Data;
                if (conv.Messages != null)
                {
                    var newMsgs = new List<PM>();
                    foreach(var msg in conv.Messages)
                    {
                        var tmpMsg = msg.Data;
                        if(msg.EmbedVM != null)
                            tmpMsg.Embed = msg.EmbedVM.EmbedData;

                        newMsgs.Add(tmpMsg);
                    }

                    tmp.Messages = newMsgs;
                }
                list.Add(tmp);
            }

            await App.ApiService.LocalStorage.SaveConversations(list);
        }

        private void SortConversationList()
        {
            var ordered = this.ConversationsList.OrderByDescending(x => x.Data.LastUpdate).ToList();

            for (int i = 0; i < ordered.Count; i++)
            {
                var item = ordered[i];
                var newIndex = i;
                var oldIndex = 0;

                foreach (var tmp in ConversationsList)
                {
                    if (tmp.Data.AuthorName == item.Data.AuthorName)
                        break;
                    else
                        oldIndex++;
                }

                if (oldIndex != newIndex)
                    this.ConversationsList.Move(oldIndex, newIndex);
            }
        }
    }
}
