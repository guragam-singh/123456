using System;
using UnityEngine;

namespace LeafGame
{
    public enum LeafState { Attached, Unstable, Detaching, Falling, Landed }

    public sealed class LeafController : MonoBehaviour
    {
        [Header("Replaceable presentation")]
        public Transform stemPivot;
        public SpriteRenderer spriteRenderer;
        public Animator animator;
        public AudioManager audioManager;

        [Header("Hanging motion (degrees)")]
        public float restingAngle = -90f;

        // TEMPORARY visual rotation limit.
        // Leaf can rotate only 50 degrees either side of restingAngle.
        public float maxRotationFromRest = 50f;

        [Min(0.1f)] public float spring = 8f;
        [Min(0.1f)] public float damping = 3.6f;
        public float idleAmplitude = 2.8f;
        public float idleFrequency = 1.5f;
        public float unstableAmplitude = 57f;
        public float trembleAmplitude = 2.5f;
        public float impulseMultiplier = 2.8f;

        [Header("Response")]
        [Range(0f, 1f)] public float missInstability = 0.08f;
        [Range(0f, 1f)] public float perfectRecovery = 0.055f;
        [Range(0f, 1f)] public float goodRecovery = 0.018f;
        [Min(0f)] public float passiveRecovery = 0.002f;
        [Range(0f, 1f)] public float cumulativeStrainPerMiss = 0.009f;
        [Range(0f, 1f)] public float maximumStrain = 0.65f;

        [Header("Independent, non-cinematic detachment")]
        public bool protectUntilFinal;
        public float ordinaryFallSpeed = 0.8f;
        public float ordinaryGroundY = -15f;
        public float ordinaryFallTurnSpeed = 24f;
        public float rainSensitivity = 1.2f;
        public float rainInertia = 0.45f;
        public float rustleInterval = 2.4f;

        public LeafState State { get; private set; }
        public float CurrentAngle { get; private set; }
        public float TargetAngle { get; private set; }
        public float Instability { get; private set; }
        public float RainWeight { get; set; }

        public event Action Detached;

        private float velocity;
        private float strain;
        private float phase;
        private float nextRustle;
        private bool scriptedFall;

        private static readonly int StateParameter =
            Animator.StringToHash("State");

        private void Awake()
        {
            CurrentAngle = restingAngle;
            TargetAngle = restingAngle;

            ApplyRotation();

            SetState(LeafState.Attached);
        }

        private void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);

            phase += dt;

            if (audioManager)
                audioManager.SetIntensity(Instability);

