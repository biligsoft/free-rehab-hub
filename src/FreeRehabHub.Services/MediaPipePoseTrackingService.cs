using System.Net.WebSockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using FreeRehabHub.Modules.Contracts;

namespace FreeRehabHub.Services;

public sealed class MediaPipePoseTrackingService : IPoseTrackingService, IDisposable
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private static readonly TimeSpan DefaultHealthCheckTimeout = TimeSpan.FromSeconds(15);
    private const int ReceiveBufferSize = 16 * 1024;

    private readonly ProcessManagerService _processManager = new();
    private readonly string _executablePath;
    private readonly string _argumentsTemplate;
    private readonly string _serviceWorkingDirectory;
    private readonly int _port;
    private readonly TimeSpan _healthCheckTimeout;

    private ClientWebSocket? _webSocket;
    private CancellationTokenSource? _receiveLoopCancellation;
    private Task? _receiveLoopTask;

    // argumentsTemplate, {0} yerine port numarasının konacağı bir format string'i — dev modunda
    // (.venv'deki python) "-m uvicorn app.main:app --host 127.0.0.1 --port {0}", paketlenmiş
    // modunda (PyInstaller'ın ürettiği run_server.py binary'si) "--host 127.0.0.1 --port {0}"
    // (bkz. AppServices.ResolveMediaPipeCommand ve services/mediapipe-service/run_server.py).
    public MediaPipePoseTrackingService(
        string executablePath, string argumentsTemplate, string serviceWorkingDirectory,
        int port = 8000, TimeSpan? healthCheckTimeout = null)
    {
        _executablePath = executablePath;
        _argumentsTemplate = argumentsTemplate;
        _serviceWorkingDirectory = serviceWorkingDirectory;
        _port = port;
        _healthCheckTimeout = healthCheckTimeout ?? DefaultHealthCheckTimeout;
    }

    public PoseTrackingStatus Status { get; private set; } = PoseTrackingStatus.Stopped;
    public string? LastError { get; private set; }

    public event EventHandler<PoseFrame>? FrameReceived;
    public event EventHandler<PoseTrackingStatus>? StatusChanged;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        SetStatus(PoseTrackingStatus.Starting);

        try
        {
            if (!_processManager.IsRunning)
            {
                _processManager.Start(
                    _executablePath,
                    string.Format(_argumentsTemplate, _port),
                    _serviceWorkingDirectory);
            }

            var healthCheckUri = new Uri($"http://127.0.0.1:{_port}/health");
            var healthy = await _processManager.WaitUntilHealthyAsync(healthCheckUri, _healthCheckTimeout, cancellationToken);
            if (!healthy)
            {
                throw new InvalidOperationException("mediapipe-service belirtilen sürede sağlıklı hale gelmedi.");
            }

            var webSocket = new ClientWebSocket();
            await webSocket.ConnectAsync(new Uri($"ws://127.0.0.1:{_port}/ws/pose"), cancellationToken);
            _webSocket = webSocket;

            _receiveLoopCancellation = new CancellationTokenSource();
            _receiveLoopTask = Task.Run(() => ReceiveLoopAsync(webSocket, _receiveLoopCancellation.Token));

            LastError = null;
            SetStatus(PoseTrackingStatus.Running);
        }
        catch (Exception exception)
        {
            LastError = exception.Message;
            SetStatus(PoseTrackingStatus.Error);
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        // Sıra önemli: _receiveLoopCancellation.Cancel() bekleyen bir ReceiveAsync'i iptal ederse
        // .NET'in ClientWebSocket'i soketi otomatik "aborted" durumuna sokuyor (belgelenmiş
        // davranış) — kapanış denemesi sonra çağrılırsa zaten-abort-edilmiş soket üzerinde
        // ObjectDisposedException fırlatıyordu (CI'da F8.09'da yakalandı). Bu yüzden önce
        // soket hâlâ canlıyken kapanış deneniyor, Cancel() döngüyü zorla durdurmak için
        // sadece bir güvenlik ağı olarak ondan sonra çağrılıyor.
        //
        // CloseAsync değil CloseOutputAsync kullanılıyor: CloseAsync, karşı taraftan gelecek
        // kapanış onayını okumak için kendi içinde ayrı bir ReceiveAsync çalıştırıyor — bu,
        // ReceiveLoopAsync'in zaten bekleyen kendi ReceiveAsync'iyle çakışıp
        // InvalidOperationException fırlatıyordu (ReceiveLoopAsync bunu yakalayıp durumu
        // Error'a çeviriyordu, CI'da F8.12'de yakalandı — Ubuntu/macOS'ta tutarlı şekilde
        // tekrar üretildi). CloseOutputAsync sadece kapanış çerçevesini gönderip hiç okuma
        // yapmadan dönüyor; karşı tarafın kapanış mesajını ReceiveLoopAsync'in kendisi okuyup
        // döngüden temiz çıkıyor (bkz. aşağıdaki "MessageType == Close" kontrolü).
        if (_webSocket is { State: WebSocketState.Open })
        {
            try
            {
                await _webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "İstemci durdurdu.", cancellationToken);
            }
            catch (Exception exception) when (
                exception is WebSocketException or OperationCanceledException or ObjectDisposedException)
            {
                // Karşı taraf zaten kapatmış/abort etmiş olabilir, durdurma işlemini engellemiyoruz.
            }
        }

        _receiveLoopCancellation?.Cancel();

        if (_receiveLoopTask is not null)
        {
            await _receiveLoopTask.WaitAsync(CancellationToken.None).ContinueWith(_ => { }, TaskScheduler.Default);
        }

        _webSocket?.Dispose();
        _webSocket = null;
        _receiveLoopCancellation?.Dispose();
        _receiveLoopCancellation = null;
        _receiveLoopTask = null;

        SetStatus(PoseTrackingStatus.Stopped);
    }

    // FrameReceived/StatusChanged, WebSocket okuma döngüsünün çalıştığı thread pool thread'inden
    // tetiklenebilir — Godot node'larına dokunan dinleyiciler (ör. ModuleHost) ana thread'e
    // CallDeferred ile geçmeli, Godot API'leri arka plan thread'inden çağrılamaz.
    private async Task ReceiveLoopAsync(ClientWebSocket webSocket, CancellationToken cancellationToken)
    {
        var buffer = new byte[ReceiveBufferSize];

        try
        {
            while (webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                using var messageStream = new MemoryStream();
                WebSocketReceiveResult result;
                do
                {
                    result = await webSocket.ReceiveAsync(buffer, cancellationToken);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        return;
                    }
                    messageStream.Write(buffer, 0, result.Count);
                } while (!result.EndOfMessage);

                messageStream.Position = 0;
                var poseFrame = await JsonSerializer.DeserializeAsync<PoseFrame>(messageStream, SerializerOptions, cancellationToken);
                if (poseFrame is not null)
                {
                    FrameReceived?.Invoke(this, poseFrame);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // StopAsync tarafından iptal edildi, normal kapanış — hata değil.
        }
        catch (Exception exception)
        {
            LastError = exception.Message;
            SetStatus(PoseTrackingStatus.Error);
        }
    }

    private void SetStatus(PoseTrackingStatus status)
    {
        Status = status;
        StatusChanged?.Invoke(this, status);
    }

    public void Dispose()
    {
        _receiveLoopCancellation?.Cancel();
        _webSocket?.Dispose();
        _receiveLoopCancellation?.Dispose();
        _processManager.Dispose();
    }
}
