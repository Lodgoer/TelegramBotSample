using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TelegramBotSample;

public static class AdminHandler
{
    public const long AdminChatId = 1909383168;

    // وضعیت گفت‌وگو کاربران با ادمین
    private static readonly Dictionary<long, bool> UserInChatWithAdmin = new();

    public static bool IsUserChattingWithAdmin(long userId)
    {
        return UserInChatWithAdmin.TryGetValue(userId, out bool isChatting) && isChatting;
    }

    public static void StartChatWithAdmin(long userId)
    {
        UserInChatWithAdmin[userId] = true;
    }

    public static void EndChatWithAdmin(long userId)
    {
        if (UserInChatWithAdmin.ContainsKey(userId))
        {
            UserInChatWithAdmin[userId] = false;
        }
    }

    public static async Task ForwardMessageToAdmin(ITelegramBotClient botClient, Message userMessage, CancellationToken cancellationToken)
    {
        var userId = userMessage.Chat.Id;
        var username = userMessage.From?.Username ?? "ندارد";
        var text = userMessage.Text ?? "(بدون متن)";

        string forwarded = $"\uD83D\uDCEC پیام جدید از کاربر:\n\n" +
                           $"🆔 آیدی عددی: {userId}\n" +
                           $"✉️ متن:\n{text}";

        await botClient.SendTextMessageAsync(
            chatId: AdminChatId,
            text: forwarded,
            cancellationToken: cancellationToken);

        await botClient.SendTextMessageAsync(
            chatId: userId,
            text: "✅ پیامت برای ادمین ارسال شد.",
            cancellationToken: cancellationToken);

        EndChatWithAdmin(userId);
    }

    public static async Task HandleAdminReplyAsync(ITelegramBotClient botClient, Message adminMessage, CancellationToken cancellationToken)
    {
        var text = adminMessage.Text ?? "";
        var adminId = adminMessage.Chat.Id;

        if (adminId != AdminChatId)
            return;

        if (!text.StartsWith("/reply"))
        {
            await botClient.SendTextMessageAsync(
                chatId: adminId,
                text: "📌 برای پاسخ به کاربر بنویس: /reply userid پیام",
                cancellationToken: cancellationToken);
            return;
        }

        var parts = text.Split(' ', 3);
        if (parts.Length < 3)
        {
            await botClient.SendTextMessageAsync(
                chatId: adminId,
                text: "❗ فرمت نادرست است. استفاده کن: /reply userid پیام",
                cancellationToken: cancellationToken);
            return;
        }

        if (!long.TryParse(parts[1], out long userId))
        {
            await botClient.SendTextMessageAsync(
                chatId: adminId,
                text: "❗ آیدی عددی کاربر اشتباه است.",
                cancellationToken: cancellationToken);
            return;
        }

        var messageToUser = parts[2];

        await botClient.SendTextMessageAsync(
            chatId: userId,
            text: $"📬 پاسخ ادمین:\n{messageToUser}",
            cancellationToken: cancellationToken);

        await botClient.SendTextMessageAsync(
            chatId: adminId,
            text: "✅ پاسخ برای کاربر ارسال شد.",
            cancellationToken: cancellationToken);
    }
}
