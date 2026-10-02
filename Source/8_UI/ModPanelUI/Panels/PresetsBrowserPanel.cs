using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using HMUI;
using JetBrains.Annotations;

namespace EasyOffset;

internal class PresetsBrowserPanel : ReeUIComponentV2 {
    private CancellationTokenSource _request;
    private Task<OwnedFileWork.Result> _work;
    private Action<OwnedFileWork.Result> _completion;
    private bool _configBoundRequest;
    private bool _disposed;
    private long _generation;

    protected override void OnInitialize() {
        _disposed = false;
        PluginConfig.ConfigWasChangedEvent += OnConfigChanged;
        PluginConfig.IsModPanelVisibleChangedEvent += OnVisibilityChanged;
        PluginConfig.OnEnabledChange += OnEnabledChanged;
    }

    protected override void OnDispose() {
        _disposed = true;
        CancelRequest();
        PluginConfig.ConfigWasChangedEvent -= OnConfigChanged;
        PluginConfig.IsModPanelVisibleChangedEvent -= OnVisibilityChanged;
        PluginConfig.OnEnabledChange -= OnEnabledChanged;
    }

    private void OnDisable() => CancelRequest();
    private void OnConfigChanged() { if (_configBoundRequest) CancelRequest(); }
    private void OnEnabledChanged(bool enabled) { if (!enabled) CancelRequest(); }
    private void OnVisibilityChanged(bool visible) {
        if (!visible) CancelRequest();
        else if (PresetsBrowserActive) UpdatePresetsBrowserList();
    }

    private void CancelRequest() {
        _generation++;
        _request?.Cancel();
        _request?.Dispose();
        _request = null;
        _work = null;
        _completion = null;
        _configBoundRequest = false;
    }

    private void BeginRequest(Func<CancellationToken, Task<OwnedFileWork.Result>> prepare,
        Action<OwnedFileWork.Result> completion, bool configBound = false) {
        CancelRequest();
        _request = new CancellationTokenSource();
        _completion = completion;
        _configBoundRequest = configBound;
        _work = prepare(_request.Token);
    }

    private void Update() {
        if (_work == null || !_work.IsCompleted) return;
        if (_disposed || !IsInitialized || !isActiveAndEnabled || !PresetsBrowserActive ||
            !PluginConfig.Enabled || !PluginConfig.IsModPanelVisible || OwnedFileWork.IsStopping ||
            _request.IsCancellationRequested) {
            CancelRequest();
            return;
        }
        var work = _work;
        var completion = _completion;
        _request.Dispose();
        _request = null;
        _work = null;
        _completion = null;
        _configBoundRequest = false;
        try {
            var result = work.GetAwaiter().GetResult();
            if (result.Success) completion(result);
        } catch (Exception error) {
            Plugin.Log.Error(error);
        }
    }

    #region Events

    public void Activate(bool allowSave, bool allowLoad) {
        PresetsBrowserActive = true;
        PresetsBrowserSaveActive = allowSave;
        PresetsBrowserLoadActive = allowLoad;
        UpdatePresetsBrowserList();
    }

    public void Deactivate() {
        CancelRequest();
        if (!PresetsBrowserActive) return;
        PresetsBrowserActive = false;
        ModPanelUI.OpenMainPage();
    }

    #endregion

    #region Active

    private bool _presetsBrowserActive;

    [UIValue("pb-active"), UsedImplicitly]
    private bool PresetsBrowserActive {
        get => _presetsBrowserActive;
        set {
            if (_presetsBrowserActive.Equals(value)) return;
            _presetsBrowserActive = value;
            NotifyPropertyChanged();
        }
    }

    #endregion

    #region Preset file name

    private string _presetFileName = "NewPreset";

    [UIValue("pb-name-value"), UsedImplicitly]
    private string PresetFileName {
        get => _presetFileName;
        set {
            if (_presetFileName.Equals(value)) return;
            _presetFileName = value;
            if (_configBoundRequest) CancelRequest();
            NotifyPropertyChanged();
        }
    }

    [UIAction("pb-name-on-change"), UsedImplicitly]
    private void PresetFilenameOnChange(string value) {
        for (var i = 0; i < _storedConfigPresets.Count; i++) {
            if (_storedConfigPresets[i].Name != value) continue;
            _presetsBrowserList.TableView.SelectCellWithIdx(i);
            return;
        }

        _presetsBrowserList.TableView.ClearSelection();
    }

