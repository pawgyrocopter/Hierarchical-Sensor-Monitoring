﻿using HSMServer.Authentication;
using HSMServer.Core.Cache;
using HSMServer.Core.Model;
using HSMServer.Model.TreeViewModel;
using HSMServer.Notification.Settings;
using HSMServer.ServerConfiguration;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using User = HSMServer.Model.Authentication.User;

namespace HSMServer.Notifications
{
    public sealed class TelegramBot : IAsyncDisposable
    {
        private const string ConfigurationsError = "Invalid Bot configurations.";

        private readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly AddressBook _addressBook = new();
        private readonly ReceiverOptions _options = new()
        {
            AllowedUpdates = { }, // receive all update types
        };

        private readonly TelegramUpdateHandler _updateHandler;
        private readonly ITelegramChatsManager _chatsManager;
        private readonly IUserManager _userManager;
        private readonly ITreeValuesCache _cache;
        private readonly TelegramConfig _config;
        private readonly TreeViewModel _tree;

        private CancellationTokenSource _tokenSource = new();
        private TelegramBotClient _bot;


        internal string BotName => _config.BotName;

        private string BotToken => _config.BotToken;

        private bool IsBotRunning => _bot is not null;


        internal TelegramBot(ITelegramChatsManager chatsManager, IUserManager userManager, ITreeValuesCache cache, TreeViewModel tree, TelegramConfig config)
        {
            _chatsManager = chatsManager;

            _userManager = userManager;
            _userManager.Removed += _addressBook.RemoveAllChats;

            _config = config;
            _cache = cache;
            _tree = tree;

            _cache.ChangeProductEvent += RemoveProductEventHandler;
            _cache.ChangePolicyResultEvent += SendMessage;

            _updateHandler = new(_addressBook, _userManager, _tree, _cache, config);

            FillAddressBook();
        }

        public async ValueTask DisposeAsync()
        {
            _userManager.Removed -= _addressBook.RemoveAllChats;

            await StopBot();
        }

        internal string GetInvitationLink(User user) =>
            $"https://t.me/{BotName}?start={_addressBook.BuildInvitationToken(user)}";

        internal string GetStartCommandForGroup(ProductNodeViewModel product) =>
            $"{TelegramBotCommands.Start}@{BotName} {_addressBook.BuildInvitationToken(product)}";

        internal async Task<string> GetChatLink(long chatId)
        {
            var link = await _bot.CreateChatInviteLinkAsync(new ChatId(chatId), cancellationToken: _tokenSource.Token);

            return link.InviteLink;
        }

        internal void RemoveOldInvitationTokens() => _addressBook.RemoveOldTokens();

        internal void RemoveChat(INotificatable entity, long chatId)
        {
            var removedChat = _addressBook.RemoveChat(entity, new ChatId(chatId));
            entity.UpdateEntity(_userManager, _tree);

            if (removedChat is not null)
                _cache.RemoveChat(removedChat.Id, removedChat.IsUserChat ? null : entity.Name);
        }

        internal void SendTestMessage(ChatId chatId, string message)
        {
            if (IsBotRunning)
                _bot?.SendTextMessageAsync(chatId, message, cancellationToken: _tokenSource.Token);
        }

        internal async Task<string> StartBot()
        {
            if (IsBotRunning)
            {
                var message = await StopBot();
                if (!string.IsNullOrEmpty(message))
                    return message;
            }

            if (!_config.IsValid)
                return ConfigurationsError;

            _tokenSource = new CancellationTokenSource();
            _bot = new TelegramBotClient(BotToken)
            {
                Timeout = new TimeSpan(0, 5, 0) // 5 minutes timeout
            };

            try
            {
                await _bot.GetMeAsync(_tokenSource.Token);
            }
            catch (ApiRequestException exc)
            {
                _bot = null;
                return $"An error ({exc.Message}) has been occurred while starting the Bot. Please check Bot configurations. The current state of the Bot is stopped.";
            }

            await _bot.SetMyCommandsAsync(TelegramBotCommands.Commands, cancellationToken: _tokenSource.Token);

            await ChatNamesSynchronization();

            _bot.StartReceiving(_updateHandler, _options, _tokenSource.Token);
            ThreadPool.QueueUserWorkItem(async _ => await MessageReceiver());

            return string.Empty;
        }

