using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;

class Program
{
    private static readonly string BotToken = "7223043557:AAEmeYzD5KRs_6y5hBwuDcNw_qO8emEHu6g";
    private static readonly long AdminChatId = 6152089950; // Replace with your Telegram ID

    private static readonly Dictionary<int, (long UserId, string MessageText)> MessageHistory = new();
    private static int MessageCounter = 0;

    static async Task Main()
    {
        var botClient = new TelegramBotClient(BotToken);
        using var cts = new CancellationTokenSource();

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>()
        };

        botClient.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            receiverOptions,
            cancellationToken: cts.Token
        );

        var me = await botClient.GetMeAsync();
        Console.WriteLine($"Bot {me.Username} is running...");

        await Task.Delay(-1);
    }

    static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Message is not { } message)
            return;

        long chatId = message.Chat.Id;
        long userId = message.From.Id;
        string userMessage = message.Text ?? "";

        // ✅ Handle Admin's Reply to Any User Message
        if (chatId == AdminChatId && message.ReplyToMessage != null)
        {
            string replyText = message.Text ?? "";
            string originalMessage = message.ReplyToMessage.Text ?? "";

            int messageIdIndex = originalMessage.LastIndexOf("🆔 MsgID: ") + "🆔 MsgID: ".Length;
            string messageIdString = "";

            for (int i = messageIdIndex; i < originalMessage.Length; i++)
            {
                if (char.IsDigit(originalMessage[i]))
                    messageIdString += originalMessage[i];
                else
                    break;
            }

            if (int.TryParse(messageIdString, out int msgId) && MessageHistory.TryGetValue(msgId, out var userData))
            {
                long replyUserId = userData.UserId;
                string originalUserMessage = userData.MessageText;

                try
                {
                    string responseMessage = $"🔄 *به پیام شما پاسخ داد:*\n❝ {originalUserMessage} ❞\n\n💬 *Admin:* {replyText}";
                    await botClient.SendTextMessageAsync(replyUserId, responseMessage, parseMode: ParseMode.Markdown, cancellationToken: cancellationToken);
                    await botClient.SendTextMessageAsync(AdminChatId, "✅ پاسخ شما با موفقیت ارسال شد.", cancellationToken: cancellationToken);
                }
                catch (Exception)
                {
                    await botClient.SendTextMessageAsync(AdminChatId, "❌ Could not send reply. User may have blocked the bot.", cancellationToken: cancellationToken);
                }
            }
            else
            {
                await botClient.SendTextMessageAsync(AdminChatId, "❌ Error: Could not find the original message.", cancellationToken: cancellationToken);
            }
            return;
        }

        // ✅ Handle User's Reply to Admin
        if (chatId != AdminChatId && message.ReplyToMessage != null)
        {
            string replyText = message.Text ?? "";
            string originalMessage = message.ReplyToMessage.Text ?? "";

            // Save user reply so the admin can reply to it later
            int messageId = ++MessageCounter;
            MessageHistory[messageId] = (userId, replyText);

            // Send reply to admin with original message reference
            string responseToAdmin = $"🔄 *Reply from User:*\n❝ {originalMessage} ❞\n\n💬 *User:* {replyText}\n🆔 MsgID: {messageId}";
            await botClient.SendTextMessageAsync(AdminChatId, responseToAdmin, parseMode: ParseMode.Markdown, cancellationToken: cancellationToken);

            // ✅ Send confirmation to user
            await botClient.SendTextMessageAsync(chatId, "✅ پاسخ شما با موفقیت ارسال شد!", cancellationToken: cancellationToken);

            return;
        }

        // ✅ Process Incoming Messages from Users
        string username = message.From.Username ?? "No username";
        string firstName = message.From.FirstName ?? "No first name";
        string lastName = message.From.LastName ?? "";

        int newMessageId = ++MessageCounter;
        MessageHistory[newMessageId] = (userId, userMessage);

        // ✅ Send user details first (MsgID included only here)
        string userInfo = $"📩 New Anonymous Message\n👤 Name: {firstName} {lastName}\n🔹 Username: @{username}\n🆔 MsgID: {newMessageId}\n\n(Reply to this message to answer)";
        await botClient.SendTextMessageAsync(AdminChatId, userInfo, cancellationToken: cancellationToken);

        // ✅ Send user message separately (without MsgID)
        await botClient.SendTextMessageAsync(AdminChatId, $"✉️ Message: {userMessage}", cancellationToken: cancellationToken);

        // ✅ Send Confirmation to User
        await botClient.SendTextMessageAsync(chatId, "✅ پیام شما با موفقیت ارسال شد!", cancellationToken: cancellationToken);
    }

    static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Error: {exception.Message}");
        return Task.CompletedTask;
    }
}

