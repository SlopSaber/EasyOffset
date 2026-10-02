using System;
using System.Threading;
using BeatSaberMarkupLanguage.Attributes;
using HMUI;
using JetBrains.Annotations;
using UnityEngine;

namespace EasyOffset;

internal class ReeVideoPlayer : ReeUIComponentV2, IWebRequestHandler<string> {
    #region Components

    [UIComponent("hover-area"), UsedImplicitly]
    private RectTransform _hoverArea = default;

    [UIComponent("screen"), UsedImplicitly]
    private ImageView _screen = default;

    [UIValue("download-progress"), UsedImplicitly]
    private DownloadProgress _downloadProgress;

    [UIValue("play-button"), UsedImplicitly]
    private PlayButton _playButton;

    [UIValue("rewind-slider"), UsedImplicitly]
    private RewindSlider _rewindSlider;

    [UIValue("funny"), UsedImplicitly]
    private Funny _funny;

    private void Awake() {
        _downloadProgress = Instantiate<DownloadProgress>(transform);
        _playButton = Instantiate<PlayButton>(transform);
        _rewindSlider = Instantiate<RewindSlider>(transform);
        _funny = Instantiate<Funny>(transform);
    }

    #endregion

    #region OnInitialize / OnDispose

    private VideoRenderer _videoRenderer;
    private HoverController _hoverController;
    private CancellationTokenSource _videoRequest;
    private bool _disposed;

    protected override void OnInitialize() {
        _disposed = false;
        _videoRenderer = VideoRenderer.FromImageView(_screen);
        _hoverController = _hoverArea.gameObject.AddComponent<HoverController>();
        _hoverController.HoverStateChangedEvent += OnHoverStateChanged;
        _videoRenderer.OnTimeUpdated += OnVideoTimeUpdated;
        _videoRenderer.OnVideoEnded += OnVideoEnded;
        _playButton.OnClickEvent += OnButtonClick;
        _rewindSlider.OnClickEvent += OnRewind;
        UpdateVisuals();
    }

    protected override void OnDispose() {
        _disposed = true;
        RetireVideoRequest();
        _hoverController.HoverStateChangedEvent -= OnHoverStateChanged;
        _videoRenderer.OnTimeUpdated -= OnVideoTimeUpdated;
        _videoRenderer.OnVideoEnded -= OnVideoEnded;
        _playButton.OnClickEvent -= OnButtonClick;
        _rewindSlider.OnClickEvent -= OnRewind;
    }

    #endregion

    #region Interaction

    private bool _isFunny;

    public void SetVideo(string key, string url, bool isFunny) {
        RetireVideoRequest();
        _isFunny = isFunny;
        _videoRenderer.Stop();
        StopAllCoroutines();
        SetState(State.Uninitialized);
        var source = _videoRequest = new CancellationTokenSource();
        StartCoroutine(VideoCache.GetVideoCoroutine(key, url, new CurrentRequestHandler(this, source), source.Token));
    }

    private void OnDisable() => RetireVideoRequest();

    private void Update() {
        if (_videoRequest != null && (_disposed || OwnedFileWork.IsStopping || !_screen || !_screen.gameObject.activeInHierarchy))
            RetireVideoRequest();
    }

    private void RetireVideoRequest() {
        _videoRequest?.Cancel();
        _videoRequest?.Dispose();
        _videoRequest = null;
        StopAllCoroutines();
    }

    private sealed class CurrentRequestHandler : IWebRequestHandler<string> {
        private readonly ReeVideoPlayer _owner;
        private readonly CancellationTokenSource _source;
        private readonly CancellationToken _token;

        internal CurrentRequestHandler(ReeVideoPlayer owner, CancellationTokenSource source) {
            _owner = owner;
            _source = source;
            _token = source.Token;
        }

        private bool IsCurrent => _owner && !_owner._disposed && ReferenceEquals(_owner._videoRequest, _source) &&
            !_token.IsCancellationRequested && !OwnedFileWork.IsStopping && _owner.isActiveAndEnabled &&
            _owner._screen && _owner._screen.gameObject.activeInHierarchy;