        internal async Task<string> StopBot()
        {
            _tokenSource.Cancel();

            var bot = _bot;
            _bot = null;

            if (bot is not null)
            {
                try
                {
                    await bot?.DeleteWebhookAsync();
                    await bot?.CloseAsync();
                }
                catch (Exception ex)
                {
                    return $"An error ({ex.Message}) has been occurred while stopping the Bot. The current state of the Bot is stopped. Try to restart Bot again.";
                }
            }

            return string.Empty;
        }

        // TODO: FillAddressBook should be from telegram chats manager
        private void FillAddressBook()
        {
            foreach (var user in _userManager.GetUsers())
                if (user.Notifications?.Telegram?.Chats?.Count > 0)
                    foreach (var (_, chat) in user.Notifications.Telegram.Chats)
                        _addressBook.RegisterChat(user, chat);

            foreach (var product in _tree.GetRootProducts())
                foreach (var (_, chat) in product.Notifications.Telegram.Chats)
                    _addressBook.RegisterChat(product, chat);
        }

        private void SendMessage(PolicyResult result)
        {
            try
            {
                if (IsBotRunning && _config.IsRunning)
                {
                    var uniqueChats = new ConcurrentDictionary<Guid, ChatSettings>();

                    foreach (var (entity, chats) in _addressBook.ServerBook)
                    {
                        foreach (var (_, chat) in chats)
                            uniqueChats.TryAdd(chat.SystemId, chat);
                    }

                    foreach (var (_, chat) in uniqueChats)
                        if (chat.Chat.SendMessages)
                        {
                            var isInstant = chat.Chat.MessagesAggregationTimeSec == 0;

                            foreach (var alert in result)
                                if (chat is not null && (alert.Destination?.Chats?.Contains(chat.SystemId) ?? false))
                                {
                                    if (isInstant)
                                        SendMessage(chat.ChatId, alert.ToString());
                                    else
                                        chat.MessageBuilder.AddMessage(alert);
                                }
                        }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
        }

        private async Task MessageReceiver()
        {
            while (IsBotRunning)
            {
                try
                {
                    var uniqueChats = new ConcurrentDictionary<Guid, ChatSettings>();

                    foreach (var (entity, chats) in _addressBook.ServerBook)
                    {
                        foreach (var (_, chat) in chats)
                            uniqueChats.TryAdd(chat.SystemId, chat);
                    }

                    foreach (var (_, chat) in uniqueChats)
                    {
                        try
                        {
                            var messagesDelay = chat.Chat.MessagesAggregationTimeSec;

                            if (messagesDelay > 0 && chat.MessageBuilder.ExpectedSendingTime <= DateTime.UtcNow)
                            {
                                var message = chat.MessageBuilder.GetAggregateMessage(messagesDelay);

                                SendMessage(chat.ChatId, message);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.Error($"Error getting message: {chat.Chat.Name} - {ex}");
                        }
                    }

                    if (_tokenSource.IsCancellationRequested)
                        break;

                    await Task.Delay(500, _tokenSource.Token);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex);
                }
            }
        }

        //remove after telegram settings migration
        //private void SendMarkdownMessageAsync(ChatId chat, string message) =>
        //    _bot?.SendTextMessageAsync(chat, message, ParseMode.MarkdownV2, cancellationToken: _tokenSource.Token);

        private void SendMessage(ChatId chat, string message)
        {
            if (!string.IsNullOrEmpty(message))
                _bot?.SendTextMessageAsync(chat, message, cancellationToken: _tokenSource.Token);
        }

        private void RemoveProductEventHandler(ProductModel model, ActionType transaction)
        {
            if (transaction == ActionType.Delete)
            {
                var product = _addressBook.ServerBook.Keys.FirstOrDefault(e => e.Id == model.Id);

                if (product != null)
                    _addressBook.RemoveAllChats(product);
            }
        }

        private async Task ChatNamesSynchronization()
        {
            foreach (var (entity, chats) in _addressBook.ServerBook)
            {
                foreach (var (chatId, chatSetting) in chats)
                {
                    try
                    {
                        var chat = await _bot?.GetChatAsync(chatId, _tokenSource.Token);
                        var chatName = chatSetting.Chat.IsUserChat ? chat.Username : chat.Title;

                        if (chatSetting.Chat.Name != chatName)
                        {
                            chatSetting.Chat.Name = chatName;
                            entity.Chats[chatId].Name = chatName;

                            entity.UpdateEntity(_userManager, _tree);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Telegram chat name '{chatSetting.Chat.Name}' updating is failed - {ex}");
                    }
                }
            }
        }
    }
}
