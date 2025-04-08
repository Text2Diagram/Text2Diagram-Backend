using System.Diagnostics;

namespace Text2Diagram_Backend;

public class NodeServerBackgroundService : BackgroundService
{
    private readonly ILogger<NodeServerBackgroundService> logger;
    private Process nodeProcess = new();

    public NodeServerBackgroundService(
        ILogger<NodeServerBackgroundService> logger)
    {
        this.logger = logger;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "node",
            Arguments = "validationServer.js",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        nodeProcess = new Process { StartInfo = startInfo };
        nodeProcess.OutputDataReceived += (sender, args) => logger.LogInformation(args.Data);
        nodeProcess.ErrorDataReceived += (sender, args) => logger.LogError(args.Data);

        nodeProcess.Start();
        nodeProcess.BeginOutputReadLine();
        nodeProcess.BeginErrorReadLine();

        logger.LogInformation("Validation server started.");

        return base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        if (nodeProcess != null && !nodeProcess.HasExited)
        {
            nodeProcess.Kill();
            nodeProcess.Dispose();
            logger.LogInformation("Validation server stopped.");
        }

        return base.StopAsync(cancellationToken);

    }
}
