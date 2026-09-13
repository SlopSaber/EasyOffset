using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.XR;

namespace EasyOffset;

internal static class VRControllerGetControllerOffsetPatch {
    public static void ApplyPatch(Harmony harmony) {
        var targetMethod = AccessTools.Method(typeof(VRController), "TryGetControllerOffset",
            new[] {
                typeof(IVRPlatformHelper),
                typeof(VRControllerTransformOffset),
                typeof(XRNode),
                typeof(Pose).MakeByRefType()
            }
        );
        var prefix = AccessTools.Method(typeof(VRControllerGetControllerOffsetPatch), nameof(PrefixCurrent));

        if (targetMethod == null) {
            targetMethod = AccessTools.Method(typeof(VRController), "TryGetControllerOffset",
                new[] {
                    typeof(IVRPlatformHelper),
                    typeof(VRControllerTransformOffset),
                    typeof(XRNode).MakeByRefType(),
                    typeof(Pose).MakeByRefType()
                }
            );
            prefix = AccessTools.Method(typeof(VRControllerGetControllerOffsetPatch), nameof(PrefixLegacy));
        }

        if (targetMethod == null || prefix == null)
            return;

        harmony.Patch(targetMethod, new HarmonyMethod(prefix));
    }

    [UsedImplicitly]
    private static bool PrefixCurrent(
        IVRPlatformHelper vrPlatformHelper,
        VRControllerTransformOffset transformOffset,
        XRNode node,
        out Pose poseOffset
    ) {
        poseOffset = Pose.identity;
        return PluginConfig.IsDeviceless && !PluginConfig.EnabledForDeviceless;
    }

    [UsedImplicitly]
    private static bool PrefixLegacy(
        IVRPlatformHelper vrPlatformHelper,
        VRControllerTransformOffset transformOffset,
        ref XRNode node,
        out Pose poseOffset
    ) {
        poseOffset = Pose.identity;
        return PluginConfig.IsDeviceless && !PluginConfig.EnabledForDeviceless;
    }
}
