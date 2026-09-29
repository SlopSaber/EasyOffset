using TMPro;
using UnityEngine;

namespace EasyOffset {
    public class OrthonormalBasis : MonoBehaviour {
        #region Serialized

        [SerializeField] private Transform pointerTransform = default;
        [SerializeField] private GameObject axlesVisuals = default;
        [SerializeField] private GameObject pointerVisuals = default;
        [SerializeField] private TextMeshPro textMesh = default;
        [SerializeField] private Transform textRoot = default;

        [SerializeField] private Material pointerMaterial = default;
        [SerializeField] private MeshRenderer pointerMeshRenderer = default;

        [SerializeField] private Material xArrowMaterial = default;
        [SerializeField] private MeshRenderer xArrowMeshRenderer = default;

        [SerializeField] private Material yArrowMaterial = default;
        [SerializeField] private MeshRenderer yArrowMeshRenderer = default;

        [SerializeField] private Material zArrowMaterial = default;
        [SerializeField] private MeshRenderer zArrowMeshRenderer = default;

        #endregion

        #region ShaderProperties

        private static readonly int ScalePropertyId = Shader.PropertyToID("_Scale");
        private static readonly int AlphaPropertyId = Shader.PropertyToID("_Alpha");

        #endregion

        #region Start

        private Material _pointerMaterialInstance;
        private Material _xArrowMaterialInstance;
        private Material _yArrowMaterialInstance;
        private Material _zArrowMaterialInstance;
        private bool _isReady;

        private void Start() {
            _pointerMaterialInstance = Instantiate(pointerMaterial);
            pointerMeshRenderer.material = _pointerMaterialInstance;

            _xArrowMaterialInstance = Instantiate(xArrowMaterial);
            xArrowMeshRenderer.material = _xArrowMaterialInstance;

            _yArrowMaterialInstance = Instantiate(yArrowMaterial);
            yArrowMeshRenderer.material = _yArrowMaterialInstance;

            _zArrowMaterialInstance = Instantiate(zArrowMaterial);
            zArrowMeshRenderer.material = _zArrowMaterialInstance;

            _targetAlpha = _currentAlpha = NoFocusAlpha;
            _isReady = true;
        }

        private void OnDestroy() {
            if (_pointerMaterialInstance != null) Destroy(_pointerMaterialInstance);
            if (_xArrowMaterialInstance != null) Destroy(_xArrowMaterialInstance);
            if (_yArrowMaterialInstance != null) Destroy(_yArrowMaterialInstance);
            if (_zArrowMaterialInstance != null) Destroy(_zArrowMaterialInstance);
        }

        #endregion

        #region Update

        private float _targetAlpha;
        private float _currentAlpha;

        private void Update() {
            var t = Time.deltaTime * 10f;
            _currentAlpha = Mathf.Lerp(_currentAlpha, _targetAlpha, t);

            _pointerMaterialInstance.SetFloat(AlphaPropertyId, _currentAlpha);
            _xArrowMaterialInstance.SetFloat(AlphaPropertyId, _currentAlpha);
            _yArrowMaterialInstance.SetFloat(AlphaPropertyId, _currentAlpha);
            _zArrowMaterialInstance.SetFloat(AlphaPropertyId, _currentAlpha);
        }

        #endregion

        #region SetFocus

        private const float NoFocusAlpha = 0.2f;
        private const float FocusAlpha = 1.0f;

        public void SetFocus(bool value) {
            _targetAlpha = value ? FocusAlpha : NoFocusAlpha;
        }

        #endregion

        #region SetCoordinates

        public void SetCoordinates(Vector3 coordinates) {
            if (!_isReady) return;
            _pointerMaterialInstance.SetVector(ScalePropertyId, coordinates);
            pointerTransform.localScale = coordinates;

            UpdateTextString(coordinates);
            UpdateTextPosition(coordinates);
        }

        #endregion

        #region SetVisible

        public void SetVisible(bool isBasisVisible, bool isPointerVisible) {
            axlesVisuals.SetActive(isBasisVisible);
            pointerVisuals.SetActive(isPointerVisible);
        }

        #endregion

        #region Text

        public void SetTextLookAt(Vector3 worldPosition) {
            textMesh.transform.LookAt(worldPosition, Vector3.up);
        }

        private void UpdateTextString(Vector3 coordinates) {
            var xString = (coordinates.x * 100.0).ToString("0.0");
            var yString = (coordinates.y * 100.0).ToString("0.0");
            var zString = (coordinates.z * 100.0).ToString("0.0");

            textMesh.text = $"<color=red>{xString}</color>  <color=green>{yString}</color>  <color=blue>{zString}</color>";
            textMesh.alignment = TextAlignmentOptions.Midline;
        }

        private void UpdateTextPosition(Vector3 coordinates) {
            textRoot.localPosition = coordinates;
        }

        #endregion
    }
}