    #endregion

    #region List

    private List<StoredConfigPreset> _storedConfigPresets = new();

    [UIComponent("pb-list"), UsedImplicitly]
    private CustomListTableData _presetsBrowserList = default;

    [UIAction("pb-list-select-cell"), UsedImplicitly]
    private void PresetsBrowserListSelectCell(TableView tableView, int row) {
        if (row >= _storedConfigPresets.Count) return;
        var selectedConfig = _storedConfigPresets[row];
        PresetFileName = selectedConfig.Name;
    }

    [UIAction("pb-refresh-on-click"), UsedImplicitly]
    private void PresetsBrowserRefreshOnClick() {
        UpdatePresetsBrowserList();
    }

    private void UpdatePresetsBrowserList() {
        _presetsBrowserList.Data.Clear();
        _storedConfigPresets.Clear();
        _presetsBrowserList.TableView.ReloadData();
        _presetsBrowserList.TableView.ClearSelection();
        BeginRequest(ConfigPresetsStorage.ReadCatalogAsync, PublishPresetsBrowserList);
    }

    private void PublishPresetsBrowserList(OwnedFileWork.Result result) {
        _storedConfigPresets = ConfigPresetsStorage.BuildCatalog(result);

        foreach (var storedConfigPreset in _storedConfigPresets) {
            _presetsBrowserList.Data.Add(new CustomListTableData.CustomCellInfo(
                    PresetUtils.GetPresetCellString(storedConfigPreset)
                )
            );
        }

        _presetsBrowserList.TableView.ReloadData();
        _presetsBrowserList.TableView.ClearSelection();
    }

    #endregion

    #region Cancel button

    [UIAction("pb-cancel-on-click"), UsedImplicitly]
    private void PresetsBrowserCancelOnClick() {
        Deactivate();
    }

    #endregion

    #region Save button

    [UIValue("pb-save-hint"), UsedImplicitly]
    private string _presetsBrowserSaveHint { get; } = "Save current preset to file" +
                                             "\n" +
                                             "\n<color=red>This action will overwrite existing files</color>";

    private bool _presetsBrowserSaveActive;

    [UIValue("pb-save-active"), UsedImplicitly]
    private bool PresetsBrowserSaveActive {
        get => _presetsBrowserSaveActive;
        set {
            if (_presetsBrowserSaveActive.Equals(value)) return;
            _presetsBrowserSaveActive = value;
            NotifyPropertyChanged();
        }
    }

    [UIAction("pb-save-on-click"), UsedImplicitly]
    private void PresetsBrowserSaveOnClick() {
        if (_configBoundRequest || !PresetsBrowserActive || !PresetsBrowserSaveActive) return;
        var name = PresetFileName;
        BeginRequest(token => ConfigPresetsStorage.SaveCurrentPresetAsync(name, token), _ => Deactivate(), true);
    }

    #endregion

    #region Load button

    private bool _presetsBrowserLoadActive;

    [UIValue("pb-load-hint"), UsedImplicitly]
    private string _presetsBrowserLoadHint { get; } = "Load preset from file" +
                                             "\n" +
                                             "\n<color=red>Any unsaved changes will be lost</color>";

    [UIValue("pb-load-active"), UsedImplicitly]
    private bool PresetsBrowserLoadActive {
        get => _presetsBrowserLoadActive;
        set {
            if (_presetsBrowserLoadActive.Equals(value)) return;
            _presetsBrowserLoadActive = value;
            NotifyPropertyChanged();
        }
    }

    [UIAction("pb-load-on-click"), UsedImplicitly]
    private void PresetsBrowserLoadOnClick() {
        if (_configBoundRequest || !PresetsBrowserActive || !PresetsBrowserLoadActive) return;
        var name = PresetFileName;
        PluginConfig.CreateUndoStep($"Load preset: {PresetFileName}");
        BeginRequest(token => ConfigPresetsStorage.ReadPresetAsync(name, token), result => {
            if (!PresetUtils.ReadPresetFromJson(result.Json, out var preset)) return;
            var generation = _generation;
            PluginConfig.ApplyPreset(preset);
            if (generation == _generation && PresetsBrowserActive) Deactivate();
        }, true);
    }

    #endregion
}
