﻿using WykopSDK.API.Models;

namespace Mirko.ViewModel
{
    public class CommentViewModel : EntryBaseViewModel
    {
        public EntryComment Data { get; set; }
        public string RootEntryAuthor { get; set; }

        public CommentViewModel()
        {
        }

        public CommentViewModel(EntryComment c) : base(c)
        {
            Data = c;
            c = null;
        }
    }
}
