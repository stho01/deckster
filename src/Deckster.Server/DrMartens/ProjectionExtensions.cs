using System.Reflection;
using Marten.Events.Daemon;
using Marten.Events.Projections;

namespace Deckster.Server.DrMartens;

public static class ProjectionExtensions
{
    private static readonly MethodInfo Method; 

    static ProjectionExtensions()
    {
        var methods = from m in typeof(ProjectionOptions).GetMethods(BindingFlags.Public | BindingFlags.Instance)
            where m is {Name: nameof(ProjectionOptions.Add), IsGenericMethod: true} &&
                  m.GetGenericArguments().Length == 1
            let parameters = m.GetParameters()
            where parameters.Length == 2 &&
                  parameters[0].ParameterType == typeof(ProjectionLifecycle) &&
                  parameters[1].ParameterType == typeof(Action<AsyncOptions>)
            select m;
                
        Method = methods.Single();
    }
    
    public static void AddType(this ProjectionOptions o, Type projectionType, ProjectionLifecycle lifecycle, Action<AsyncOptions>? asyncConfiguration = null)
    {
        if (!typeof(GeneratedProjection).IsAssignableFrom(projectionType))
        {
            throw new ArgumentException($"{projectionType.Name} must be a {nameof(GeneratedProjection)}", nameof(projectionType));
        }

        Method.MakeGenericMethod(projectionType).Invoke(o, [lifecycle, asyncConfiguration]);
    }
}