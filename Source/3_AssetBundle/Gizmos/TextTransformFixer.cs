using UnityEngine;

namespace EasyOffset {
    public class TextTransformFixer : MonoBehaviour {
        [SerializeField] private Vector3 worldOffset = default;
        [SerializeField] private Transform target = default;

        private void Update() {
            transform.position = target.position + worldOffset;
        }
    }
}
