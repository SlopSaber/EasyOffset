using JetBrains.Annotations;
using UnityEngine;

namespace EasyOffset {
    public class ControllerModel : MonoBehaviour {
        #region Serialized

        [SerializeField]
        private GameObject oculusCV1Left = default;

        [SerializeField]
        private GameObject oculusCV1Right = default;

        [SerializeField]
        private GameObject oculusQuest2Left = default;

        [SerializeField]
        private GameObject oculusQuest2Right = default;

        [SerializeField]
        private GameObject riftSLeft = default;

        [SerializeField]
        private GameObject riftSRight = default;

        [SerializeField]
        private GameObject valveIndexLeft = default;

        [SerializeField]
        private GameObject valveIndexRight = default;

        [SerializeField]
        private GameObject pico4Left = default;

        [SerializeField]
        private GameObject pico4Right = default;

        [SerializeField]
        private GameObject piMaxSwordLeft = default;

        [SerializeField]
        private GameObject piMaxSwordRight = default;

        [SerializeField]
        private GameObject viveTracker2 = default;

        [SerializeField]
        private GameObject viveTracker3 = default;

        [SerializeField]
        private GameObject tundraTracker = default;

        [SerializeField]
        private GameObject vive = default;


        #endregion

        #region Properties

        private ControllerType _controllerType = ControllerType.None;

        public ControllerType ControllerType {
            get => _controllerType;
            set {
                if (_controllerType == value) return;
                _controllerType = value;
                UpdateModel();
            }
        }

        private Hand _hand;

        public Hand Hand {
            get => _hand;
            set {
                if (_hand == value) return;
                _hand = value;
                UpdateModel();
            }
        }

        private void UpdateModel() {
            var prefab = GetPrefab(_controllerType, _hand is Hand.Left);

            if (prefab != null) {
                SetPrefab(prefab);
            } else {
                DestroyInstance();
            }
        }

        #endregion

        #region SetVisible

        public void SetVisible(bool value) {
            gameObject.SetActive(value);
        }

        public void SetPoseOffset(Pose pose) {
            transform.SetLocalPositionAndRotation(pose.position, pose.rotation);
        }

        #endregion

        #region Instance

        private GameObject _instance;
        private bool _spawned;

        private void SetPrefab(GameObject prefab) {
            DestroyInstance();
            _instance = Instantiate(prefab, transform, false);
            _spawned = true;
        }

        private void DestroyInstance() {
            if (!_spawned) return;
            Destroy(_instance);
            _spawned = false;
        }

        #endregion

        #region Utils

        [CanBeNull]
        private GameObject GetPrefab(ControllerType controllerType, bool isLeft) {
            return controllerType switch {
                ControllerType.None => null,
                ControllerType.ValveIndex => isLeft ? valveIndexLeft : valveIndexRight,
                ControllerType.OculusQuest2 => isLeft ? oculusQuest2Left : oculusQuest2Right,
                ControllerType.OculusRiftS => isLeft ? riftSLeft : riftSRight,
                ControllerType.OculusCV1 => isLeft ? oculusCV1Left : oculusCV1Right,
                ControllerType.HtcVive => vive,
                ControllerType.Pico4 => isLeft ? pico4Left : pico4Right,
                ControllerType.PiMaxSword => isLeft ? piMaxSwordLeft : piMaxSwordRight,
                ControllerType.ViveTracker2 => viveTracker2,
                ControllerType.ViveTracker3 => viveTracker3,
                ControllerType.TundraTracker => tundraTracker,
                _ => null
            };
        }

        #endregion
    }
}
