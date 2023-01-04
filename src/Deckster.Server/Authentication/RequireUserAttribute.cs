using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Deckster.Server.Authentication;

public class RequireUserAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var user = context.HttpContext.GetUser();
        if (user != null)
        {
            return;
        }

        context.Result = new ForbidResult();
    }
}