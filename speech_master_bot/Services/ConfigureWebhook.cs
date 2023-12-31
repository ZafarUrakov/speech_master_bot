﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using speech_master_bot.Models;
using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace speech_master_bot.Services
{
    public class ConfigureWebhook : IHostedService
    {
        private readonly ILogger<ConfigureWebhook> logger;
        private readonly IServiceProvider serviceProvider;
        private readonly BotConfiguration botConfiguration;
        public ConfigureWebhook(
            ILogger<ConfigureWebhook> logger,
            IServiceProvider serviceProvider,
            IConfiguration configuration)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.botConfiguration = configuration
                .GetSection("BotConfiguration").Get<BotConfiguration>();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                using var scope = this.serviceProvider.CreateScope();

                var botClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();

                var webhookAddress = $"https://speechmasterbot-70b139b8f396.herokuapp.com/{botConfiguration.Token}";

                this.logger.LogInformation($"Setting webhook to: {webhookAddress}");

                await botClient.SetWebhookAsync(
                        url: webhookAddress,
                        allowedUpdates: Array.Empty<UpdateType>(),
                        cancellationToken: cancellationToken);

                this.logger.LogInformation("Webhook set successfully");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            using var scope = this.serviceProvider.CreateScope();

            var botClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();

            this.logger.LogInformation("Setting webhook");

            await botClient.SendTextMessageAsync(
                chatId: 1924521160,
                text: "Bot sleeping");
        }
    }
}
