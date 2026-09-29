using UnityEngine;

namespace EasyOffset {
    public class GizmosController : MonoBehaviour {
        #region Serialized

        [SerializeField] private Transform controllerTransform = default;
        [SerializeField] private Transform saberTransform = default;
        [SerializeField] private Transform pivotTransform = default;
        [SerializeField] private Transform pivotPositionOnly = default;
        [SerializeField] private Transform referenceRotationVisuals = default;

        [SerializeField] private Pivot pivot = default;
        [SerializeField] private SphericalBasis sphericalBasis = default;
        [SerializeField] private OrthonormalBasis orthonormalBasis = default;
        [SerializeField] private ControllerModel controllerModel = default;
        [SerializeField] private SwingPreview swingPreview = default;
        [SerializeField] private PreciseGimbal preciseGimbal = default;

        #endregion

        #region SetHand

        public void SetHand(Hand hand) {
            sphericalBasis.SetHand(hand);
            controllerModel.Hand = hand;
        }

        #endregion

        #region SetVisibility

        private bool _isSwingPreviewVisible;
        private bool _isReferenceVisible;
        private bool _hasReference;

        public void SetVisibility(
            bool isPivotVisible,
            bool isSphericalBasisVisible,
            bool isOrthonormalBasisVisible,
            bool isOrthonormalBasisPointerVisible,
            bool isControllerModelVisible,
            bool isSwingPreviewVisible,
            bool isPreciseGimbalVisible,
            bool isReferenceVisible
        ) {
            pivot.SetVisible(isPivotVisible);
            sphericalBasis.SetVisible(isSphericalBasisVisible);
            orthonormalBasis.SetVisible(isOrthonormalBasisVisible, isOrthonormalBasisPointerVisible);
            controllerModel.SetVisible(isControllerModelVisible);
            swingPreview.SetVisible(isSwingPreviewVisible);
            preciseGimbal.SetVisible(isPreciseGimbalVisible);

            _isSwingPreviewVisible = isSwingPreviewVisible;
            UpdateSwingPreviewVisibility();

            _isReferenceVisible = isReferenceVisible;
            UpdateReferenceVisibility();
        }

        private void UpdateSwingPreviewVisibility() {
            swingPreview.SetVisible(_isSwingPreviewVisible && !(_orthoFocused || _sphericalFocused));
        }

        private void UpdateReferenceVisibility() {
            referenceRotationVisuals.gameObject.SetActive(_isReferenceVisible && _hasReference);
        }

        #endregion

        #region SetConfigValues

        public void SetConfigValues(
            Vector3 pivotPosition,
            Quaternion saberRotation,
            float zOffset,
            bool hasReference,
            Quaternion referenceRotation
        ) {
            var saberTranslation = pivotPosition + saberRotation * new Vector3(0, 0, zOffset);
            saberTransform.localPosition = saberTranslation;
            saberTransform.localRotation = saberRotation;
            pivotTransform.localPosition = new Vector3(0, 0, -zOffset);
            pivotPositionOnly.localPosition = pivotPosition;

            orthonormalBasis.SetCoordinates(pivotPosition);
            sphericalBasis.SetRotations(saberRotation, hasReference, referenceRotation);
            referenceRotationVisuals.localRotation = referenceRotation;
            preciseGimbal.SetValues(pivotPosition, saberRotation);

            _hasReference = hasReference;
            UpdateReferenceVisibility();
        }

        #endregion

        #region SetControllerTransform

        public void SetControllerTransform(Vector3 position, Quaternion rotation) {
            controllerTransform.SetPositionAndRotation(position, rotation);
        }

        #endregion

        #region SetFocus

        private bool _orthoFocused;
        private bool _sphericalFocused;

        public void SetOrthonormalBasisFocus(bool value) {
            if (_orthoFocused == value) return;
            _orthoFocused = value;

            orthonormalBasis.SetFocus(value);
            UpdateSwingPreviewVisibility();
        }

        public void SetSphericalBasisFocus(bool value) {
            if (_sphericalFocused == value) return;
            _sphericalFocused = value;

            sphericalBasis.SetFocus(value);
            UpdateSwingPreviewVisibility();
        }

        #endregion

        #region SetCameraPosition

        public void SetCameraPosition(Vector3 cameraWorldPosition) {
            orthonormalBasis.SetTextLookAt(cameraWorldPosition);
        }

        #endregion

        #region Zoom

        public void Zoom(float magnitude) {
            sphericalBasis.Zoom(magnitude);
        }

        #endregion

        #region SetControllerType

        public void SetControllerType(ControllerType controllerType) {
            controllerModel.ControllerType = controllerType;
        }

        #endregion
    }
}
