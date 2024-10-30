using Deckster.Client.Games;
using Deckster.Client.Protocol;
using Deckster.Server.CodeGeneration;
using Deckster.Server.CodeGeneration.Meta;
using Deckster.Server.Games;

namespace Deckster.Generated.Client;

public class Program
{
    public static async Task<int> Main(string[] argz)
    {
        try
        {
            Console.WriteLine("Generating code!");
            var projectPath = GetProjectPath(argz);
            var openapi = new OpenApiDocumentGenerator(typeof(DecksterMessage));
            await openapi.WriteAsJsonAsync(Path.Combine(projectPath, "deckster.openapi.json"));
            await openapi.WriteAsYamlAsync(Path.Combine(projectPath, "deckster.openapi.yaml"));

            var baseType = typeof(GameObject);
            var types = baseType.Assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && baseType.IsAssignableFrom(t))
                .ToArray();

            await GenerateKotlinCodeAsync(Path.Combine(projectPath, "kotlin"), types);
            
            return 0;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return 1;
        }
    }

    private static string GetProjectPath(string[] argz)
    {
        if (argz.Any())
        {
            return argz[0];
        }

        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory != null && directory.GetFiles("Deckster.Generated.Client.csproj").Length == 0)
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Could not find project path for Deckster.Generated.Client.csproj");
    }

    private static async Task GenerateKotlinCodeAsync(string basePath, Type[] types)
    {
        if (Directory.Exists(basePath))
        {
            Directory.Delete(basePath, true);
        }

        Directory.CreateDirectory(basePath);
        
        foreach (var type in types)
        {
            if (GameMeta.TryGetFor(type, out var game))
            {
                var ns = type.Namespace?.Split('.').LastOrDefault()?.ToLowerInvariant() ?? throw new Exception($"OMG CANT HAZ NAEMSPAZE OF ITZ TAYP '{type.Name}'");
                var path = Path.Combine(basePath, "no.forse.decksterlib",  ns, $"{game.Name}Client.kt");
                
                Console.WriteLine(path);
                var kotlin = new KotlinGenerator(game, $"no.forse.decksterlib.{ns}");
                await kotlin.WriteToAsync(path);    
            }
        }
    }
}
