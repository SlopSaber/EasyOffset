using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using System.Threading;
using IPA.Utilities;
using UnityEngine.Networking;

namespace EasyOffset;

internal static class VideoCache {
    #region Cache

    private static readonly string CacheDirectory = Path.Combine(UnityGame.UserDataPath, "EasyOffset", "Cache");

    private static readonly Dictionary<string, (string Url, string Path)> Cache = new();
    private static readonly Dictionary<string, string> RequestedUrls = new();

    #endregion

    #region GetVideoCoroutine

    public static IEnumerator GetVideoCoroutine(string key, string url, IWebRequestHandler<string> handler,
        CancellationToken token = default) {
        if (token.IsCancellationRequested || OwnedFileWork.IsStopping) yield break;
        RequestedUrls[key] = url;
        if (Cache.TryGetValue(key, out var cached) && cached.Url == url) {
            handler.OnRequestFinished(cached.Path);
            yield break;
        }

        var request = new VideoRequestDescriptor(url);
        var capture = new ResponseCapture(handler, token);
        yield return NetworkingUtils.ProcessRequestCoroutine(request, capture, 1, 300);
        if (capture.Data == null || token.IsCancellationRequested || OwnedFileWork.IsStopping) yield break;

        var path = Path.Combine(CacheDirectory, $"{key}-{Guid.NewGuid():N}.mp4");
        var work = OwnedFileWork.Queue(new OwnedFileWork.Request(OwnedFileWork.Operation.WriteBytes,
            path, token, bytes: capture.Data));
        yield return new UnityEngine.WaitUntil(() => work.IsCompleted || token.IsCancellationRequested || OwnedFileWork.IsStopping);
        if (token.IsCancellationRequested || OwnedFileWork.IsStopping) yield break;
        var result = work.GetAwaiter().GetResult();
        if (!result.Success) {
            handler.OnRequestFailed($"Internal error: {result.Error?.Message}");
            yield break;
        }
        if (RequestedUrls[key] == url) Cache[key] = (url, path);
        try {
            handler.OnRequestFinished(path);
        } catch (Exception error) {
            Plugin.Log.Debug($"Video response exception: {error}");
            handler.OnRequestFailed($"Internal error: {error.Message}");
        }
    }

    #endregion

    #region VideoRequestDescriptor

    private class VideoRequestDescriptor : IWebRequestDescriptor<byte[]> {
        private readonly string _url;

        public VideoRequestDescriptor(string url) {
            _url = url;
        }

        public UnityWebRequest CreateWebRequest() {
            return UnityWebRequest.Get(_url);
        }

        public byte[] ParseResponse(UnityWebRequest request) => request.downloadHandler.data;
    }

    private sealed class ResponseCapture : IWebRequestHandler<byte[]> {
        private readonly IWebRequestHandler<string> _handler;
        private readonly CancellationToken _token;
        internal byte[] Data { get; private set; }

        internal ResponseCapture(IWebRequestHandler<string> handler, CancellationToken token) {
            _handler = handler;
            _token = token;
        }

        private bool IsCurrent => !_token.IsCancellationRequested && !OwnedFileWork.IsStopping;
        public void OnRequestStarted() { if (IsCurrent) _handler.OnRequestStarted(); }
        public void OnRequestFinished(byte[] data) { if (IsCurrent) Data = data; }
        public void OnRequestFailed(string reason) { if (IsCurrent) _handler.OnRequestFailed(reason); }
        public void OnRequestProgress(float uploadProgress, float downloadProgress, float overallProgress) {
            if (IsCurrent) _handler.OnRequestProgress(uploadProgress, downloadProgress, overallProgress);
        }
    }

    #endregion
}
