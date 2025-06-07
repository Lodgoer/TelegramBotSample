using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBotSample
{
    internal class Program
    {
        static async Task Main()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var botToken = config["BotConfiguration:Token"];
            var botClient = new TelegramBotClient(botToken);
            using var cts = new CancellationTokenSource();

            await botClient.DeleteWebhookAsync(cancellationToken: cts.Token);

            await botClient.SetMyCommandsAsync(new[]
            {
                new BotCommand { Command = "start", Description = "شروع گفتگو با ربات" },
                new BotCommand { Command = "exit", Description = "خروج از گفتگو با ادمین" },
                new BotCommand { Command = "reply", Description = "پاسخ دادن به کاربر (فقط برای ادمین)" }
            });

            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>()
            };

            botClient.StartReceiving(
                updateHandler: HandleUpdateAsync,
                pollingErrorHandler: HandlePollingErrorAsync,
                receiverOptions: receiverOptions,
                cancellationToken: cts.Token
            );


            var botInfo = await botClient.GetMeAsync(cts.Token);
            Console.WriteLine($"\u2705 Bot @{botInfo.Username} is running...");

            Console.ReadLine();
            cts.Cancel();
        }

        private static async Task HandleUpdateAsync(
        ITelegramBotClient botClient,
        Update update,
        CancellationToken cancellationToken)
        {
            if (update.CallbackQuery is { } callbackQuery)
            {
                var callbackChatId = callbackQuery.Message.Chat.Id;
                var data = callbackQuery.Data;

                await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: cancellationToken);

                switch (data)
                {
                    case "contact":
                        await botClient.SendTextMessageAsync(callbackChatId, "\uD83D\uDCDE: example@example.com", cancellationToken: cancellationToken);
                        break;
                    case "about":
                        await botClient.SendTextMessageAsync(callbackChatId, "\u2139\uFE0F ربات نمونه.", cancellationToken: cancellationToken);
                        break;
                    case "chat_with_admin":
                        AdminHandler.StartChatWithAdmin(callbackChatId);
                        await botClient.SendTextMessageAsync(callbackChatId, "✉️ حالا پیامت رو بنویس تا برای ادمین ارسال کنم.", cancellationToken: cancellationToken);
                        break;
                    default:
                        await botClient.SendTextMessageAsync(callbackChatId, "❓ فرمان نامشخص.", cancellationToken: cancellationToken);
                        break;
                }

                return;
            }


            if (update.Message is not { Text: { } messageText }) return;

            var chatId = update.Message.Chat.Id;

            if (chatId == AdminHandler.AdminChatId && messageText.StartsWith("/reply"))
            {
                var parts = messageText.Split(' ', 3);
                if (parts.Length >= 3 && long.TryParse(parts[1], out long targetUserId))
                {
                    await botClient.SendTextMessageAsync(
                        chatId: targetUserId,
                        text: $"📣 پیام از ادمین:{ parts[2]}",
                        cancellationToken: cancellationToken);

                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "✅ پیام با موفقیت ارسال شد.",
                        cancellationToken: cancellationToken);
                }
                else
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "❌ فرمت اشتباه است. استفاده صحیح: /reply [UserId] [Message]",
                        cancellationToken: cancellationToken);
                }

                return;
            }

            if (AdminHandler.IsUserChattingWithAdmin(chatId))
            {
                await AdminHandler.ForwardMessageToAdmin(botClient, update.Message, cancellationToken);
                return;
            }

            switch (messageText)
            {
                case "/start":
                    var keyboard = new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("📞 تماس با ما", "contact"),
                            InlineKeyboardButton.WithCallbackData("ℹ️ درباره ما", "about")
                        },
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("💬 چت با ادمین", "chat_with_admin")
                        }
                    });

                    await botClient.SendTextMessageAsync(chatId, "خوش آمدید! لطفاً یکی از گزینه‌ها را انتخاب کنید:", replyMarkup: keyboard, cancellationToken: cancellationToken);
                    break;

                case "/exit":
                    AdminHandler.EndChatWithAdmin(chatId);
                    await botClient.SendTextMessageAsync(chatId, "👋 به امید دیدار!", cancellationToken: cancellationToken);
                    break;

                default:
                    await botClient.SendTextMessageAsync(chatId, "❓ فرمان نامعتبر است. از /start یا /exit استفاده کنید.", cancellationToken: cancellationToken);
                    break;
            }
        }

        private static Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine($"❌ Error: {exception.Message}");
            return Task.CompletedTask;
        }
    }
}
