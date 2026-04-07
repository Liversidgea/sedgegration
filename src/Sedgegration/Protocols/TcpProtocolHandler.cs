using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;
using Sedgegration.Models;
using Sedgegration.Workflows;

namespace Sedgegration.Protocols;

public class TcpProtocolHandler(WorkflowEngine engine, ILogger<TcpProtocolHandler> logger) : IProtocolHandler
{
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private Task? _acceptLoop;
    private string? _workflowKey;

    public string Name => "TCP";
    public bool IsRunning => _listener is not null;

    public Task StartAsync(ProtocolConfig config, CancellationToken ct)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _listener = new TcpListener(IPAddress.Any, config.Port);
        _listener.Start();

        // Capture any workflow override supplied by the protocol config
        if (config.Options != null && config.Options.TryGetValue("Workflow", out var wf) && wf is not null)
            _workflowKey = wf.ToString();

        logger.LogInformation("TCP listener started on port {Port}", config.Port);
        _acceptLoop = AcceptLoopAsync(_cts.Token);

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken ct)
    {
        _cts?.Cancel();
        _listener?.Stop();

        if (_acceptLoop is not null)
        {
            try { await _acceptLoop; }
            catch (OperationCanceledException) { }
        }

        _listener = null;
        logger.LogInformation("TCP listener stopped");
    }

    private async Task AcceptLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var client = await _listener!.AcceptTcpClientAsync(ct);
                _ = HandleClientAsync(client, ct);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error accepting TCP client");
            }
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken ct)
    {
        using (client)
        {
            try
            {
                await using var stream = client.GetStream();
                using var ms = new MemoryStream();
                var buffer = new byte[4096];
                int bytesRead;

                // Read until no more data available
                client.ReceiveTimeout = 5000;
                while ((bytesRead = await stream.ReadAsync(buffer, ct)) > 0)
                {
                    ms.Write(buffer, 0, bytesRead);
                    if (!stream.DataAvailable) break;
                }

                var metadata = new Dictionary<string, object>
                {
                    ["RemoteEndpoint"] = client.Client.RemoteEndPoint?.ToString() ?? "unknown"
                };

                // Determine workflow key to use (override or remote endpoint)
                var workflowKey = _workflowKey ?? metadata["RemoteEndpoint"]?.ToString() ?? string.Empty;

                var results = await engine.ProcessAsync(workflowKey, ms.ToArray(), metadata, ct);

                var allValid = results.TrueForAll(r => r.IsValid);
                var response = allValid
                    ? "OK"u8.ToArray()
                    : Encoding.UTF8.GetBytes($"ERROR: {string.Join("; ", results.SelectMany(r => r.Errors))}");

                await stream.WriteAsync(response, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error handling TCP client");
            }
        }
    }
}