        public void OnRequestStarted() { if (IsCurrent) _owner.OnRequestStarted(); }
        public void OnRequestFinished(string result) { if (IsCurrent) _owner.OnRequestFinished(result); }
        public void OnRequestFailed(string reason) { if (IsCurrent) _owner.OnRequestFailed(reason); }
        public void OnRequestProgress(float uploadProgress, float downloadProgress, float overallProgress) {
            if (IsCurrent) _owner.OnRequestProgress(uploadProgress, downloadProgress, overallProgress);
        }
    }

    #endregion

    #region Events

    private void OnButtonClick() {
        switch (_currentState) {
            case State.Playing:
                _videoRenderer.Pause();
                SetState(State.Paused);
                break;
            case State.Paused:
                _videoRenderer.Play();
                SetState(State.Playing);
                break;
            case State.Finished:
                _videoRenderer.Play();
                SetState(State.Playing);
                break;
            default: return;
        }
    }

    private void OnRewind(float time) {
        _videoRenderer.Seek(time);
    }

    private void OnVideoTimeUpdated(float currentSeconds, float totalSeconds) {
        _rewindSlider.SetTime(currentSeconds, totalSeconds);
    }

    private void OnVideoEnded() {
        SetState(State.Finished);
    }

    private void OnHoverStateChanged(bool isHovered) {
        _isHovered = isHovered;
        UpdateVisuals();
    }

    public void OnRequestStarted() {
        SetState(State.Downloading);
    }

    public void OnRequestFinished(string result) {
        _videoRenderer.Prepare($"file://{result}");
        _videoRenderer.Play();
        SetState(State.Playing);
    }

    public void OnRequestFailed(string reason) {
        SetState(State.Failed);
    }

    public void OnRequestProgress(float uploadProgress, float downloadProgress, float overallProgress) {
        _downloadProgress.Progress = downloadProgress;
    }

    #endregion

    #region State

    private event Action<State> StateChangedEvent;

    private State _currentState = State.Uninitialized;

    private void SetState(State newState) {
        if (_currentState == newState) return;
        _currentState = newState;
        UpdateVisuals();
        StateChangedEvent?.Invoke(newState);
    }

    public void AddStateListener(Action<State> handler) {
        StateChangedEvent += handler;
        handler?.Invoke(_currentState);
    }

    public void RemoveStateListener(Action<State> handler) {
        StateChangedEvent -= handler;
    }

    public enum State {
        Uninitialized,
        Downloading,
        Playing,
        Paused,
        Finished,
        Failed
    }

    #endregion

    #region UpdateVisuals

    private bool _isHovered;

    private void UpdateVisuals() {
        switch (_currentState) {
            case State.Uninitialized:
                _downloadProgress.SetActive(false);
                _playButton.SetActive(false);
                _rewindSlider.SetActive(false);
                _funny.SetActive(false);
                break;
            case State.Downloading:
                _downloadProgress.Label = "Loading";
                _downloadProgress.SetActive(true);
                _playButton.SetActive(false);
                _rewindSlider.SetActive(false);
                _funny.SetActive(false);
                break;
            case State.Playing:
                _downloadProgress.SetActive(false);
                _playButton.SetState(PlayButton.State.Pause);
                _playButton.SetActive(!_isFunny && _isHovered);
                _rewindSlider.SetActive(!_isFunny && _isHovered);
                _funny.SetActive(_isFunny && _isHovered);
                break;
            case State.Paused:
            case State.Finished:
                _downloadProgress.SetActive(false);
                _playButton.SetState(PlayButton.State.Play);
                _playButton.SetActive(!_isFunny && _isHovered);
                _rewindSlider.SetActive(!_isFunny && _isHovered);
                _funny.SetActive(_isFunny && _isHovered);
                break;
            case State.Failed:
                _downloadProgress.Label = "Download failed";
                _downloadProgress.SetActive(true);
                _playButton.SetActive(false);
                _rewindSlider.SetActive(false);
                _funny.SetActive(false);
                break;
            default: return;
        }
    }

    #endregion
}
