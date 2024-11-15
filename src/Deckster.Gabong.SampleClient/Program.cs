﻿using Deckster.Client;
using Deckster.Client.Games.Gabong;
using Microsoft.Extensions.Configuration;

namespace Deckster.Gabong.SampleClient;

class Program
{
    public static async Task<int> Main(string[] argz)
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true)            ;
        var config = builder.Build();
        var settings = config.GetSection("Deckster").Get<DecksterSettings>();
        if (settings == null)
        {
            throw new Exception("OMG APPSETTINGS IZ NULLZ");
        }
        
        try
        {
            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, _) => cts.Cancel();
            
            var deckster = await DecksterClient.LogInOrRegisterAsync(settings.ServerUrl, "Kamuf Larsen", "hest");
            
            string? gamename = null;
            while (gamename == null)
            {
                Console.WriteLine("Enter game name:");
                gamename = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(gamename))
                {
                    Console.WriteLine("Not nearly good enough.");
                }
            }
            
            var game = await deckster.Gabong().CreateAndJoinAsync(gamename, cts.Token);
            var interactive = new GabongInteractive(game);
            await interactive.PlayAsync();
            
            return 0;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return 1;
        }
    }
}