using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

namespace Microservice.Logging;

public sealed class CustomJsonConsoleFormatter : ConsoleFormatter, IDisposable
{
    public const string FormatterName = "customJson";

    private readonly IDisposable? _optionsReloadToken;
    private CustomJsonConsoleFormatterOptions _options;

    public CustomJsonConsoleFormatter(IOptionsMonitor<CustomJsonConsoleFormatterOptions> options)
        : base(FormatterName)
    {
        _options = options.CurrentValue;
        _optionsReloadToken = options.OnChange(updatedOptions => _options = updatedOptions);
    }

    public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider, TextWriter textWriter)
    {
        var message = logEntry.Formatter?.Invoke(logEntry.State, logEntry.Exception);
        if (string.IsNullOrWhiteSpace(message) && logEntry.Exception is null)
        {
            return;
        }

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("timestamp", DateTimeOffset.UtcNow);
            writer.WriteString("level", logEntry.LogLevel.ToString());
            writer.WriteString("category", logEntry.Category);
            writer.WriteNumber("eventId", logEntry.EventId.Id);
            writer.WriteString("message", message);

            if (logEntry.Exception is not null)
            {
                writer.WriteString("exception", logEntry.Exception.ToString());
            }

            if (_options.IncludeScopes && scopeProvider is not null)
            {
                writer.WriteStartArray("scopes");
                scopeProvider.ForEachScope((scope, jsonWriter) => jsonWriter.WriteStringValue(scope?.ToString()), writer);
                writer.WriteEndArray();
            }

            writer.WriteEndObject();
            writer.Flush();
        }

        textWriter.WriteLine(Encoding.UTF8.GetString(stream.ToArray()));
    }

    public void Dispose()
    {
        _optionsReloadToken?.Dispose();
    }
}
