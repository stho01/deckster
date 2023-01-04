using System.Net;
using System.Net.Sockets;
using Deckster.Client;
using Deckster.Client.Common;
using Deckster.Client.Communication;
using Deckster.Client.Communication.Handshake;
using Deckster.Client.Logging;
using Deckster.Server.Users;

namespace Deckster.Server.Infrastructure;

public class DecksterServer : IDisposable
{
    private readonly int _port;
    private readonly ILogger _logger = Log.Factory.CreateLogger<DecksterServer>();
    private readonly IServiceProvider _services;
    private readonly UserRepo _userRepo;
    private readonly TcpListener _listener;
    private readonly List<IDecksterChannel> _communicators = new();

    private readonly DecksterDelegate _pipeline;

    public DecksterServer(int port, IServiceProvider services, DecksterDelegate pipeline)
    {
        _port = port;
        _listener = new TcpListener(IPAddress.Any, port);
        _userRepo = services.GetRequiredService<UserRepo>();
        _pipeline = pipeline;
        _services = services;
    }
    
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _listener.Start();
            _logger.LogInformation("Listening for connections on port {port}", _port);
            while (!cancellationToken.IsCancellationRequested)
            {
                var socket = await _listener.AcceptSocketAsync(cancellationToken);
                HandleSocketAsync(socket, cancellationToken);
            }
        }
        finally
        {
            _logger.LogInformation("Stop listening for connections on port {port}", _port);
            _listener.Stop();
        }
    }

    private async void HandleSocketAsync(Socket socket, CancellationToken cancellationToken)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(10));
            var communicator = await HandshakeAsync(socket, cts.Token);
            if (communicator != null)
            {
                communicator.OnDisconnected += OnDisconnected;
                _communicators.Add(communicator);
            }
        }
        catch (Exception e)
        {
            socket.Dispose();
            _logger.LogError(e, "Unhandled handshake exception");
        }
    }

    private Task OnDisconnected(IDecksterChannel channel)
    {
        channel.OnDisconnected -= OnDisconnected;
        Remove(channel);
        return Task.CompletedTask;
    }

    private void Remove(IDecksterChannel channel)
    {
        _communicators.Remove(channel);
        channel.Dispose();
    }

    private async Task<IDecksterChannel?> HandshakeAsync(Socket socket, CancellationToken cancellationToken = default)
    {
        if (socket.RemoteEndPoint is not IPEndPoint endpoint)
        {
            _logger.LogError("Expected client endpoint to be IPEndPoint, but was {actualType} {actual}", socket.RemoteEndPoint?.GetType().Name, socket.RemoteEndPoint);
            socket.Dispose();
            return null;
        }
        _logger.LogInformation("Got connection from {endpoint}", endpoint);
        
        // 1. Read client hello
        var stream = new NetworkStream(socket);
        var request = await stream.ReceiveJsonAsync<ConnectRequest>(cancellationToken);
        if (request == null)
        {
            _logger.LogError("Invalid connect request");
            await stream.SendJsonAsync(new ConnectResponse { StatusCode = 400, Description = "Invalid request"}, cancellationToken);
            socket.Dispose();
            return null;
        }

        var user = await _userRepo.GetByTokenAsync(request.AccessToken, cancellationToken);
        if (user == null)
        {
            _logger.LogError("Invalid access token '{token}'. No user found", request.AccessToken);
            await stream.SendJsonAsync(new ConnectResponse { StatusCode = 400, Description = "Invalid accesstoken"}, cancellationToken);
            socket.Dispose();
            return null;
        }

        // 2. Connect to client
        var clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        await clientSocket.ConnectAsync(endpoint.Address, request.ClientPort, cancellationToken);
        var clientStream = new NetworkStream(clientSocket);

        var playerData = new PlayerData
        {
            Name = user.Name,
            PlayerId = user.Id
        };
        var communicator = new DecksterChannel(socket, stream, clientSocket, clientStream, playerData);

        var context = new ConnectionContext(communicator, request, user, _services);
        
        await _pipeline.Invoke(context);

        if (context.Response.StatusCode == 200)
        {
            await communicator.RespondAsync(context.Response, cancellationToken);
            return communicator;
        }

        context.Close();

        return null;
    }

    public void Dispose()
    {
        foreach (var communicator in _communicators)
        {
            try
            {
                communicator.OnDisconnected -= OnDisconnected;
                communicator.Dispose();
            }
            catch (ObjectDisposedException)
            {
                // ¯\_(ツ)_/¯
            }
            catch
            {
                _logger.LogError($"Could not dispose communicator {communicator}", communicator.PlayerData.Name);
            }
        }
        GC.SuppressFinalize(this);
    }
}

