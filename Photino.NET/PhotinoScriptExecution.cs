#nullable enable

using System.Collections.Concurrent;
using System.Text.Json;

namespace Photino.NET;

public partial class PhotinoWindow
{
    private const string ExecuteScriptCallbackMessageType = "__photino_execute_script_result";
    private readonly ConcurrentDictionary<string, TaskCompletionSource<object?>> _scriptExecutionTasks = new();

    /// <summary>
    /// Executes JavaScript in the current page and waits for a JSON-serializable result.
    /// </summary>
    /// <param name="script">The JavaScript body to execute. Use a return statement to return a value.</param>
    /// <returns>The JSON-serializable result converted to a .NET primitive, list, dictionary, or <c>null</c>.</returns>
    /// <exception cref="ApplicationException">Thrown when the window is not initialized, the script fails, or the method is called synchronously on the UI thread.</exception>
    public object? ExecuteScript(string script)
    {
        if (Environment.CurrentManagedThreadId == _managedThreadId)
            throw new ApplicationException("ExecuteScript cannot be called synchronously on the Photino UI thread. Use ExecuteScriptAsync instead.");

        return ExecuteScriptAsync(script).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Executes JavaScript in the current page and asynchronously waits for a JSON-serializable result.
    /// </summary>
    /// <param name="script">The JavaScript body to execute. Use a return statement to return a value.</param>
    /// <param name="cancellationToken">A token that cancels waiting for the script result.</param>
    /// <returns>A task that completes with the JSON-serializable result converted to a .NET primitive, list, dictionary, or <c>null</c>.</returns>
    /// <exception cref="ApplicationException">Thrown when the window is not initialized or the script fails.</exception>
    public async Task<object?> ExecuteScriptAsync(string script, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(script);

        if (_nativeInstance == IntPtr.Zero)
            throw new ApplicationException("ExecuteScriptAsync cannot be called until after the Photino window is initialized.");

        cancellationToken.ThrowIfCancellationRequested();

        var id = Guid.NewGuid().ToString("N");
        var task = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _scriptExecutionTasks[id] = task;

        using var cancellationRegistration = cancellationToken.Register(() =>
        {
            if (_scriptExecutionTasks.TryRemove(id, out var pendingTask))
                pendingTask.TrySetCanceled(cancellationToken);
        });

        try
        {
            var wrappedScript = CreateScriptExecutionWrapper(id, script);
            var executed = false;
            Invoke(() => executed = Photino_ExecuteScript(_nativeInstance, wrappedScript));
            if (!executed)
                throw new ApplicationException("ExecuteScriptAsync cannot execute because the browser control is not ready.");

            return await task.Task.ConfigureAwait(false);
        }
        catch
        {
            _scriptExecutionTasks.TryRemove(id, out _);
            throw;
        }
    }

    private static string CreateScriptExecutionWrapper(string id, string script)
    {
        var serializedId = JsonSerializer.Serialize(id);
        var serializedType = JsonSerializer.Serialize(ExecuteScriptCallbackMessageType);
        var serializedScript = JsonSerializer.Serialize(script);

        return $$"""
            (() => {
                const id = {{serializedId}};
                const type = {{serializedType}};
                const script = {{serializedScript}};
                const send = payload => window.external.sendMessage(JSON.stringify(payload));
                const serializeResult = value => {
                    if (typeof value === "undefined")
                        return { resultKind: "undefined", result: null };

                    const json = JSON.stringify(value);
                    if (typeof json === "undefined")
                        return { resultKind: "undefined", result: null };

                    return { resultKind: "json", result: JSON.parse(json) };
                };
                const serializeError = error => ({
                    name: error && error.name ? error.name : "Error",
                    message: error && error.message ? error.message : String(error),
                    stack: error && error.stack ? error.stack : null
                });

                Promise.resolve()
                    .then(() => new Function(script)())
                    .then(result => {
                        const serializedResult = serializeResult(result);
                        send({
                            type,
                            id,
                            success: true,
                            resultKind: serializedResult.resultKind,
                            result: serializedResult.result
                        });
                    })
                    .catch(error => {
                        send({
                            type,
                            id,
                            success: false,
                            error: serializeError(error)
                        });
                    });
            })();
            """;
    }

    private bool TryHandleScriptExecutionCallback(string message)
    {
        try
        {
            using var document = JsonDocument.Parse(message);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty("type", out var type) ||
                type.GetString() != ExecuteScriptCallbackMessageType)
                return false;

            if (!root.TryGetProperty("id", out var idProperty) ||
                idProperty.GetString() is not { } id ||
                !_scriptExecutionTasks.TryRemove(id, out var task))
                return true;

            if (root.TryGetProperty("success", out var success) &&
                success.ValueKind == JsonValueKind.True)
            {
                if (root.TryGetProperty("resultKind", out var resultKind) &&
                    resultKind.GetString() == "undefined")
                {
                    task.TrySetResult(null);
                    return true;
                }

                task.TrySetResult(
                    root.TryGetProperty("result", out var result)
                        ? ConvertScriptResult(result)
                        : null);
                return true;
            }

            task.TrySetException(new ApplicationException(GetScriptExecutionErrorMessage(root)));
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string GetScriptExecutionErrorMessage(JsonElement root)
    {
        if (!root.TryGetProperty("error", out var error))
            return "JavaScript execution failed.";

        var name = error.TryGetProperty("name", out var errorName)
            ? errorName.GetString()
            : null;
        var message = error.TryGetProperty("message", out var errorMessage)
            ? errorMessage.GetString()
            : null;

        return string.IsNullOrWhiteSpace(name)
            ? message ?? "JavaScript execution failed."
            : $"{name}: {message ?? "JavaScript execution failed."}";
    }

    private static object? ConvertScriptResult(JsonElement result)
    {
        return result.ValueKind switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.False => false,
            JsonValueKind.True => true,
            JsonValueKind.String => result.GetString(),
            JsonValueKind.Number => result.TryGetInt64(out var longValue) ? longValue : result.GetDouble(),
            JsonValueKind.Array => result.EnumerateArray().Select(ConvertScriptResult).ToList(),
            JsonValueKind.Object => result.EnumerateObject().ToDictionary(x => x.Name, x => ConvertScriptResult(x.Value)),
            _ => null
        };
    }
}
