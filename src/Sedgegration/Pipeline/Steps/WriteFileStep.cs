using Microsoft.Extensions.Logging;
using Sedgegration.IO;
using Sedgegration.Models;

namespace Sedgegration.Pipeline.Steps;

/// <summary>
/// Writes payload content or metadata to a file using DirectoryFileWriter.
/// Config options:
/// - directory: optional override directory (defaults to writer base dir)
/// - filename: required filename or template (supports {id}, {timestamp}, {protocol})
/// - source: one of "raw", "response", or "metadata:<key>" (default: raw)
/// - encoding: "utf8" or "base64" (default: utf8 for text)
/// </summary>
public class WriteFileStep : IPayloadStep
{
    private readonly string _name = "WriteFile";
    public string Name => _name;

    private readonly Dictionary<string, object> _config;
    private readonly DirectoryFileWriter _writer;
    private readonly ILogger<WriteFileStep> _logger;

    public WriteFileStep(Dictionary<string, object> config, DirectoryFileWriter writer, ILogger<WriteFileStep> logger)
    {
        _config = config ?? new();
        _writer = writer;
        _logger = logger;
    }

    public async Task ExecuteAsync(PayloadContext context, CancellationToken ct)
    {
        // Resolve filename template
        var filename = GetConfigString("filename");
        if (string.IsNullOrWhiteSpace(filename))
        {
            context.Errors.Add("WriteFile: missing filename in configuration");
            return;
        }

        // Replace common tokens
        filename = filename.Replace("{id}", context.Metadata.ContainsKey("Id") ? context.Metadata["Id"].ToString() ?? string.Empty : Guid.NewGuid().ToString("N"));
        filename = filename.Replace("{timestamp}", DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ"));
        filename = filename.Replace("{protocol}", context.ProtocolSource ?? string.Empty);

        var source = GetConfigString("source")?.ToLowerInvariant() ?? "raw";
        byte[] bytesToWrite;
        string? textToWrite = null;

        try
        {
            if (source.StartsWith("metadata:", StringComparison.OrdinalIgnoreCase))
            {
                var key = source.Substring("metadata:".Length);
                if (context.Metadata.TryGetValue(key, out var val) && val is not null)
                {
                    textToWrite = val.ToString();
                    bytesToWrite = System.Text.Encoding.UTF8.GetBytes(textToWrite);
                }
                else
                {
                    context.Errors.Add($"WriteFile: metadata key '{key}' not found");
                    return;
                }
            }
            else if (source == "response")
            {
                if (context.Metadata.TryGetValue("Response", out var resp) && resp is not null)
                {
                    if (resp is byte[] rb)
                    {
                        bytesToWrite = rb;
                    }
                    else
                    {
                        textToWrite = resp.ToString();
                        bytesToWrite = System.Text.Encoding.UTF8.GetBytes(textToWrite);
                    }
                }
                else
                {
                    context.Errors.Add("WriteFile: no Response available in context metadata");
                    return;
                }
            }
            else // raw
            {
                bytesToWrite = context.RawData ?? Array.Empty<byte>();
                // If config expects text, also set textToWrite
                textToWrite = System.Text.Encoding.UTF8.GetString(bytesToWrite);
            }

            // Determine encoding option
            var encoding = GetConfigString("encoding")?.ToLowerInvariant() ?? "utf8";
            var targetFile = filename;

            if (encoding == "base64")
            {
                var b64 = Convert.ToBase64String(bytesToWrite);
                await _writer.WriteTextAsync(targetFile, b64, ct);
            }
            else
            {
                // write bytes if binary (raw or resp bytes) when text likely okay
                if (textToWrite is not null && encoding == "utf8")
                {
                    await _writer.WriteTextAsync(targetFile, textToWrite, ct);
                }
                else
                {
                    await _writer.WriteBytesAsync(targetFile, bytesToWrite, ct);
                }
            }

            _logger.LogInformation("WriteFile: wrote {File} (size={Size})", targetFile, bytesToWrite.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WriteFile: failed to write file");
            context.Errors.Add($"WriteFile: exception: {ex.Message}");
        }
    }

    private string? GetConfigString(string key)
    {
        if (_config.TryGetValue(key, out var v))
        {
            if (v is System.Text.Json.JsonElement je) return je.GetString();
            return v?.ToString();
        }
        return null;
    }
}

