using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RelicTrail.Core
{
    /// <summary>
    /// Master audio manager managing background music crossfading, SFX playback, 3D spatial audio,
    /// and built-in procedural synthesis fallbacks.
    /// </summary>
    [AddComponentMenu("Relic Trail/Core/Audio Manager")]
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Music Tracks")]
        [SerializeField] private AudioClip forestMusic;
        [SerializeField] private AudioClip canyonMusic;
        [SerializeField] private AudioClip frozenMusic;
        [SerializeField] private AudioClip sanctuaryMusic;
        [SerializeField] private AudioClip victoryMusic;

        [Header("Shared SFX")]
        [SerializeField] private AudioClip coinPickupSfx;
        [SerializeField] private AudioClip relicPickupSfx;
        [SerializeField] private AudioClip magicShootSfx;
        [SerializeField] private AudioClip magicHitSfx;
        [SerializeField] private AudioClip playerHurtSfx;
        [SerializeField] private AudioClip enemyDefeatSfx;
        [SerializeField] private AudioClip portalTeleportSfx;
        [SerializeField] private AudioClip puzzleChimeSfx;
        [SerializeField] private AudioClip buttonClickSfx;

        [Header("Biome Footstep Audio")]
        [SerializeField] private AudioClip forestFootstepSfx;
        [SerializeField] private AudioClip canyonFootstepSfx;
        [SerializeField] private AudioClip frozenFootstepSfx;
        [SerializeField] private AudioClip sanctuaryFootstepSfx;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSourceA;
        [SerializeField] private AudioSource musicSourceB;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource footstepSource;

        private AudioSource activeMusicSource;
        private Coroutine crossfadeRoutine;

        // Public SFX getters so gameplay systems can access user-configured audio clips
        public AudioClip VictoryMusic => victoryMusic;
        public AudioClip CoinPickupSfx => coinPickupSfx;
        public AudioClip RelicPickupSfx => relicPickupSfx;
        public AudioClip MagicShootSfx => magicShootSfx;
        public AudioClip MagicHitSfx => magicHitSfx;
        public AudioClip PlayerHurtSfx => playerHurtSfx;
        public AudioClip EnemyDefeatSfx => enemyDefeatSfx;
        public AudioClip PortalTeleportSfx => portalTeleportSfx;
        public AudioClip PuzzleChimeSfx => puzzleChimeSfx;
        public AudioClip ButtonClickSfx => buttonClickSfx;
        public AudioClip ForestFootstepSfx => forestFootstepSfx;
        public AudioClip CanyonFootstepSfx => canyonFootstepSfx;
        public AudioClip FrozenFootstepSfx => frozenFootstepSfx;
        public AudioClip SanctuaryFootstepSfx => sanctuaryFootstepSfx;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            SetupAudioSources();
            GenerateProceduralFallbacks();
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnBiomeChanged += HandleBiomeMusic;
                GameManager.Instance.OnGameStateChanged += HandleGameStateAudio;

                if (GameManager.Instance.CurrentGameState == GameState.Playing)
                {
                    HandleBiomeMusic(GameManager.Instance.CurrentBiome);
                }
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnBiomeChanged -= HandleBiomeMusic;
                GameManager.Instance.OnGameStateChanged -= HandleGameStateAudio;
            }
        }

        private void SetupAudioSources()
        {
            if (musicSourceA == null)
            {
                musicSourceA = gameObject.AddComponent<AudioSource>();
                musicSourceA.loop = true;
                musicSourceA.playOnAwake = false;
                musicSourceA.volume = 0.4f;
            }

            if (musicSourceB == null)
            {
                musicSourceB = gameObject.AddComponent<AudioSource>();
                musicSourceB.loop = true;
                musicSourceB.playOnAwake = false;
                musicSourceB.volume = 0f;
            }

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.loop = false;
                sfxSource.playOnAwake = false;
            }

            if (footstepSource == null)
            {
                footstepSource = gameObject.AddComponent<AudioSource>();
                footstepSource.loop = false;
                footstepSource.playOnAwake = false;
                footstepSource.volume = 0.5f;
            }

            activeMusicSource = musicSourceA;
        }

        private void GenerateProceduralFallbacks()
        {
            // Synthesize pleasing audio tones if audio clips are not assigned in inspector
            if (coinPickupSfx == null) coinPickupSfx = CreateToneClip("CoinTone", 987.77f, 0.15f, true); // B5 tone
            if (relicPickupSfx == null) relicPickupSfx = CreateToneClip("RelicTone", 659.25f, 0.6f, true); // E5 fanfare
            if (magicShootSfx == null) magicShootSfx = CreateNoiseClip("MagicShootTone", 0.18f, 1200f);
            if (magicHitSfx == null) magicHitSfx = CreateNoiseClip("MagicHitTone", 0.2f, 400f);
            if (playerHurtSfx == null) playerHurtSfx = CreateNoiseClip("PlayerHurtTone", 0.22f, 280f);
            if (enemyDefeatSfx == null) enemyDefeatSfx = CreateToneClip("EnemyDefeatTone", 220f, 0.4f, false);
            if (portalTeleportSfx == null) portalTeleportSfx = CreateToneClip("PortalTone", 440f, 0.8f, true);
            if (puzzleChimeSfx == null) puzzleChimeSfx = CreateToneClip("PuzzleChime", 523.25f, 0.4f, false);
            if (buttonClickSfx == null) buttonClickSfx = CreateToneClip("ButtonClick", 800f, 0.05f, false);
            if (forestFootstepSfx == null) forestFootstepSfx = CreateNoiseClip("ForestStepTone", 0.08f, 180f);
            if (canyonFootstepSfx == null) canyonFootstepSfx = CreateNoiseClip("CanyonStepTone", 0.10f, 320f);
            if (frozenFootstepSfx == null) frozenFootstepSfx = CreateNoiseClip("FrozenStepTone", 0.08f, 850f);
            if (sanctuaryFootstepSfx == null) sanctuaryFootstepSfx = CreateToneClip("SanctuaryStepTone", 260f, 0.06f, false);
        }

        private AudioClip CreateToneClip(string clipName, float frequency, float duration, bool arpeggio)
        {
            int sampleRate = 44100;
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float currentFreq = frequency;
                if (arpeggio && t > duration * 0.5f) currentFreq *= 1.334f; // Perfect fourth jump

                float envelope = Mathf.Clamp01(1f - (t / duration));
                data[i] = Mathf.Sin(2f * Mathf.PI * currentFreq * t) * envelope * 0.4f;
            }

            AudioClip clip = AudioClip.Create(clipName, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip CreateNoiseClip(string clipName, float duration, float centerFreq)
        {
            int sampleRate = 44100;
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Clamp01(1f - (t / duration));
                float noise = (Random.value * 2f - 1f) * 0.3f;
                float sine = Mathf.Sin(2f * Mathf.PI * centerFreq * t) * 0.2f;
                data[i] = (noise + sine) * envelope;
            }

            AudioClip clip = AudioClip.Create(clipName, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public void PlaySfx(AudioClip clip, float volume = 1f)
        {
            if (clip != null && sfxSource != null)
            {
                sfxSource.PlayOneShot(clip, volume);
            }
        }

        public void Play3DSfx(AudioClip clip, Vector3 position, float volume = 1f)
        {
            if (clip != null)
            {
                AudioSource.PlayClipAtPoint(clip, position, volume);
            }
        }

        public void PlayCoinPickup(Vector3 pos)
        {
            if (coinPickupSfx != null)
            {
                if (sfxSource != null) sfxSource.PlayOneShot(coinPickupSfx, 0.85f);
                else AudioSource.PlayClipAtPoint(coinPickupSfx, pos, 0.85f);
            }
        }

        public void PlayRelicPickup(Vector3 pos)
        {
            if (relicPickupSfx != null)
            {
                if (sfxSource != null) sfxSource.PlayOneShot(relicPickupSfx, 1f);
                else AudioSource.PlayClipAtPoint(relicPickupSfx, pos, 1f);
            }
        }

        public void PlayMagicShoot(Vector3 pos)
        {
            if (magicShootSfx != null)
            {
                if (sfxSource != null) sfxSource.PlayOneShot(magicShootSfx, 0.85f);
                else AudioSource.PlayClipAtPoint(magicShootSfx, pos, 0.85f);
            }
        }
        public void PlayMagicHit(Vector3 pos) => Play3DSfx(magicHitSfx, pos, 0.8f);
        public void PlayPlayerHurt(Vector3 pos)
        {
            if (playerHurtSfx != null)
            {
                if (sfxSource != null) sfxSource.PlayOneShot(playerHurtSfx, 0.9f);
                else AudioSource.PlayClipAtPoint(playerHurtSfx, pos, 0.9f);
            }
        }
        public void PlayEnemyDefeat(Vector3 pos) => Play3DSfx(enemyDefeatSfx, pos, 0.9f);
        public void PlayPortalTeleport(Vector3 pos) => Play3DSfx(portalTeleportSfx, pos, 0.9f);
        public void PlayPuzzleChime(Vector3 pos)
        {
            if (puzzleChimeSfx != null)
            {
                if (sfxSource != null) sfxSource.PlayOneShot(puzzleChimeSfx, 0.85f);
                else AudioSource.PlayClipAtPoint(puzzleChimeSfx, pos, 0.85f);
            }
        }
        public void PlayButtonClick() => PlaySfx(buttonClickSfx, 0.7f);

        public void PlayFootstep(Vector3 pos, BiomeType biome)
        {
            AudioClip clip = null;
            switch (biome)
            {
                case BiomeType.ForgottenForest:
                    clip = forestFootstepSfx;
                    break;
                case BiomeType.ScorchedCanyon:
                    clip = canyonFootstepSfx;
                    break;
                case BiomeType.FrozenRuins:
                    clip = frozenFootstepSfx;
                    break;
                case BiomeType.FinalSanctuary:
                    clip = sanctuaryFootstepSfx;
                    break;
            }

            if (clip != null)
            {
                if (footstepSource != null)
                {
                    footstepSource.pitch = Random.Range(0.92f, 1.08f);
                    footstepSource.PlayOneShot(clip, 0.45f);
                }
                else if (sfxSource != null)
                {
                    sfxSource.PlayOneShot(clip, 0.45f);
                }
                else
                {
                    AudioSource.PlayClipAtPoint(clip, pos, 0.45f);
                }
            }
        }

        private void HandleBiomeMusic(BiomeType biome)
        {
            AudioClip targetTrack = null;
            switch (biome)
            {
                case BiomeType.ForgottenForest:
                    targetTrack = forestMusic;
                    break;
                case BiomeType.ScorchedCanyon:
                    targetTrack = canyonMusic;
                    break;
                case BiomeType.FrozenRuins:
                    targetTrack = frozenMusic;
                    break;
                case BiomeType.FinalSanctuary:
                    targetTrack = sanctuaryMusic;
                    break;
            }

            if (targetTrack != null)
            {
                CrossfadeMusic(targetTrack, 1.5f);
            }
        }

        private void HandleGameStateAudio(GameState state)
        {
            if (state == GameState.Playing && GameManager.Instance != null)
            {
                HandleBiomeMusic(GameManager.Instance.CurrentBiome);
            }
            else if (state == GameState.Victory)
            {
                PlayVictoryAudio();
            }
        }

        public void PlayVictoryAudio()
        {
            if (crossfadeRoutine != null)
            {
                StopCoroutine(crossfadeRoutine);
                crossfadeRoutine = null;
            }

            if (musicSourceA != null)
            {
                musicSourceA.Stop();
                musicSourceA.volume = 0f;
            }
            if (musicSourceB != null)
            {
                musicSourceB.Stop();
                musicSourceB.volume = 0f;
            }

            if (victoryMusic != null)
            {
                if (sfxSource != null)
                {
                    sfxSource.PlayOneShot(victoryMusic, 0.95f);
                }
                else if (musicSourceA != null)
                {
                    musicSourceA.clip = victoryMusic;
                    musicSourceA.loop = false;
                    musicSourceA.volume = 0.95f;
                    musicSourceA.Play();
                }
            }
        }

        public void CrossfadeMusic(AudioClip newClip, float duration = 1.5f)
        {
            if (newClip == null || (activeMusicSource != null && activeMusicSource.clip == newClip)) return;

            if (crossfadeRoutine != null) StopCoroutine(crossfadeRoutine);
            crossfadeRoutine = StartCoroutine(CrossfadeRoutine(newClip, duration));
        }

        private IEnumerator CrossfadeRoutine(AudioClip newClip, float duration)
        {
            AudioSource fadingOut = activeMusicSource;
            AudioSource fadingIn = (activeMusicSource == musicSourceA) ? musicSourceB : musicSourceA;

            fadingIn.clip = newClip;
            fadingIn.Play();

            float elapsed = 0f;
            float maxVolume = 0.45f;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                fadingOut.volume = Mathf.Lerp(maxVolume, 0f, t);
                fadingIn.volume = Mathf.Lerp(0f, maxVolume, t);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            fadingOut.Stop();
            fadingOut.volume = 0f;
            fadingIn.volume = maxVolume;
            activeMusicSource = fadingIn;
        }
    }
}
