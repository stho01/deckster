using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Deckster.Server.ContentNegotiation.Html;

public class ViewDataFilter : IActionFilter
{
    public void OnActionExecuted(ActionExecutedContext context)
    {
        if (context.Controller is Controller controller)
        {
            var viewData = controller.ViewData;
            if (viewData.Any())
            {
                context.HttpContext.SetViewData(viewData);
            }
        }
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
            
    }
}