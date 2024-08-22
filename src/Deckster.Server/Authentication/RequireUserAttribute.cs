using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Deckster.Server.Authentication;

public class RequireUserAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.HttpContext.TryGetUser(out _))
        {
            return;
        }

        context.Result = new ChallengeResult(AuthenticationSchemes.Cookie);
    }
}