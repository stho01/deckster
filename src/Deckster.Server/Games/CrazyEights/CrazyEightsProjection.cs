using System.Reflection;
using Deckster.Client.Games.CrazyEights;
using Deckster.Client.Protocol;
using Deckster.Server.Games.CrazyEights.Core;
using Marten.Events.Aggregation;

namespace Deckster.Server.Games.CrazyEights;

public class CrazyEightsProjection : SingleStreamProjection<CrazyEightsGame>
{
    private static readonly Dictionary<Type, MethodInfo> _applies;

    static CrazyEightsProjection()
    {
        var methods = from method in typeof(CrazyEightsProjection)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
            where method.Name == "Apply" && method.ReturnType == typeof(Task)
            let parameters = method.GetParameters()
            where parameters.Length == 2 &&
                  parameters[0].ParameterType.IsSubclassOf(typeof(DecksterRequest)) &&
                  parameters[1].ParameterType == typeof(CrazyEightsGame)
            let parameter = parameters[0]
            select (parameter, method);
        _applies = methods.ToDictionary(m => m.parameter.ParameterType, m => m.method);
    }
    
    public static CrazyEightsGame Create(CrazyEightsGameCreatedEvent created)
    {
        return CrazyEightsGame.Create(created);
    }
    
    public static Task Apply(PutCardRequest @event, CrazyEightsGame game) => game.PutCard(@event.PlayerId, @event.Card);
    public static Task Apply(PutEightRequest @event, CrazyEightsGame game) => game.PutEight(@event.PlayerId, @event.Card, @event.NewSuit);
    public static Task Apply(DrawCardRequest @event, CrazyEightsGame game) => game.DrawCard(@event.PlayerId);
    public static Task Apply(PassRequest @event, CrazyEightsGame game) => game.Pass(@event.PlayerId);
    
    public static async Task<bool> HandleAsync(DecksterRequest request, CrazyEightsGame game)
    {
        if (!_applies.TryGetValue(request.GetType(), out var del))
        {
            return false;
        }

        await (Task)del.Invoke(null, [request, game]);
        return true;
    }
}