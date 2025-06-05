using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

class Program
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

        // ✅ تنظیم فرمان‌ها
        await botClient.SetMyCommandsAsync(new[]
        {
        new Telegram.Bot.Types.BotCommand { Command = "start", Description = "شروع گفتگو با ربات" },
        new Telegram.Bot.Types.BotCommand { Command = "exit", Description = "خروج از ربات" }
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
        Console.WriteLine($"✅ Bot @{botInfo.Username} is running...");

        Console.ReadLine();
        cts.Cancel();
    }


    private static async Task HandleUpdateAsync(
        ITelegramBotClient botClient,
        Update update,
        CancellationToken cancellationToken)
    {
        if (update.Message is not { Text: { } messageText }) return;

        var chatId = update.Message.Chat.Id;

        var response = messageText switch
        {
            "/start" => "خوش آمدید!",
            "/exit" => "به امید دیدار!",
            _ => "❓ فرمان نامعتبر است. از /start یا /exit استفاده کنید."
        };

        await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: response,
            cancellationToken: cancellationToken
        );
    }

    private static Task HandlePollingErrorAsync(
        ITelegramBotClient botClient,
        Exception exception,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"❌ Error: {exception.Message}");
        return Task.CompletedTask;
    }
}

