using System.Text.Json;
using System.Text.Json.Serialization;
using AiUsage.Core.Budget;
using AiUsage.Infrastructure.Providers;

namespace AiUsage.Infrastructure.Persistence;

internal sealed class BudgetJsonFile(string directory)
{
    internal static readonly JsonSerializerOptions Options = new()
    {
        MaxDepth = 16, UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectNullableAnnotations = true, RespectRequiredConstructorParameters = true
    };
    internal string PathFor(string name) => Path.Combine(directory, name);
    internal void Check(string path)
    {
        ProviderStatePaths.CheckDirectory(directory);
        ProviderStatePaths.CheckFile(path);
    }

    internal async Task<StoreRead<T>> ReadAsync<T>(string name, int maximumBytes, Func<T> empty,
        Action<T> validate, CancellationToken token)
    {
        var path = PathFor(name);
        Check(path);
        if (!File.Exists(path)) return new(empty());
        T value;
        try
        {
            await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
            if (stream.Length > maximumBytes) throw new InvalidDataException("Store is too large.");
            value = await JsonSerializer.DeserializeAsync<T>(stream, Options, token).ConfigureAwait(false)
                ?? throw new InvalidDataException("Empty store.");
            validate(value);
        }
        catch (Exception error) when (error is JsonException or InvalidDataException)
        {
            token.ThrowIfCancellationRequested();
            if (Directory.EnumerateFiles(directory, name + ".quarantine-*").Take(3).Count() >= 3)
                throw new IOException("Store recovery capacity reached.");
            var quarantine = path + ".quarantine-" + Guid.NewGuid().ToString("N");
            Check(path);
            Check(quarantine);
            File.Move(path, quarantine);
            return new(empty(), true);
        }
        return new(value);
    }

    internal async Task WriteAsync<T>(string name, T value, int maximumBytes, CancellationToken token)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, Options);
        if (bytes.Length > maximumBytes) throw new IOException("Store capacity reached.");
        var path = PathFor(name);
        var staged = path + ".stage-" + Guid.NewGuid().ToString("N");
        Check(path);
        Check(staged);
        try
        {
            await using (var stream = new FileStream(staged, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096,
                FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await stream.WriteAsync(bytes, token).ConfigureAwait(false);
                await stream.FlushAsync(token).ConfigureAwait(false);
                stream.Flush(flushToDisk: true);
            }
            token.ThrowIfCancellationRequested();
            Check(path);
            Check(staged);
            File.Move(staged, path, overwrite: true);
        }
        finally
        {
            Check(staged);
            if (File.Exists(staged)) File.Delete(staged);
        }
    }
}
