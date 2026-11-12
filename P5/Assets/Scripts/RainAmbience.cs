using System.Collections;
using UnityEngine;

public class RainAmbience : MonoBehaviour
{
    [SerializeField] private AudioClip rainClip;
    [SerializeField, Range(0f, 1f)] private float volume = 0.6f;
    [SerializeField] private float fadeInTime = 2f;
    [SerializeField, Tooltip("0 = native looping (clip must loop seamlessly). >0 = crossfade the clip's end into its start.")]
    private float crossfadeTime = 0f;
    [SerializeField] private bool playOnStart = true;

    [Header("Indoor / Outdoor")]
    [SerializeField, Range(0f, 1f), Tooltip("Volume multiplier while the player is inside an indoor zone.")]
    private float indoorVolume = 0.35f;
    [SerializeField, Tooltip("Muffle the rain indoors with a low-pass filter.")]
    private bool muffleIndoors = true;
    [SerializeField, Tooltip("Low-pass cutoff (Hz) when fully indoors.")]
    private float indoorCutoff = 1200f;
    [SerializeField, Tooltip("Seconds to blend between outside and inside.")]
    private float transitionTime = 0.8f;

    private const float OpenCutoff = 22000f;

    private readonly AudioSource[] sources = new AudioSource[2];
    private readonly float[] gains = new float[2];
    private AudioLowPassFilter lowPass;
    private float master;
    private float indoorBlend; // 0 = outside, 1 = inside
    private int indoorZones;
    private int current;
    private Coroutine loopRoutine;
    private Coroutine fadeRoutine;

    public bool IsPlaying => loopRoutine != null || sources[0].isPlaying;
    public bool IsIndoors => indoorZones > 0;

    private void Awake()
    {
        for (int i = 0; i < sources.Length; i++)
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.clip = rainClip;
            s.playOnAwake = false;
            s.spatialBlend = 0f; // 2D ambience
            s.volume = 0f;
            sources[i] = s;
        }

        lowPass = GetComponent<AudioLowPassFilter>();
        if (lowPass == null) lowPass = gameObject.AddComponent<AudioLowPassFilter>();
        lowPass.cutoffFrequency = OpenCutoff;
    }

    private void Start()
    {
        if (playOnStart) Play();
    }

    private void Update()
    {
        float target = IsIndoors ? 1f : 0f;
        indoorBlend = transitionTime > 0f
            ? Mathf.MoveTowards(indoorBlend, target, Time.unscaledDeltaTime / transitionTime)
            : target;

        float environment = Mathf.Lerp(1f, indoorVolume, indoorBlend);
        for (int i = 0; i < sources.Length; i++)
            sources[i].volume = volume * master * gains[i] * environment;

        lowPass.enabled = muffleIndoors;
        if (muffleIndoors) // logarithmic sweep sounds even to the ear
            lowPass.cutoffFrequency = OpenCutoff * Mathf.Pow(indoorCutoff / OpenCutoff, indoorBlend);
    }

    public void SetVolume(float value) => volume = Mathf.Clamp01(value);

    public void EnterIndoor() => indoorZones++;

    public void ExitIndoor() => indoorZones = Mathf.Max(0, indoorZones - 1);

    public void Play()
    {
        if (rainClip == null || IsPlaying) return;

        current = 0;
        gains[0] = 1f;
        gains[1] = 0f;

        if (crossfadeTime > 0f)
        {
            crossfadeTime = Mathf.Min(crossfadeTime, rainClip.length * 0.5f);
            sources[0].loop = false;
            loopRoutine = StartCoroutine(CrossfadeLoop());
        }
        else
        {
            sources[0].loop = true;
            sources[0].Play();
        }

        FadeTo(1f, fadeInTime, false);
    }

    public void Stop(float fadeOutTime = 2f)
    {
        FadeTo(0f, fadeOutTime, true);
    }

    private IEnumerator CrossfadeLoop()
    {
        sources[current].time = 0f;
        sources[current].Play();

        while (true)
        {
            AudioSource cur = sources[current];
            yield return new WaitUntil(() => cur.time >= rainClip.length - crossfadeTime || !cur.isPlaying);

            int next = 1 - current;
            sources[next].time = 0f;
            sources[next].Play();

            float t = 0f;
            while (t < crossfadeTime)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / crossfadeTime);
                gains[current] = Mathf.Cos(k * Mathf.PI * 0.5f); // equal-power crossfade
                gains[next] = Mathf.Sin(k * Mathf.PI * 0.5f);
                yield return null;
            }

            sources[current].Stop();
            gains[current] = 0f;
            current = next;
        }
    }

    private void FadeTo(float target, float duration, bool stopWhenDone)
    {
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(Fade(target, duration, stopWhenDone));
    }

    private IEnumerator Fade(float target, float duration, bool stopWhenDone)
    {
        float start = master;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            master = Mathf.Lerp(start, target, t / duration);
            yield return null;
        }
        master = target;

        if (stopWhenDone)
        {
            if (loopRoutine != null) StopCoroutine(loopRoutine);
            loopRoutine = null;
            foreach (var s in sources) s.Stop();
        }
        fadeRoutine = null;
    }
}