using System.Diagnostics;
using System.Net.Http;

namespace FreeRehabHub.Services;

// Genel amaçlı native süreç yaşam döngüsü yöneticisi — mediapipe-service'e özel bir şey bilmez,
// IPoseTrackingService implementasyonu bunu sarmalayarak kullanacak (F5.07).
public sealed class ProcessManagerService : IDisposable
{
    private static readonly TimeSpan HealthCheckPollInterval = TimeSpan.FromMilliseconds(200);

    private Process? _process;

    public bool IsRunning => _process is { HasExited: false };

    public void Start(string fileName, string arguments, string workingDirectory)
    {
        if (IsRunning)
        {
            return;
        }

        // Stdout/stderr'i redirect edip okumadan bırakmak, OS pipe buffer'ı dolunca alt sürecin
        // asılı kalmasına (deadlock) yol açabilir — bu yüzden redirect etmiyoruz, konsolu miras alır.
        _process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        _process.Start();
    }

    public async Task<bool> WaitUntilHealthyAsync(
        Uri healthCheckUri, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        using var httpClient = new HttpClient();
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            if (!IsRunning)
            {
                return false;
            }

            try
            {
                var response = await httpClient.GetAsync(healthCheckUri, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
            }
            catch (HttpRequestException)
            {
                // Servis henüz ayakta değil, süre dolana kadar tekrar denenecek.
            }

            await Task.Delay(HealthCheckPollInterval, cancellationToken);
        }

        return false;
    }

    public void Stop()
    {
        if (_process is null)
        {
            return;
        }

        if (!_process.HasExited)
        {
            _process.Kill(entireProcessTree: true);
            _process.WaitForExit();
        }

        _process.Dispose();
        _process = null;
    }

    public void Dispose()
    {
        Stop();
    }
}
