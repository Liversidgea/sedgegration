using System.IO;

namespace Sedgegration.IO;

/// <summary>
/// Helper for safely writing files into a directory.
/// Writes to a temp file and atomically moves to the target path to avoid partial writes.
/// </summary>
public class DirectoryFileWriter
{
    private readonly string _directory;

    public DirectoryFileWriter(string directory)
    {
        _directory = directory ?? throw new ArgumentNullException(nameof(directory));
        if (!Directory.Exists(_directory)) Directory.CreateDirectory(_directory);
    }

    public string BaseDirectory => _directory;

    /// <summary>
    /// Atomically writes text content to the target filename (relative name or full path).
    /// </summary>
    public async Task WriteTextAsync(string fileName, string content, CancellationToken ct = default)
    {
        var targetPath = GetFullPath(fileName);
        var targetDir = Path.GetDirectoryName(targetPath) ?? _directory;
        if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

        var tempPath = Path.Combine(targetDir, ".tmp." + Guid.NewGuid().ToString("N"));

        await File.WriteAllTextAsync(tempPath, content, ct).ConfigureAwait(false);
        // Ensure written to disk by opening and flushing
        using (var fs = new FileStream(tempPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None, 4096, FileOptions.WriteThrough))
        {
            await fs.FlushAsync(ct).ConfigureAwait(false);
        }

        // Replace existing file atomically
        File.Move(tempPath, targetPath, true);
    }

    /// <summary>
    /// Atomically writes binary content to the target filename (relative name or full path).
    /// </summary>
    public async Task WriteBytesAsync(string fileName, byte[] bytes, CancellationToken ct = default)
    {
        var targetPath = GetFullPath(fileName);
        var targetDir = Path.GetDirectoryName(targetPath) ?? _directory;
        if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

        var tempPath = Path.Combine(targetDir, ".tmp." + Guid.NewGuid().ToString("N"));

        await File.WriteAllBytesAsync(tempPath, bytes, ct).ConfigureAwait(false);
        using (var fs = new FileStream(tempPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None, 4096, FileOptions.WriteThrough))
        {
            await fs.FlushAsync(ct).ConfigureAwait(false);
        }

        File.Move(tempPath, targetPath, true);
    }

    private string GetFullPath(string fileName)
    {
        if (Path.IsPathRooted(fileName)) return fileName;
        return Path.Combine(_directory, fileName);
    }
}
