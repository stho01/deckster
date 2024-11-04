using Deckster.CodeGenerator.CSharp;
using Deckster.CodeGenerator.Generators;
using Deckster.CodeGenerator.IO;
using Deckster.Core.Protocol;
using Deckster.Games;
using Deckster.Games.CodeGeneration;
using Deckster.Games.CodeGeneration.Meta;

namespace Deckster.CodeGenerator;

public class Program
{
    public static async Task<int> Main(string[] argz)
    {
        try
        {
            Console.WriteLine("Generating code!");
            var gitDirectory = GetGitPath();
            
            Console.WriteLine($"git path: {gitDirectory}");
            
            var openapi = new OpenApiDocumentGenerator(typeof(DecksterMessage));

            gitDirectory.GetFile("generated", "deckster.opeanpi.json");
            await openapi.WriteAsJsonAsync(gitDirectory.GetFile("generated", "deckster.opeanpi.json"));
            await openapi.WriteAsYamlAsync(gitDirectory.GetFile("generated", "deckster.opeanpi.json"));

            var baseType = typeof(GameObject);
            var types = baseType.Assembly.GetTypes()
                .Where(t => t is {IsClass: true, IsAbstract: false} && baseType.IsAssignableFrom(t))
                .ToArray();
            
            await GenerateClientsAsync(gitDirectory, types);
            
            return 0;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return 1;
        }
    }

    private static DirectoryInfo GetGitPath()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory != null && directory.GetDirectories(".git").Length == 0)
        {
            directory = directory.Parent;
        }

        return directory ?? throw new InvalidOperationException("Could not find .git path");
    }

    private static async Task GenerateClientsAsync(DirectoryInfo gitDirectory, Type[] types)
    {
        foreach (var type in types)
        {
            if (CSharpGameMeta.TryGetFor(type, out var gameMeta))
            {
                await GenerateCsharpAsync(gitDirectory, type, gameMeta);    
            }
            
            if (GameMeta.TryGetFor(type, out var game))
            {
                
                await GenerateKotlinAsync(gitDirectory, type, game);
            }
        }
    }

    private static async Task GenerateCsharpAsync(DirectoryInfo gitDirectory, Type gameType, CSharpGameMeta game)
    {
        var directory = gitDirectory.GetSubDirectory("src", "Deckster.Client", "Games", game.Name);

        if (directory.Exists)
        {
            directory.Delete(true);
        }
        directory.Create();
        Console.WriteLine(directory);
        
        var ns = gameType.Namespace?.Split('.').LastOrDefault() ?? throw new Exception($"OMG CANT HAZ NAEMSPAZE OF ITZ TAYP '{gameType.Name}'");
        
        var file = directory.GetFile($"{game.Name}Client.cs");
        
        var kotlin = new CsharpClientGenerator(game, $"Deckster.Client.Games.{ns}");
        await kotlin.WriteToAsync(file);
    }

    private static async Task GenerateKotlinAsync(DirectoryInfo gitDirectory, Type type, GameMeta game)
    {
        var kotlinDirectory = gitDirectory.GetSubDirectory("generated", "kotlin");
        if (kotlinDirectory.Exists)
        {
            kotlinDirectory.Delete(true);
        }

        kotlinDirectory.Create();
        
        var ns = type.Namespace?.Split('.').LastOrDefault()?.ToLowerInvariant() ?? throw new Exception($"OMG CANT HAZ NAEMSPAZE OF ITZ TAYP '{type.Name}'");
        var file = kotlinDirectory.GetFile("no.forse.decksterlib",  ns, $"{game.Name}Client.kt");
                
        Console.WriteLine(file);
        var kotlin = new KotlinClientGenerator(game, $"no.forse.decksterlib.{ns}");
        await kotlin.WriteToAsync(file);
    }
}
