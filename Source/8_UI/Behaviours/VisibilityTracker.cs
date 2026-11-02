using UnityEngine;

namespace EasyOffset;

internal class VisibilityTracker : MonoBehaviour {
    private bool _applicationIsQuitting;

    private void OnApplicationQuit() {
        _applicationIsQuitting = true;
    }

    private void OnEnable() {
        PluginConfig.IsModPanelVisible = true;
    }

    private void OnDisable() {
        if (_applicationIsQuitting) return;
        PluginConfig.IsModPanelVisible = false;
        ConfigFileData.Instance.Changed();
    }
}
