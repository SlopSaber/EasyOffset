using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace EasyOffset {
    internal static class XRUtils {
        public static float GetRefreshRate() {
            var displays = new List<XRDisplaySubsystem>();
            SubsystemManager.GetSubsystems(displays);
            foreach (var display in displays) {
                if (display.running && display.TryGetDisplayRefreshRate(out var refreshRate) &&
                    refreshRate > 0 && !float.IsInfinity(refreshRate)) {
                    return refreshRate;
                }
            }

            // Callers clamp to their minimum sample capacity when no display is active.
            return 0;
        }
    }
}
