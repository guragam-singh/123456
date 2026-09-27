using UnityEngine;

namespace LeafGame
{
    public sealed class LeafCameraController : MonoBehaviour
    {
        [Header("References")]
        public Camera targetCamera;
        public LeafController leaf;

        [Header("Normal Gameplay / Before Final Fall")]
        public Vector3 closeupPosition = new Vector3(0f, 0f, -10f);
        public float closeupSize = 5.5f;

        [Header("Final Fall - First Background")]
        [Tooltip("How far the camera zooms in during the first part of the fall.")]
        public float fallZoomSize = 3.6f;

        [Tooltip("How quickly the camera follows the leaf during the fall.")]
        public float followSmoothing = 2.5f;

        [Tooltip("How long the camera takes to reach the close-up framing.")]
        public float framingBlendSeconds = 5f;

        [Tooltip("Horizontal offset between the camera and leaf during the first fall.")]
        public float fallFollowOffsetX = -0.6f;

        [Tooltip("Vertical offset between the camera and leaf during the first fall.")]
        public float fallFollowOffsetY = 0.8f;

        [Tooltip("Lowest Y position the camera can reach during the first fall.")]
        public float groundCameraY = -12f;

        [Header("Alternate Background")]
        [Tooltip("Fixed camera Y position while travelling across the alternate background.")]
        public float alternateCameraY = 0f;

        [Tooltip("Camera zoom while travelling across the alternate background.")]
        public float alternateSize = 3.6f;

        [Tooltip("Horizontal offset between camera and leaf in the alternate background.")]
        public float alternateFollowOffsetX = 0f;

        [Tooltip("How smoothly the camera follows the leaf horizontally.")]
        public float alternateFollowSmoothing = 2.5f;

        [Header("Wind Shake")]
        public float gustDisplacement = 0.045f;
        public float gustDecay = 2.5f;

        private float gust;

        private bool cinematic;
        private bool alternateFlight;
        private bool frozen;

        private float cinematicStart;
        private float alternateStart;

        private Vector3 initialPosition;
        private float initialSize;

        public void OnWind(WindResult result)
        {
            if (result == WindResult.Miss)
                gust = 1f;
        }

        /// <summary>
        /// Starts the first cinematic fall through the original background.
        /// The camera follows the leaf and gradually zooms in.
        /// </summary>
        public void BeginFollow()
        {
            cinematic = true;
            alternateFlight = false;
            frozen = false;

            cinematicStart = Time.unscaledTime;

            initialPosition = transform.position;
            initialSize = targetCamera.orthographicSize;
        }

        /// <summary>
        /// Called while the screen is black.
        /// Places the camera at the starting position of the alternate background.
        /// </summary>
        public void SetupAlternateBackground(
            Vector3 cameraPosition,
            float cameraSize)
        {
            cinematic = false;
            alternateFlight = false;

            transform.position = new Vector3(
                cameraPosition.x,
                cameraPosition.y,
                transform.position.z
            );

            targetCamera.orthographicSize = cameraSize;
        }

        /// <summary>
        /// Starts the horizontal journey through the alternate background.
        /// The camera follows the leaf's X position but keeps a fixed Y.
        /// </summary>
        public void BeginAlternateFlight()
        {
            cinematic = false;
            alternateFlight = true;
            frozen = false;

            alternateStart = Time.unscaledTime;
        }

        public void Freeze()
        {
            frozen = true;
        }

        private void LateUpdate()
        {
            if (frozen)
                return;

            if (!targetCamera || !leaf)
                return;

            float dt = Time.unscaledDeltaTime;

            if (!cinematic && !alternateFlight)
            {
                UpdateNormalCamera(dt);
            }
            else if (cinematic)
            {
                UpdateFirstFallCamera(dt);
            }
            else if (alternateFlight)
            {
                UpdateAlternateCamera(dt);
            }
        }

        private void UpdateNormalCamera(float dt)
        {
            gust = Mathf.MoveTowards(
                gust,
                0f,
                dt * gustDecay
            );

            Vector3 shake =
                new Vector3(
                    Mathf.Sin(Time.unscaledTime * 5f),
                    Mathf.Sin(Time.unscaledTime * 3.7f),
                    0f
                ) *
                (gust * gustDisplacement);

            transform.position = closeupPosition + shake;

            targetCamera.orthographicSize = closeupSize;
        }

        private void UpdateFirstFallCamera(float dt)
        {
            float elapsed =
                Time.unscaledTime - cinematicStart;

            float blend = Mathf.SmoothStep(
                0f,
                1f,
                elapsed / Mathf.Max(
                    0.01f,
                    framingBlendSeconds
                )
            );

            // Follow the leaf downward.
            Vector3 target = leaf.transform.position;

            target.x += fallFollowOffsetX;
            target.y += fallFollowOffsetY;
            target.z = transform.position.z;

            // Prevent the camera from going below the bottom
            // of the first background.
            target.y = Mathf.Max(
                groundCameraY,
                target.y
            );

            Vector3 desiredPosition =
                Vector3.Lerp(
                    initialPosition,
                    target,
                    blend
                );

            transform.position = Vector3.Lerp(
                transform.position,
                desiredPosition,
                1f - Mathf.Exp(
                    -followSmoothing * dt
                )
            );

            // IMPORTANT:
            // 5.5 -> 3.6 means zoom IN.
            targetCamera.orthographicSize =
                Mathf.Lerp(
                    initialSize,
                    fallZoomSize,
                    blend
                );
        }

        private void UpdateAlternateCamera(float dt)
        {
            // Only follow X.
            // We deliberately DON'T follow the leaf's Y because
            // the leaf needs to visibly sway and fall independently
            // inside the frame.

            float targetX =
                leaf.transform.position.x +
                alternateFollowOffsetX;

            Vector3 target =
                new Vector3(
                    targetX,
                    alternateCameraY,
                    transform.position.z
                );

            transform.position = Vector3.Lerp(
                transform.position,
                target,
                1f - Mathf.Exp(
                    -alternateFollowSmoothing * dt
                )
            );

            targetCamera.orthographicSize =
                Mathf.Lerp(
                    targetCamera.orthographicSize,
                    alternateSize,
                    1f - Mathf.Exp(-3f * dt)
                );
        }
    }
}