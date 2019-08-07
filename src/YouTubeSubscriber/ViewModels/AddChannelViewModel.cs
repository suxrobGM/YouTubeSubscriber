﻿using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Prism.Commands;
using Prism.Mvvm;
using YouTubeSubscriber.Data;
using YouTubeSubscriber.Models;
using YouTubeSubscriber.Services;

namespace YouTubeSubscriber.ViewModels
{
    public class AddChannelViewModel : BindableBase
    {
        private string _urlField;
        private string _statusText;
        private bool _isBusy;
        private bool _channelVerified;
        private readonly ApplicationDbContext _context;
        private Channel _channel;

        public ObservableCollection<Channel> Channels { get; set; }
        public bool IsBusy { get => _isBusy; set { SetProperty(ref _isBusy, value); } }
        public string StatusText { get => _statusText; set { SetProperty(ref _statusText, value); } }
        public string UrlField
        {
            get => _urlField;
            set
            {
                SetProperty(ref _urlField, value);
                VerifyChannelCommand.RaiseCanExecuteChanged();
            }
        }        
        

        public DelegateCommand VerifyChannelCommand { get; }
        public DelegateCommand AddChannelCommand { get; }      

        public AddChannelViewModel(ApplicationDbContext context)
        {
            _context = context;
            StatusText = "";

            VerifyChannelCommand = new DelegateCommand(() =>
            {
                Task.Run(() =>
                {
                    IsBusy = true;
                    StatusText += "Verifying channel...\n";

                    using (var automatization = new Automatization(true))
                    {
                        _channel = new Channel()
                        {
                            Url = UrlField,
                        };

                        if (automatization.VerifyChannel(ref _channel))
                        {
                            StatusText += "Channel verified now you can add to database!\n";
                            StatusText += _channel.ToString() + "\n";
                            _channelVerified = true;
                            IsBusy = false;
                            AddChannelCommand.RaiseCanExecuteChanged();
                        }
                    }                    
                });
                
            }, CanExecuteVerifyCommand);

            AddChannelCommand = new DelegateCommand(() =>
            {
                UrlField = "";
                _channelVerified = false;
                _context.Channels.Add(_channel);

                try
                {
                    _context.SaveChanges();
                    Channels.Add(_channel);
                }
                catch (DbUpdateException)
                {
                    StatusText += "ERROR: Channel already exists in database\n";
                    return;
                }

                StatusText += $"Added channel to database \n{_channel}";
                AddChannelCommand.RaiseCanExecuteChanged();

            }, CanExecuteAddCommand);
        }

        private bool CanExecuteVerifyCommand() => !string.IsNullOrWhiteSpace(UrlField);
        private bool CanExecuteAddCommand() => _channelVerified;
    }
}
