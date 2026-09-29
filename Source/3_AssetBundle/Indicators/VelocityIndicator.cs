using UnityEngine;

namespace EasyOffset {
    public class VelocityIndicator : MonoBehaviour {
        #region Serialized

        [SerializeField] private Material pointMaterial = default;
        [SerializeField] private MeshRenderer pointMeshRenderer = default;

        [SerializeField] private Color minimalVelocityColor = default;
        [SerializeField] private Color maximalVelocityColor = default;

        [SerializeField] private float minimalVelocity = default;
        [SerializeField] private float maximalVelocity = default;

        [SerializeField] private float smoothFactor = 10f;

        #endregion

        #region ShaderProperties

        private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");

        #endregion

        #region Start

        private Material _materialInstance;

        private void Start() {
            _materialInstance = Instantiate(pointMaterial);
            pointMeshRenderer.material = _materialInstance;
        }

        private void OnDestroy() {
            if (_materialInstance != null) Destroy(_materialInstance);
        }

        #endregion

        #region Update

        private bool _hasPreviousPosition;
        private Vector3 _previousPosition;
        private float _smoothedVelocity;

        private void Update() {
            var currentPosition = transform.position;

            if (_hasPreviousPosition) {
                var velocity = (currentPosition - _previousPosition).magnitude / Time.deltaTime;
                if (float.IsInfinity(velocity) || float.IsNaN(velocity)) return;
                
                _smoothedVelocity = Mathf.Lerp(_smoothedVelocity, velocity, Time.deltaTime * smoothFactor);
                var t = Mathf.InverseLerp(minimalVelocity, maximalVelocity, _smoothedVelocity);
                var color = Color.Lerp(minimalVelocityColor, maximalVelocityColor, t);
                _materialInstance.SetColor(ColorPropertyId, color);
            }

            _previousPosition = currentPosition;
            _hasPreviousPosition = true;
        }

        #endregion
    }
}
