using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace BlazorTask.DebugHelper;

internal class Debugger
{
    private const string DebugCondition = "DEBUG";

    public static void Configure()
    {
        // Trace.Listeners.Clear();
        // Trace.Listeners.Add(new MyTraceListener());
    }

    [Conditional(DebugCondition)]
    public static void WriteMessage(string message, [CallerFilePath] string? path = null, [CallerMemberName] string? caller = null, [CallerLineNumber] int? line = null)
    {
        var source = new SourceInfo(path, caller, line);
        Console.WriteLine($"$DEBUG:{message} (source:{source.ToString()})");
    }

    [Conditional(DebugCondition)]
    public static void CheckPoint(string? message = null, [CallerFilePath] string? path = null, [CallerMemberName] string? caller = null, [CallerLineNumber] int? line = null)
    {
        var source = new SourceInfo(path, caller, line);
        if (message is null)
        {
            Console.WriteLine($"$DEBUG:CheckPoint {source.ToString()}");
        }
        else
        {
            Console.WriteLine($"$DEBUG:CheckPoint message:{message} {source.ToString()}");
        }
    }

    [Conditional(DebugCondition)]
    public static void Assert([DoesNotReturnIf(false)] bool condition, string? message = null, [CallerFilePath] string? path = null, [CallerMemberName] string? caller = null, [CallerLineNumber] int? line = null)
    {
        if (!condition)
        {
            var source = new SourceInfo(path, caller, line);
            if (message is null)
            {
                Console.WriteLine($"$DEBUG:Assertion failed:{source.ToString()}");
            }
            else
            {
                Console.WriteLine($"$DEBUG:Assertion failed:{message} {source.ToString()}");
            }
        }
        Debug.Assert(condition);
    }
}

/*
public class MyTraceListener : TraceListener
{
    public override bool IsThreadSafe => true;

    public override string Name { get => base.Name; set => base.Name = value; }

    public static void Write<T>(T? message)
    {
        Console.Write($"$DEBUG:{message?.ToString()}");
    }

    public static void WriteLine<T>(T? message)
    {
        Console.WriteLine($"DEBUG:{message?.ToString()}");
    }

    public override void Close()
    {
        base.Close();
    }

    public override void Fail(string? message)
    {
        throw new NotImplementedException();
    }

    public override void Fail(string? message, string? detailMessage)
    {
        throw new NotImplementedException();
    }

    public override void Flush()
    {
        base.Flush();
    }

    public override void TraceData(TraceEventCache? eventCache, string source, TraceEventType eventType, int id, object? data)
    {
        base.TraceData(eventCache, source, eventType, id, data);
    }

    public override void TraceData(TraceEventCache? eventCache, string source, TraceEventType eventType, int id, params object?[]? data)
    {
        base.TraceData(eventCache, source, eventType, id, data);
    }

    public override void TraceEvent(TraceEventCache? eventCache, string source, TraceEventType eventType, int id)
    {
        base.TraceEvent(eventCache, source, eventType, id);
    }

    public override void TraceEvent(TraceEventCache? eventCache, string source, TraceEventType eventType, int id, string? message)
    {
        base.TraceEvent(eventCache, source, eventType, id, message);
    }

    public override void TraceEvent(TraceEventCache? eventCache, string source, TraceEventType eventType, int id, string? format, params object?[]? args)
    {
        base.TraceEvent(eventCache, source, eventType, id, format, args);
    }

    public override void TraceTransfer(TraceEventCache? eventCache, string source, int id, string? message, Guid relatedActivityId)
    {
        base.TraceTransfer(eventCache, source, id, message, relatedActivityId);
    }

    public override void Write(object? o)
    {
        Write<object?>(o);
    }

    public override void Write(object? o, string? category)
    {
        Write($"[{category}]{o?.ToString()}");
    }

    public override void Write(string? message)
    {
        Write<string?>(message);
    }

    public override void Write(string? message, string? category)
    {
        Write<string?>($"[{category}]{message}");
    }

    public override void WriteLine(object? o)
    {
        WriteLine<object?>(o);
    }

    public override void WriteLine(object? o, string? category)
    {
        WriteLine<string?>($"[{category}]{o?.ToString()}");
    }

    public override void WriteLine(string? message)
    {
        WriteLine<string?>(message);
    }

    public override void WriteLine(string? message, string? category)
    {
        WriteLine<string?>($"[{category}]{message}");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
    }

    protected override string[]? GetSupportedAttributes()
    {
        return base.GetSupportedAttributes();
    }

    protected override void WriteIndent()
    {
        base.WriteIndent();
    }
}
*/

internal class SourceInfo
{
    public SourceInfo(string? filePath, string? caller, int? line)
    {
        FilePath = filePath;
        Caller = caller;
        Line = line;
    }

    public string? FilePath { get; set; }

    public string? Caller { get; set; }

    public int? Line { get; set; }

    public override string ToString()
    {
        var file = "null";
        if (FilePath is not null)
        {
            var index = FilePath.LastIndexOf('\\');
            if (index != -1)
            {
                file = FilePath[(index + 1)..];
            }
            else
            {
                file = FilePath;
            }
        }
        return $"{file}::{Caller ?? "null"},line:{Line?.ToString() ?? "unknown"}";
    }
}