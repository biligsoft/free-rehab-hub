using System.Net;
using FreeRehabHub.Services;
using Xunit;

namespace FreeRehabHub.Services.Tests;

public sealed class ProcessManagerServiceTests
{
    [Fact]
    public void Start_LaunchesLongRunningProcess_IsRunningTrue()
    {
        using var manager = new ProcessManagerService();
        var (fileName, arguments) = LongRunningProcessCommand();

        manager.Start(fileName, arguments, workingDirectory: Environment.CurrentDirectory);

        Assert.True(manager.IsRunning);

        manager.Stop();
    }

    [Fact]
    public void Stop_TerminatesProcess_IsRunningFalse()
    {
        using var manager = new ProcessManagerService();
        var (fileName, arguments) = LongRunningProcessCommand();
        manager.Start(fileName, arguments, workingDirectory: Environment.CurrentDirectory);

        manager.Stop();

        Assert.False(manager.IsRunning);
    }

    [Fact]
    public void Stop_CalledTwice_DoesNotThrow()
    {
        using var manager = new ProcessManagerService();
        var (fileName, arguments) = LongRunningProcessCommand();
        manager.Start(fileName, arguments, workingDirectory: Environment.CurrentDirectory);

        manager.Stop();
        manager.Stop();
    }

    [Fact]
    public async Task WaitUntilHealthyAsync_ServerRespondsOk_ReturnsTrue()
    {
        using var manager = new ProcessManagerService();
        var (fileName, arguments) = LongRunningProcessCommand();
        manager.Start(fileName, arguments, workingDirectory: Environment.CurrentDirectory);

        using var listener = new HttpListener();
        var port = GetFreeTcpPort();
        var prefix = $"http://127.0.0.1:{port}/";
        listener.Prefixes.Add(prefix);
        listener.Start();
        var serverTask = Task.Run(async () =>
        {
            var context = await listener.GetContextAsync();
            context.Response.StatusCode = 200;
            context.Response.Close();
        });

        var healthy = await manager.WaitUntilHealthyAsync(new Uri(prefix), TimeSpan.FromSeconds(5));

        Assert.True(healthy);
        await serverTask;
        listener.Stop();
        manager.Stop();
    }

    [Fact]
    public async Task WaitUntilHealthyAsync_NothingListening_ReturnsFalseWithinTimeout()
    {
        using var manager = new ProcessManagerService();
        var (fileName, arguments) = LongRunningProcessCommand();
        manager.Start(fileName, arguments, workingDirectory: Environment.CurrentDirectory);
        var unusedPort = GetFreeTcpPort();

        var healthy = await manager.WaitUntilHealthyAsync(
            new Uri($"http://127.0.0.1:{unusedPort}/"), TimeSpan.FromMilliseconds(500));

        Assert.False(healthy);

        manager.Stop();
    }

    [Fact]
    public void Start_InvalidWorkingDirectory_ThrowsAndLeavesStateCleanForStop()
    {
        using var manager = new ProcessManagerService();
        var (fileName, arguments) = LongRunningProcessCommand();
        var nonExistentDirectory = Path.Combine(Environment.CurrentDirectory, Guid.NewGuid().ToString());

        Assert.ThrowsAny<Exception>(() => manager.Start(fileName, arguments, nonExistentDirectory));

        Assert.False(manager.IsRunning);
        manager.Stop();
    }

    [Fact]
    public async Task WaitUntilHealthyAsync_ProcessNotRunning_ReturnsFalseImmediately()
    {
        using var manager = new ProcessManagerService();

        var healthy = await manager.WaitUntilHealthyAsync(
            new Uri("http://127.0.0.1:1/"), TimeSpan.FromSeconds(5));

        Assert.False(healthy);
    }

    private static (string FileName, string Arguments) LongRunningProcessCommand()
    {
        return OperatingSystem.IsWindows()
            ? ("cmd.exe", "/c timeout /t 30")
            : ("sleep", "30");
    }

    private static int GetFreeTcpPort()
    {
        var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
