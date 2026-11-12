using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RainAmbience : MonoBehaviour
{
    [Serializable]
    public class LoopLayer
    {
        public string name = "Rain";
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 0.6f;
        [Tooltip("0 = native looping (clip must loop seamlessly). >0 = crossfade the clip's end into its start.")]
        public float crossfadeTime = 0f;
        [Range(0f, 1f), Tooltip("Volume multiplier while indoors.")]
        public float indoorVolume = 0.35f;
        public bool muffleIndoors = true;

        [NonSerialized] public AudioSource[] sources;
        [NonSerialized] public float[] gains;
        [NonSerialized] public AudioLowPassFilter lowPass;
        [NonSerialized] public int current;
        [NonSerialized] public Coroutine routine;
    }

    [Serializable]
    public class RandomSound
    {
        public string name = "Thunder";
        [Tooltip("One is picked at random each time.")]
        public AudioClip[] clips;
        [Range(0f, 1f)] public float volume = 0.8f;
        [Tooltip("Seconds between plays (min, max).")]
        public Vector2 interval = new Vector2(15f, 40f);
        [Tooltip("Random pitch range (min, max).")]
        public Vector2 pitch = new Vector2(0.9f, 1.1f);
        [Range(0f, 1f), Tooltip("Volume multiplier while indoors.")]
        public float indoorVolume = 0.5f;
        public bool muffleIndoors = true;

        [NonSerialized] public AudioSource source;
        [NonSerialized] public AudioLowPassFilter lowPass;
        [NonSerialized] public float timer;
    }

    [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
    [SerializeField] private float fadeInTime = 2f;
    [SerializeField] private bool playOnStart = true;

    [Header("Sounds")]
    [SerializeField] private List<LoopLayer> loops = new List<LoopLayer> { new LoopLayer() };
    [SerializeField] private List<RandomSound> randomSounds = new List<RandomSound>();

    [Header("Indoor / Outdoor")]
    [SerializeField, Tooltip("Low-pass cutoff (Hz) when fully indoors.")]
    private float indoorCutoff = 1200f;
    [SerializeField, Tooltip("Seconds to blend between outside and inside.")]
    private float transitionTime = 0.8f;

    private const float OpenCutoff = 22000f;

    private float fade;
    private float indoorBlend; // 0 = outside, 1 = inside
    private int indoorZones;
    private bool playing;
    private Coroutine fadeRoutine;

    public bool IsPlaying => playing;
    public bool IsIndoors => indoorZones > 0;

    private void Awake()
    {
        foreach (var layer in loops)
        {
            var go = CreateChild("Loop_" + layer.name);
            layer.sources = new[] { CreateSource(go, layer.clip), CreateSource(go, layer.clip) };
            layer.gains = new float[2];
            layer.lowPass = go.AddComponent<AudioLowPassFilter>();
            layer.lowPass.cutoffFrequency = OpenCutoff;
        }

        foreach (var sound in randomSounds)
        {
            var go = CreateChild("Random_" + sound.name);
            sound.source = CreateSource(go, null);
            sound.lowPass = go.AddComponent<AudioLowPassFilter>();
            sound.lowPass.cutoffFrequency = OpenCutoff;
        }
    }

    private void Start()
    {
        if (playOnStart) Play();
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        float target = IsIndoors ? 1f : 0f;
        indoorBlend = transitionTime > 0f ? Mathf.MoveTowards(indoorBlend, target, dt / transitionTime) : target;

        // Logarithmic sweep sounds even to the ear
        float cutoff = OpenCutoff * Mathf.Pow(indoorCutoff / OpenCutoff, indoorBlend);

        foreach (var layer in loops)
        {
            float env = Mathf.Lerp(1f, layer.indoorVolume, indoorBlend);
            for (int i = 0; i < 2; i++)
                layer.sources[i].volume = layer.volume * masterVolume * fade * layer.gains[i] * env;

            layer.lowPass.enabled = layer.muffleIndoors;
            if (layer.muffleIndoors) layer.lowPass.cutoffFrequency = cutoff;
        }

        foreach (var sound in randomSounds)
        {
            float env = Mathf.Lerp(1f, sound.indoorVolume, indoorBlend);
            sound.source.volume = sound.volume * masterVolume * fade * env;

            sound.lowPass.enabled = sound.muffleIndoors;
            if (sound.muffleIndoors) sound.lowPass.cutoffFrequency = cutoff;

            if (!playing || sound.clips == null || sound.clips.Length == 0) continue;

            sound.timer -= dt;
            if (sound.timer > 0f) continue;

            var clip = sound.clips[UnityEngine.Random.Range(0, sound.clips.Length)];
            if (clip != null)
            {
                sound.source.pitch = RandomBetween(sound.pitch);
                sound.source.PlayOneShot(clip);
            }
            sound.timer = RandomBetween(sound.interval);
        }
    }

    public void SetVolume(float value) => masterVolume = Mathf.Clamp01(value);

    public void SetLayerVolume(string layerName, float value)
    {
        var layer = loops.Find(l => l.name == layerName);
        if (layer != null) layer.volume = Mathf.Clamp01(value);
    }

    public void EnterIndoor() => indoorZones++;

    public void ExitIndoor() => indoorZones = Mathf.Max(0, indoorZones - 1);

    public void Play()
    {
        if (playing) return;
        playing = true;

        foreach (var layer in loops)
        {
            if (layer.clip == null) continue;

            layer.current = 0;
            layer.gains[0] = 1f;
            layer.gains[1] = 0f;

            if (layer.crossfadeTime > 0f)
            {
                layer.crossfadeTime = Mathf.Min(layer.crossfadeTime, layer.clip.length * 0.5f);
                layer.sources[0].loop = false;
                layer.routine = StartCoroutine(CrossfadeLoop(layer));
            }
            else
            {
                layer.sources[0].loop = true;
                layer.sources[0].Play();
            }
        }

        foreach (var sound in randomSounds)
            sound.timer = RandomBetween(sound.interval);

        FadeTo(1f, fadeInTime, false);
    }

    public void Stop(float fadeOutTime = 2f)
    {
        FadeTo(0f, fadeOutTime, true);
    }

    private IEnumerator CrossfadeLoop(LoopLayer layer)
    {
        var first = layer.sources[layer.current];
        first.time = 0f;
        first.Play();

        while (true)
        {
            AudioSource cur = layer.sources[layer.current];
            yield return new WaitUntil(() => cur.time >= layer.clip.length - layer.crossfadeTime || !cur.isPlaying);

            int next = 1 - layer.current;
            layer.sources[next].time = 0f;
            layer.sources[next].Play();

            float t = 0f;
            while (t < layer.crossfadeTime)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / layer.crossfadeTime);
                layer.gains[layer.current] = Mathf.Cos(k * Mathf.PI * 0.5f); // equal-power crossfade
                layer.gains[next] = Mathf.Sin(k * Mathf.PI * 0.5f);
                yield return null;
            }

            cur.Stop();
            layer.gains[layer.current] = 0f;
            layer.current = next;
        }
    }

    private void FadeTo(float target, float duration, bool stopWhenDone)
    {
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(Fade(target, duration, stopWhenDone));
    }

    private IEnumerator Fade(float target, float duration, bool stopWhenDone)
    {
        float start = fade;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            fade = Mathf.Lerp(start, target, t / duration);
            yield return null;
        }
        fade = target;

        if (stopWhenDone)
        {
            playing = false;
            foreach (var layer in loops)
            {
                if (layer.routine != null) StopCoroutine(layer.routine);
                layer.routine = null;
                foreach (var s in layer.sources) s.Stop();
            }
            foreach (var sound in randomSounds) sound.source.Stop();
        }
        fadeRoutine = null;
    }

    private GameObject CreateChild(string childName)
    {
        var go = new GameObject(childName);
        go.transform.SetParent(transform, false);
        return go;
    }

    private static AudioSource CreateSource(GameObject go, AudioClip clip)
    {
        var s = go.AddComponent<AudioSource>();
        s.clip = clip;
        s.playOnAwake = false;
        s.spatialBlend = 0f; // 2D ambience
        s.volume = 0f;
        return s;
    }

    private static float RandomBetween(Vector2 range) =>
        UnityEngine.Random.Range(Mathf.Min(range.x, range.y), Mathf.Max(range.x, range.y));
}