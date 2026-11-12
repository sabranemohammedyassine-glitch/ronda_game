using UnityEngine;

namespace Zellige.UI
{
    /// <summary>
    /// Short sound effects synthesised at start-up (no audio files needed, so nothing
    /// to license). Replace any clip with a recorded one later without touching callers.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class SoundFx : MonoBehaviour
    {
        private const int SampleRate = 44100;

        private AudioSource source;
        private AudioClip deal;
        private AudioClip place;
        private AudioClip capture;
        private AudioClip score;
        private AudioClip win;
        private AudioClip lose;

        public bool Muted
        {
            get => source.mute;
            set => source.mute = value;
        }

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            deal = Noise("deal", 0.05f, 0.25f);
            place = Tone("place", new[] { 180f }, 0.09f, 0.5f);
            capture = Tone("capture", new[] { 660f, 880f }, 0.09f, 0.35f);
            score = Tone("score", new[] { 523f, 659f, 784f }, 0.1f, 0.3f);
            win = Tone("win", new[] { 523f, 659f, 784f, 1047f }, 0.16f, 0.32f);
            lose = Tone("lose", new[] { 392f, 330f, 262f }, 0.2f, 0.3f);
        }

        public void PlayDeal() => source.PlayOneShot(deal);
        public void PlayPlace() => source.PlayOneShot(place);
        public void PlayCapture() => source.PlayOneShot(capture);
        public void PlayScore() => source.PlayOneShot(score);
        public void PlayResult(bool won) => source.PlayOneShot(won ? win : lose);

        /// <summary>A sequence of soft bell-like notes, each <paramref name="noteLength"/> seconds.</summary>
        private static AudioClip Tone(string name, float[] frequencies, float noteLength, float volume)
        {
            int noteSamples = (int)(SampleRate * noteLength);
            int tail = SampleRate / 5;
            var data = new float[noteSamples * frequencies.Length + tail];
            for (int n = 0; n < frequencies.Length; n++)
            {
                int start = n * noteSamples;
                for (int i = 0; start + i < data.Length; i++)
                {
                    float t = i / (float)SampleRate;
                    float envelope = Mathf.Exp(-t * 18f) * Mathf.Clamp01(i / 200f);
                    float wave = Mathf.Sin(2f * Mathf.PI * frequencies[n] * t) + 0.3f * Mathf.Sin(4f * Mathf.PI * frequencies[n] * t);
                    data[start + i] += wave * envelope * volume;
                }
            }
            return Clip(name, data);
        }

        /// <summary>A short filtered noise burst, like a card sliding on cloth.</summary>
        private static AudioClip Noise(string name, float length, float volume)
        {
            var random = new System.Random(7);
            var data = new float[(int)(SampleRate * length)];
            float smoothed = 0f;
            for (int i = 0; i < data.Length; i++)
            {
                float white = (float)random.NextDouble() * 2f - 1f;
                smoothed = Mathf.Lerp(smoothed, white, 0.35f);
                float envelope = Mathf.Sin(Mathf.PI * i / data.Length);
                data[i] = smoothed * envelope * volume;
            }
            return Clip(name, data);
        }

        private static AudioClip Clip(string name, float[] data)
        {
            AudioClip clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
