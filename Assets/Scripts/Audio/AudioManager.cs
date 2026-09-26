using System.Collections;
using UnityEngine;

namespace LeafGame
{
    public sealed class AudioManager : MonoBehaviour
    {
        public AudioLibrary library;
        public AudioSource music;
        public AudioSource musicCrossfade;
        public AudioSource ambient;
        public AudioSource rain;
        public AudioSource sfx;
        public AudioSource voice;
        [Range(0f, 1f)] public float musicVolume = 0.28f;
        [Range(0f, 1f)] public float ambientVolume = 0.14f;
        [Range(0f, 1f)] public float rainVolume = 0.16f;
        [Min(0.01f)] public float crossfadeSeconds = 1.2f;
        private Coroutine musicRoutine;
        private bool cinematic;
        private float rainTarget;

        private void Start()
        {
            if (!library) return;
            SetAmbient(library.ambient);
            rain.clip = library.rain;
            rain.loop = true;
            rain.volume = 0f;
            if (rain.clip) rain.Play();
            if (!music.isPlaying) ChangeMusic(library.gameplayMusic);
        }
        private void Update() { if (rain) rain.volume = Mathf.MoveTowards(rain.volume, rainTarget, Time.unscaledDeltaTime * 0.15f); }
        public void PlaySFX(SFXType type, float volume = 1f)
        {
            if (!library || !sfx) return;
            foreach (var entry in library.sounds)
                if (entry.type == type && entry.clip) { sfx.PlayOneShot(entry.clip, entry.volume * volume); return; }
        }
        public void ChangeMusic(AudioClip clip)
        {
            if (!clip || (music.clip == clip && music.isPlaying)) return;
            if (musicRoutine != null) StopCoroutine(musicRoutine);
            musicRoutine = StartCoroutine(Crossfade(clip));
        }
        private IEnumerator Crossfade(AudioClip clip)
        {
            var old = music;
            var next = musicCrossfade;
            next.Stop(); next.clip = clip;
            next.loop = !cinematic || (library && clip == library.placeholderFinalMusic);
            next.volume = 0; next.Play();
            float from = old.volume;
            for (float t = 0; t < crossfadeSeconds; t += Time.unscaledDeltaTime)
            {
                float u = t / crossfadeSeconds;
                old.volume = Mathf.Lerp(from, 0f, u);
                next.volume = Mathf.Lerp(0f, musicVolume, u);
                yield return null;
            }
            old.Stop(); next.volume = musicVolume;
            music = next; musicCrossfade = old;
            musicRoutine = null;
        }
        public void SetAmbient(AudioClip clip)
        {
            if (!clip || !ambient || ambient.clip == clip) return;
            ambient.clip = clip; ambient.loop = true; ambient.volume = ambientVolume; ambient.Play();
        }
        public void SetIntensity(float instability)
        {
            if (ambient && !cinematic) ambient.volume = ambientVolume * Mathf.Lerp(0.7f, 1.5f, instability);
        }
        public void SetRain(float intensity) { rainTarget = cinematic ? 0f : intensity * rainVolume; }
        public void BeginFinal()
        {
            cinematic = true;
            SetRain(0f);
            if (ambient) ambient.volume = ambientVolume * 0.18f;
            if (library) ChangeMusic(library.finalMusic ? library.finalMusic : library.placeholderFinalMusic);
        }
        public void Speak(AudioClip clip) { if (voice && clip) { voice.clip = clip; voice.Play(); } }
        public void FadeToSilence(float duration) { StartCoroutine(Silence(duration)); }
        private IEnumerator Silence(float duration)
        {
            if (musicRoutine != null) { StopCoroutine(musicRoutine); musicRoutine = null; }
            float m = music.volume, b = musicCrossfade.volume, a = ambient.volume;
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                float u = 1f - t / Mathf.Max(0.01f, duration);
                music.volume = m * u; musicCrossfade.volume = b * u; ambient.volume = a * u;
                yield return null;
            }
            music.Stop(); musicCrossfade.Stop(); ambient.Stop(); rain.Stop(); voice.Stop();
        }
    }
}
