using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using FreeRehabHub.Modules.Contracts;
using FreeRehabHub.Services;
using Xunit;

namespace FreeRehabHub.Services.Tests;

public sealed class MediaPipePoseTrackingServiceTests
{
    private static readonly TimeSpan ShortHealthCheckTimeout = TimeSpan.FromMilliseconds(500);

    [Fact]
    public async Task StartAsync_ServerSendsFrame_RaisesFrameReceivedWithParsedData()
    {
        var port = GetFreeTcpPort();
        const string framePayload = """
            {"capturedAt":"2026-07-25T12:00:00Z","poses":[{"landmarks":[
                {"type":"leftShoulder","normalized":{"x":0.1,"y":0.2,"z":0.3},"world":{"x":1.1,"y":1.2,"z":1.3},"visibility":0.9,"presence":0.95}
            ]}]}
            """;
        using var fakeServer = new FakeMediaPipeServer(port, [framePayload]);
        var (fileName, arguments) = PlaceholderProcessCommand();
        using var service = new MediaPipePoseTrackingService(fileName, arguments, port, ShortHealthCheckTimeout);

        PoseFrame? receivedFrame = null;
        using var frameReceivedSignal = new SemaphoreSlim(0);
        service.FrameReceived += (_, frame) =>
        {
            receivedFrame = frame;
            frameReceivedSignal.Release();
        };

        await service.StartAsync();
        var signaled = await frameReceivedSignal.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.True(signaled);
        Assert.Equal(PoseTrackingStatus.Running, service.Status);
        Assert.NotNull(receivedFrame);
        var landmark = Assert.Single(Assert.Single(receivedFrame!.Poses).Landmarks);
        Assert.Equal(PoseLandmarkType.LeftShoulder, landmark.Type);
        Assert.Equal(0.1f, landmark.Normalized.X);
        Assert.Equal(1.1f, landmark.World.X);
        Assert.Equal(0.9f, landmark.Visibility);

        await service.StopAsync();
    }

    [Fact]
    public async Task StartAsync_HealthCheckNeverSucceeds_ThrowsAndSetsErrorStatus()
    {
        var unusedPort = GetFreeTcpPort();
        var (fileName, arguments) = PlaceholderProcessCommand();
        using var service = new MediaPipePoseTrackingService(fileName, arguments, unusedPort, ShortHealthCheckTimeout);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.StartAsync());

        Assert.Equal(PoseTrackingStatus.Error, service.Status);
        Assert.NotNull(service.LastError);
    }

    [Fact]
    public async Task StartStop_FullCycle_StatusTransitionsInOrder()
    {
        var port = GetFreeTcpPort();
        using var fakeServer = new FakeMediaPipeServer(port, []);
        var (fileName, arguments) = PlaceholderProcessCommand();
        using var service = new MediaPipePoseTrackingService(fileName, arguments, port, ShortHealthCheckTimeout);

        var observedStatuses = new List<PoseTrackingStatus>();
        service.StatusChanged += (_, status) => observedStatuses.Add(status);

        await service.StartAsync();
        await service.StopAsync();

        Assert.Equal(
            [PoseTrackingStatus.Starting, PoseTrackingStatus.Running, PoseTrackingStatus.Stopped],
            observedStatuses);
    }

    private static (string PythonExecutablePath, string WorkingDirectory) PlaceholderProcessCommand()
    {
        // MediaPipePoseTrackingService, verilen yürütülebiliri her zaman "-m uvicorn ..." argümanlarıyla
        // çağırıyor — testte gerçek uvicorn'a ihtiyacımız yok (health-check/WS trafiği ayrıca
        // FakeMediaPipeServer'a gidiyor), sadece Process.Start()'ın başarıyla bir OS süreci başlatması
        // yeterli; python3/python modül bulunamayınca hızlıca çıkacak ama bu testleri etkilemiyor.
        var pythonExecutablePath = OperatingSystem.IsWindows() ? "python" : "python3";
        return (pythonExecutablePath, Environment.CurrentDirectory);
    }

    private static int GetFreeTcpPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private sealed class FakeMediaPipeServer : IDisposable
    {
        private readonly HttpListener _listener;
        private readonly List<string> _messagesToSend;

        public FakeMediaPipeServer(int port, IEnumerable<string> messagesToSend)
        {
            _messagesToSend = messagesToSend.ToList();
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://127.0.0.1:{port}/");
            _listener.Start();
            _ = Task.Run(AcceptLoopAsync);
        }

        private async Task AcceptLoopAsync()
        {
            while (_listener.IsListening)
            {
                HttpListenerContext context;
                try
                {
                    context = await _listener.GetContextAsync();
                }
                catch (Exception)
                {
                    return;
                }

                _ = Task.Run(() => HandleContextAsync(context));
            }
        }

        private async Task HandleContextAsync(HttpListenerContext context)
        {
            if (context.Request.Url?.AbsolutePath == "/health")
            {
                context.Response.StatusCode = 200;
                context.Response.Close();
                return;
            }

            if (context.Request.Url?.AbsolutePath == "/ws/pose" && context.Request.IsWebSocketRequest)
            {
                var wsContext = await context.AcceptWebSocketAsync(subProtocol: null);
                var webSocket = wsContext.WebSocket;

                foreach (var message in _messagesToSend)
                {
                    var bytes = Encoding.UTF8.GetBytes(message);
                    await webSocket.SendAsync(bytes, WebSocketMessageType.Text, endOfMessage: true, CancellationToken.None);
                }

                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "test tamamlandı", CancellationToken.None);
                return;
            }

            context.Response.StatusCode = 404;
            context.Response.Close();
        }

        public void Dispose()
        {
            _listener.Stop();
            _listener.Close();
        }
    }
}
