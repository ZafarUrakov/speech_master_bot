using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using speech_master_bot.Models;
using speech_master_bot.Services;
using Telegram.Bot;

namespace speech_master_bot
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            BotConfiguration = Configuration.GetSection("BotConfiguration").Get<BotConfiguration>();
        }

        public IConfiguration Configuration { get; }
        public BotConfiguration BotConfiguration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHostedService<ConfigureWebhook>();

            services.AddHttpClient("tgWebhook")
                .AddTypedClient<ITelegramBotClient>(httpClient =>
                new TelegramBotClient(BotConfiguration.Token, httpClient));

            services.AddScoped<TelegramBotService>();

            services.AddControllers().AddNewtonsoftJson();

        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseDeveloperExceptionPage();
            app.UseHttpsRedirection();
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                var token = BotConfiguration.Token;

                endpoints.MapControllerRoute(
                    name: "tgWebhook",
                    pattern: $"bot/{token}",
                    new { controller = "Webhook", action = "Post" });
                endpoints.MapControllers();

                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
