using System.Collections.Concurrent;
using System.Reflection;
using Deckster.Core.Protocol;
using Marten.Events.Aggregation;
using Marten.Events.CodeGeneration;

namespace Deckster.Server.Games;

public interface IGameProjection;

public abstract class GameProjection<TGame> : SingleStreamProjection<TGame>, IGameProjection
{
    [MartenIgnore]
    public abstract (TGame game, object startEvent) Create(IGameHost host);
}

public static class GameProjectionExtensions
{
    private static readonly ConcurrentDictionary<Type, Dictionary<Type, MethodInfo>> Applies = new(); 
    
    /// <summary>
    /// This is crap. Lazy reflection crap.
    /// </summary>
    public static async Task<bool> HandleAsync<TGame>(this GameProjection<TGame> projection, DecksterRequest request, TGame game)
    {
        if (!GetApplies(projection).TryGetValue(request.GetType(), out var del))
        {
            return false;
        }

        await (Task)del.Invoke(projection, [request, game]);
        return true;
    }

    private static Dictionary<Type, MethodInfo> GetApplies<TGame>(GameProjection<TGame> projection)
    {
        var projectionType = projection.GetType();
        return Applies.GetOrAdd(projectionType, Create());

        Dictionary<Type, MethodInfo> Create()
        {
            var methods = from method in projectionType
                    .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                where method.Name == "Apply" && method.ReturnType == typeof(Task)
                let parameters = method.GetParameters()
                where parameters.Length == 2 &&
                      parameters[0].ParameterType.IsSubclassOf(typeof(DecksterRequest)) &&
                      parameters[1].ParameterType == typeof(TGame)
                let parameter = parameters[0]
                select (parameter, method);
            return methods.ToDictionary(m => m.parameter.ParameterType, m => m.method);    
        }
    }
}