            if (State == LeafState.Attached || State == LeafState.Unstable)
            {
                Instability = Mathf.Clamp(
                    Instability - passiveRecovery * dt,
                    strain,
                    1f
                );

                TargetAngle =
                    restingAngle +
                    Mathf.Sin(phase * idleFrequency) *
                    (idleAmplitude + unstableAmplitude * Instability);

                float inertia =
                    1f + RainWeight * rainInertia;

                velocity +=
                    (TargetAngle - CurrentAngle) *
                    spring /
                    inertia *
                    dt;

                velocity *= Mathf.Exp(
                    -damping *
                    Mathf.Lerp(1f, 0.45f, Instability) *
                    dt /
                    inertia
                );

                CurrentAngle += velocity * dt;

                // -------------------------------------------------
                // TEMPORARY ROTATION LIMIT
                // -------------------------------------------------
                float minAngle =
                    restingAngle - maxRotationFromRest;

                float maxAngle =
                    restingAngle + maxRotationFromRest;

                CurrentAngle =
                    Mathf.Clamp(
                        CurrentAngle,
                        minAngle,
                        maxAngle
                    );

                // Stop the spring from continuing to push
                // against the rotation limit.
                if (CurrentAngle <= minAngle && velocity < 0f)
                    velocity = 0f;

                if (CurrentAngle >= maxAngle && velocity > 0f)
                    velocity = 0f;

                // Keep the old detachment code disabled for now.
                // We'll fix the actual falling system later.

                float tremble =
                    Mathf.Sin(phase * 23f) *
                    trembleAmplitude *
                    Instability *
                    Instability;

                float visualAngle =
                    Mathf.Clamp(
                        CurrentAngle + tremble,
                        minAngle,
                        maxAngle
                    );

                stemPivot.localRotation =
                    Quaternion.Euler(
                        0f,
                        0f,
                        visualAngle
                    );

                if (State != LeafState.Falling)
                {
                    SetState(
                        Instability > 0.45f
                            ? LeafState.Unstable
                            : LeafState.Attached
                    );
                }

                if (Instability > 0.5f &&
                    phase >= nextRustle)
                {
                    nextRustle = phase + rustleInterval;

                    if (audioManager)
                    {
                        audioManager.PlaySFX(
                            SFXType.LeafRustle,
                            Mathf.Lerp(
                                0.25f,
                                0.65f,
                                Instability
                            )
                        );
                    }
                }
            }
            else if (State == LeafState.Falling && !scriptedFall)
            {
                transform.position +=
                    new Vector3(
                        Mathf.Sin(phase) * 0.15f,
                        -ordinaryFallSpeed,
                        0f
                    ) * dt;

                stemPivot.Rotate(
                    0f,
                    0f,
                    ordinaryFallTurnSpeed * dt
                );

                if (transform.position.y <= ordinaryGroundY)
                    SetState(LeafState.Landed);
            }
        }

        public void ReceiveWind(
            WindResult result,
            int direction,
            float strength
        )
        {
            if (State != LeafState.Attached &&
                State != LeafState.Unstable)
                return;

            float sensitivity =
                1f + RainWeight * rainSensitivity;

            if (result == WindResult.Miss)
            {
                strain =
                    Mathf.Min(
                        maximumStrain,
                        strain + cumulativeStrainPerMiss
                    );

                Instability =
                    Mathf.Clamp01(
                        Instability + missInstability
                    );

                velocity +=
                    direction *
                    strength *
                    impulseMultiplier *
                    sensitivity;
            }
            else
            {
                Instability =
                    Mathf.Max(
                        strain,
                        Instability -
                        (
                            result == WindResult.Perfect
                                ? perfectRecovery
                                : goodRecovery
                        )
                    );

                velocity *=
                    result == WindResult.Perfect
                        ? 0.2f
                        : 0.6f;

                velocity +=
                    direction *
                    strength *
                    (
                        result == WindResult.Perfect
                            ? -0.25f
                            : 0.65f
                    );
            }

            if (audioManager)
            {
                audioManager.PlaySFX(
                    SFXType.LeafRustle,
                    0.25f + 0.5f * Instability
                );
            }
        }

        public void AddInstability(float amount)
        {
            Instability =
                Mathf.Clamp01(
                    Instability + amount
                );
        }

        public void DetachNaturally()
        {
            scriptedFall = false;

            SetState(LeafState.Falling);

            Detached?.Invoke();
        }

        public void BeginScriptedDetachment()
        {
            scriptedFall = true;
            RainWeight = 0f;

            SetState(LeafState.Detaching);

            if (audioManager)
                audioManager.PlaySFX(
                    SFXType.Detach
                );
        }

        public void SetScriptedPose(
            Vector3 position,
            float angle,
            bool landed
        )
        {
            scriptedFall = true;

            transform.position = position;

            CurrentAngle = angle;

            stemPivot.localRotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    angle
                );

            SetState(
                landed
                    ? LeafState.Landed
                    : LeafState.Falling
            );
        }

        private void ApplyRotation()
        {
            float minAngle =
                restingAngle - maxRotationFromRest;

            float maxAngle =
                restingAngle + maxRotationFromRest;

            CurrentAngle =
                Mathf.Clamp(
                    CurrentAngle,
                    minAngle,
                    maxAngle
                );

            stemPivot.localRotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    CurrentAngle
                );
        }

        private void SetState(LeafState value)
        {
            State = value;

            if (animator &&
                animator.runtimeAnimatorController)
            {
                animator.SetInteger(
                    StateParameter,
                    (int)value
                );
            }
        }
    }
}