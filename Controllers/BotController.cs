using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Filters;
using Telegram.Bot.Services;
using Telegram.Bot.Types;
using Parsec;
using PlanFixApiGetUser;
using System.Xml;

namespace Telegram.Bot.Controllers;

public class BotController : ControllerBase
{
    [HttpPost]
    [ValidateTelegramBot]
    public async Task<IActionResult> Post(
        [FromBody] Update update,
        [FromServices] UpdateHandlers handleUpdateService,
        CancellationToken cancellationToken)
    {
        await handleUpdateService.HandleUpdateAsync(update, cancellationToken);
        return Ok();
    }
}
