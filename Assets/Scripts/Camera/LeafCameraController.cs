using UnityEngine;

namespace LeafGame
{
    public sealed class LeafCameraController : MonoBehaviour
    {
        public Camera targetCamera;
        public LeafController leaf;
        public Vector3 closeupPosition = new Vector3(0f, 0f, -10f);
        public float closeupSize = 5.5f;
        public float gustDisplacement = 0.045f;
        public float gustDecay = 2.5f;
        public Vector3 followOffset = new Vector3(-0.6f, 0.8f, -10f);
        public float groundCameraY = -12f;
        public float finalSize = 5.8f;
        public float followSmoothing = 2.5f;
        public float framingBlendSeconds = 7f;
        private float gust;
        private bool cinematic;
        private bool frozen;
        private float cinematicStart;
        private Vector3 initialPosition;
        public void OnWind(WindResult result) { if (result == WindResult.Miss) gust = 1f; }
        public void BeginFollow()
        {
            cinematic = true; frozen = false; cinematicStart = Time.unscaledTime;
            initialPosition = transform.position;
        }
        public void Freeze() { frozen = true; }
        private void LateUpdate()
        {
            if (frozen) return;
            float dt = Time.unscaledDeltaTime;
            if (!cinematic)
            {
                gust = Mathf.MoveTowards(gust, 0f, dt * gustDecay);
                transform.position = closeupPosition + new Vector3(Mathf.Sin(Time.unscaledTime * 5f), Mathf.Sin(Time.unscaledTime * 3.7f), 0) * (gust * gustDisplacement);
                targetCamera.orthographicSize = closeupSize;
            }
            else
            {
                float blend = Mathf.SmoothStep(0, 1, (Time.unscaledTime - cinematicStart) / Mathf.Max(0.01f, framingBlendSeconds));
                Vector3 target = leaf.transform.position + followOffset;
                target.y = Mathf.Max(groundCameraY, target.y);
                target = Vector3.Lerp(initialPosition, target, blend);
                transform.position = Vector3.Lerp(transform.position, target, 1f - Mathf.Exp(-followSmoothing * dt));
                targetCamera.orthographicSize = Mathf.Lerp(closeupSize, finalSize, blend);
            }
        }
    }
}
