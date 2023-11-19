using Microsoft.AspNetCore.Mvc;
using speech_master_bot.Services;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace speech_master_bot.Controllers
{
    public class WebhookController : Controller
    {
        [HttpPost]
        public async ValueTask<IActionResult> Post(
            [FromServices] TelegramBotService telegramBotService,
            [FromBody] Update update)
        {
            await telegramBotService.EchoAsync(update);

            return Ok();
        }
    }
}
