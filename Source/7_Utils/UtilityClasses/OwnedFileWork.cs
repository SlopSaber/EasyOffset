using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace EasyOffset;

internal static class OwnedFileWork {
    internal static readonly object FileGate = new();
    private static readonly object QueueGate = new();
    private static readonly CancellationTokenSource Shutdown = new();
    private static Task _tail = Task.CompletedTask;

    internal static bool IsStopping => Shutdown.IsCancellationRequested;
    internal static void Stop() => Shutdown.Cancel();

    internal enum Operation { Catalog, ReadJson, WriteText, WriteBytes }

    internal sealed class Request {
        internal readonly Operation Kind;
        internal readonly string Path;
        internal readonly string Text;
        internal readonly byte[] Bytes;
        internal readonly CancellationToken Token;

        internal Request(Operation kind, string path, CancellationToken token, string text = null, byte[] bytes = null) {
            Kind = kind;
            Path = path;
            Token = token;
            Text = text;
            Bytes = bytes;
        }
    }

    internal sealed class FileData {
        internal string Path;
        internal JObject Json;
    }

    internal sealed class Result {
        internal bool Success;
        internal JObject Json;
        internal FileData[] Files;
        internal Exception Error;
    }

    internal static Task<Result> Queue(Request request) {
        lock (QueueGate) {
            var task = _tail.ContinueWith((_, state) => Run((Request)state), request,
                CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
            // Retain the physical work even when its owner stops polling the result.
            _tail = task.ContinueWith(completed => { _ = completed.Exception; },
                CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
            return task;
        }
    }

    private static void CheckCancellation(Request request) {
        request.Token.ThrowIfCancellationRequested();
        Shutdown.Token.ThrowIfCancellationRequested();
    }

    private static Result Run(Request request) {
        try {
            lock (FileGate) {
                CheckCancellation(request);
                switch (request.Kind) {
                    case Operation.Catalog:
                        Directory.CreateDirectory(request.Path);
                        var paths = Directory.GetFiles(request.Path, "*.json");
                        var files = new FileData[paths.Length];
                        for (var i = 0; i < paths.Length; i++) {
                            CheckCancellation(request);
                            JObject json = null;
                            try { json = JObject.Parse(File.ReadAllText(paths[i])); } catch (Exception) { }
                            files[i] = new FileData { Path = paths[i], Json = json };
                        }
                        CheckCancellation(request);
                        return new Result { Success = true, Files = files };
                    case Operation.ReadJson:
                        var parsed = JObject.Parse(File.ReadAllText(request.Path));
                        CheckCancellation(request);
                        return new Result { Success = true, Json = parsed };
                    case Operation.WriteText:
                    case Operation.WriteBytes:
                        Write(request);
                        return new Result { Success = true };
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        } catch (Exception error) {
            return new Result { Error = error };
        }
    }

    private static void Write(Request request) {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(request.Path));
        var temporaryPath = request.Path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try {
            if (request.Kind == Operation.WriteText) {
                File.WriteAllText(temporaryPath, request.Text, Encoding.UTF8);
            } else {
                using var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                for (var offset = 0; offset < request.Bytes.Length;) {
                    CheckCancellation(request);
                    var count = Math.Min(65536, request.Bytes.Length - offset);
                    stream.Write(request.Bytes, offset, count);
                    offset += count;
                }
            }
            CheckCancellation(request);
            if (File.Exists(request.Path)) File.Replace(temporaryPath, request.Path, null);
            else File.Move(temporaryPath, request.Path);
        } finally {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }
}
