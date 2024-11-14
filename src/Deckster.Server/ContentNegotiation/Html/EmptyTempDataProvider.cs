using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Deckster.Server.ContentNegotiation.Html;

public class EmptyTempDataProvider : ITempDataProvider
{
    private const string Key = "tempdataz";
        
    public IDictionary<string, object> LoadTempData(HttpContext context)
    {
        if (context.Items.TryGetValue(Key, out var value) && value is IDictionary<string, object> dictionary)
        {
            return dictionary;
        }
        return new Dictionary<string, object>();
    }

    public void SaveTempData(HttpContext context, IDictionary<string, object> values)
    {
        context.Items[Key] = values;
    }
}