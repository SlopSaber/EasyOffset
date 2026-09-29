using JetBrains.Annotations;
using UnityEngine;

namespace EasyOffset {
    public class ControllerModel : MonoBehaviour {
        #region Serialized

        // Preserve the embedded prefab serialization layout while upstream controller meshes remain disabled.
#pragma warning disable CS0414

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


#pragma warning restore CS0414
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
            // Upstream disabled controller meshes in 1.30.2. Keep serialized fields for bundle compatibility.
            return null;
        }

        #endregion
    }
}
