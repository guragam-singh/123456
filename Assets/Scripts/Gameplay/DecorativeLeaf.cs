using UnityEngine;

namespace LeafGame
{
    public sealed class DecorativeLeaf : MonoBehaviour
    {
        public SpriteRenderer spriteRenderer;
        public float fallSpeed = 0.7f;
        public float drift = 0.35f;
        public float rotationSpeed = 32f;
        public float lifetime = 22f;
        private float elapsed;
        private float phase;
        private void Awake() { phase = Random.value * Mathf.PI * 2f; }
        private void Update()
        {
            elapsed += Time.unscaledDeltaTime;
            transform.position += new Vector3(Mathf.Sin(elapsed + phase) * drift, -fallSpeed, 0f) * Time.unscaledDeltaTime;
            transform.Rotate(0, 0, rotationSpeed * Time.unscaledDeltaTime);
            if (elapsed >= lifetime) Destroy(gameObject);
        }
    }
}
