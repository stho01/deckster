using Deckster.CodeGenerator.CSharp;
using Deckster.CodeGenerator.Generators;
using Deckster.CodeGenerator.IO;
using Deckster.Core.Protocol;
using Deckster.Games;
using Deckster.Games.CodeGeneration;
using Deckster.Games.CodeGeneration.Meta;
using Deckster.Server;
using Deckster.Server.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;

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

            await GenerateOpenApiForEverythingAsync(gitDirectory);
            await GenerateOpenApiSchemaForMessagesAsync(gitDirectory);
            await GenerateClientsAsync(gitDirectory);
            
            return 0;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return 1;
        }
    }

    private static async Task GenerateOpenApiSchemaForMessagesAsync(DirectoryInfo gitDirectory)
    {
        var openapi = new OpenApiDocumentGenerator(typeof(DecksterMessage));
        gitDirectory.GetFile("generated", "deckster.opeanpi.json");
        await openapi.WriteAsJsonAsync(gitDirectory.GetFile("generated", "deckster.opeanpi.json"));
        await openapi.WriteAsYamlAsync(gitDirectory.GetFile("generated", "deckster.opeanpi.json"));
    }

    private static async Task GenerateOpenApiForEverythingAsync(DirectoryInfo gitDirectory)
    {
        using var server = new TestServer(new WebHostBuilder()
            .ConfigureServices(s => Startup.ConfigureServices(s, new DecksterConfig()))
            .Configure(Startup.Configure)
        );

        {
            var response = await server.CreateClient().GetAsync("/swagger/v1/swagger.json");
            var yaml = await response.Content.ReadAsStringAsync();

            await gitDirectory.GetFile("generated", "shebang.opeanpi.json")
                .WriteAllTextAsync(yaml);

            
        }

        {
            var response = await server.CreateClient().GetAsync("/swagger/v1/swagger.yaml");
            var yaml = await response.Content.ReadAsStringAsync();

            await gitDirectory.GetFile("generated", "shebang.opeanpi.yaml")
                .WriteAllTextAsync(yaml);
            
            await gitDirectory.GetFile("decksterapi.yml").WriteAllTextAsync(yaml);
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

    private static async Task GenerateClientsAsync(DirectoryInfo gitDirectory)
    {
        var baseType = typeof(GameObject);
        var types = baseType.Assembly.GetTypes()
            .Where(t => t is {IsClass: true, IsAbstract: false} && baseType.IsAssignableFrom(t))
            .ToArray();
        
        var kotlinDirectory = gitDirectory.GetSubDirectory("generated", "kotlin");
        if (kotlinDirectory.Exists)
        {
            kotlinDirectory.Delete(true);
        }
        kotlinDirectory.Create();
        
        foreach (var type in types)
        {
            if (CSharpGameMeta.TryGetFor(type, out var gameMeta))
            {
                await GenerateCsharpAsync(gitDirectory, type, gameMeta);    
            }
            
            if (GameMeta.TryGetFor(type, out var game))
            {
                await GenerateKotlinAsync(kotlinDirectory, type, game);
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
        
        var file = directory.GetFile($"{game.Name}Client.g.cs");
        
        var kotlin = new CsharpClientGenerator(game, $"Deckster.Client.Games.{ns}");
        await kotlin.WriteToAsync(file);
    }

    private static async Task GenerateKotlinAsync(DirectoryInfo kotlinDirectory, Type type, GameMeta game)
    {

        var ns = type.Namespace?.Split('.').LastOrDefault()?.ToLowerInvariant() ?? throw new Exception($"OMG CANT HAZ NAEMSPAZE OF ITZ TAYP '{type.Name}'");
        var file = kotlinDirectory.GetFile("no.forse.decksterlib",  ns, $"{game.Name}Client.kt");
                
        Console.WriteLine(file);
        var kotlin = new KotlinClientGenerator(game, $"no.forse.decksterlib.{ns}");
        await kotlin.WriteToAsync(file);
    }
}
