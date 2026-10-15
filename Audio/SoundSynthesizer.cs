using System.Runtime.InteropServices;
using Raylib_cs;

namespace TheFifthAxis.Audio;

public class SoundSynthesizer : IDisposable
{
    private const int SampleRate = 44100;
    private const int BufferFrames = 1024;
    private AudioStream _audioStream;
    private bool _initialized;
    private readonly short[] _buffer = new short[BufferFrames * 2]; // Stereo interleaved

    // Musical Engine
    public bool MusicEnabled { get; set; } = true;
    public bool SfxEnabled { get; set; } = true;
    public float MasterVolume { get; set; } = 0.8f;

    private double _sampleTime;
    private double _musicBeatTime;
    private int _step;
    private readonly float[] _scaleFreqs;

    // Active SFX slots
    private class SfxVoice
    {
        public bool Active;
        public string Type = "";
        public float Duration;
        public float Elapsed;
        public float Volume;
    }

    private readonly List<SfxVoice> _sfxPool = new();

    public SoundSynthesizer()
    {
        // 12-TET frequencies
        _scaleFreqs = new float[128];
        for (int i = 0; i < 128; i++)
        {
            _scaleFreqs[i] = 440.0f * MathF.Pow(2.0f, (i - 69) / 12.0f);
        }

        for (int i = 0; i < 16; i++)
        {
            _sfxPool.Add(new SfxVoice());
        }
    }

    public void Init()
    {
        if (_initialized) return;
        Raylib.InitAudioDevice();
        if (!Raylib.IsAudioDeviceReady()) return;

        _audioStream = Raylib.LoadAudioStream(SampleRate, 16, 2);
        Raylib.PlayAudioStream(_audioStream);
        _initialized = true;
    }

    public void Update()
    {
        if (!_initialized) return;

        while (Raylib.IsAudioStreamProcessed(_audioStream))
        {
            GenerateAudioBuffer();
            unsafe
            {
                fixed (short* p = _buffer)
                {
                    Raylib.UpdateAudioStream(_audioStream, p, BufferFrames);
                }
            }
        }
    }

    // Melodic pattern for The 5th Axis (C minor / Eb / Ab heroic theme in 125 BPM)
    // Gilles Soulet arrangement
    private static readonly int[] MelodyNotes = [
        60, 63, 67, 70,  72, 70, 67, 63,  65, 68, 72, 75,  72, 68, 65, 63,
        60, 63, 67, 72,  75, 74, 72, 70,  68, 70, 72, 75,  79, 75, 72, 67,
        58, 62, 65, 68,  70, 68, 65, 62,  56, 60, 63, 67,  68, 67, 63, 60,
        55, 58, 62, 65,  67, 65, 62, 58,  60, 63, 67, 72,  75, 72, 79, 84
    ];

    private static readonly int[] BassNotes = [
        36, 36, 48, 36,  36, 36, 48, 36,  41, 41, 53, 41,  41, 41, 53, 41,
        36, 36, 48, 36,  36, 36, 48, 36,  44, 44, 56, 44,  44, 44, 56, 44,
        34, 34, 46, 34,  34, 34, 46, 34,  32, 32, 44, 32,  32, 32, 44, 32,
        31, 31, 43, 31,  31, 31, 43, 31,  36, 36, 48, 36,  36, 36, 48, 48
    ];

    private void GenerateAudioBuffer()
    {
        double dt = 1.0 / SampleRate;
        double beatDuration = 60.0 / 125.0 / 4.0; // 16th notes at 125 BPM

        for (int i = 0; i < BufferFrames; i++)
        {
            _sampleTime += dt;
            _musicBeatTime += dt;

            if (_musicBeatTime >= beatDuration)
            {
                _musicBeatTime -= beatDuration;
                _step = (_step + 1) % MelodyNotes.Length;
            }

            float left = 0f;
            float right = 0f;

            // Music Synthesizer
            if (MusicEnabled)
            {
                float stepProgress = (float)(_musicBeatTime / beatDuration);

                // Voice 1: Lead Melody (Pulse Wave with PWM & Vibrato)
                int mNote = MelodyNotes[_step];
                if (mNote > 0)
                {
                    float freq = _scaleFreqs[mNote];
                    float vibrato = 1f + 0.015f * MathF.Sin((float)(_sampleTime * 35.0));
                    float phase = (float)((_sampleTime * freq * vibrato) % 1.0);
                    float pwm = 0.5f + 0.25f * MathF.Sin((float)(_sampleTime * 3.0));
                    float leadWave = phase < pwm ? 1f : -1f;
                    float leadEnv = MathF.Pow(1f - stepProgress, 0.8f);
                    left += leadWave * leadEnv * 0.18f;
                    right += leadWave * leadEnv * 0.14f;
                }

                // Voice 2: Harmony Arpeggio (Sawtooth)
                int arpOffset = (_step % 4) switch { 0 => 0, 1 => 4, 2 => 7, _ => 12 };
                int arpNote = BassNotes[_step] + 24 + arpOffset;
                float arFreq = _scaleFreqs[Math.Clamp(arpNote, 0, 127)];
                float arpPhase = (float)((_sampleTime * arFreq) % 1.0);
                float arpWave = 2f * arpPhase - 1f;
                float arpEnv = MathF.Pow((float)(1f - (_musicBeatTime / beatDuration)), 2f);
                left += arpWave * arpEnv * 0.09f;
                right += arpWave * arpEnv * 0.12f;

                // Voice 3: Bassline (Deep Triangle / Square mix)
                int bNote = BassNotes[_step];
                if (bNote > 0)
                {
                    float bFreq = _scaleFreqs[bNote];
                    float bPhase = (float)((_sampleTime * bFreq) % 1.0);
                    float triWave = 2f * MathF.Abs(2f * bPhase - 1f) - 1f;
                    float sqWave = bPhase < 0.5f ? 0.7f : -0.7f;
                    float bass = (triWave * 0.7f + sqWave * 0.3f);
                    float bassEnv = MathF.Max(0f, 1f - stepProgress * 0.9f);
                    left += bass * bassEnv * 0.22f;
                    right += bass * bassEnv * 0.22f;
                }

                // Voice 4: Percussion (Kick / Snare / HiHat)
                int beatInBar = _step % 16;
                // Kick on 0, 4, 8, 12
                if (beatInBar == 0 || beatInBar == 8 || beatInBar == 14)
                {
                    float kEnv = MathF.Max(0f, 1f - stepProgress * 2.5f);
                    float kFreq = 120f * (1f - stepProgress) + 45f;
                    float kWave = MathF.Sin((float)(_sampleTime * kFreq * 2.0 * Math.PI));
                    left += kWave * kEnv * 0.25f;
                    right += kWave * kEnv * 0.25f;
                }
                // Snare on 4, 12
                if (beatInBar == 4 || beatInBar == 12)
                {
                    float sEnv = MathF.Max(0f, 1f - stepProgress * 1.5f);
                    float noise = (float)(Random.Shared.NextDouble() * 2.0 - 1.0);
                    left += noise * sEnv * 0.16f;
                    right += noise * sEnv * 0.16f;
                }
                // Hi-Hat on offbeats
                if (beatInBar % 2 == 1)
                {
                    float hEnv = MathF.Max(0f, 1f - stepProgress * 4.0f);
                    float noise = (float)(Random.Shared.NextDouble() * 2.0 - 1.0);
                    left += noise * hEnv * 0.06f;
                    right += noise * hEnv * 0.08f;
                }
            }

            // Sound Effects Processing
            if (SfxEnabled)
            {
                foreach (var sfx in _sfxPool)
                {
                    if (!sfx.Active) continue;
                    sfx.Elapsed += (float)dt;
                    if (sfx.Elapsed >= sfx.Duration)
                    {
                        sfx.Active = false;
                        continue;
                    }

                    float progress = sfx.Elapsed / sfx.Duration;
                    float sfxL = 0f;
                    float sfxR = 0f;

                    switch (sfx.Type)
                    {
                        case "punch":
                            float pEnv = 1f - progress;
                            float pFreq = 180f * (1f - progress * 0.6f);
                            float pNoise = (float)(Random.Shared.NextDouble() * 2.0 - 1.0) * 0.4f;
                            float pTone = MathF.Sin(sfx.Elapsed * pFreq * 2f * MathF.PI);
                            float pVal = (pTone + pNoise) * pEnv * sfx.Volume;
                            sfxL = pVal * 1.1f;
                            sfxR = pVal * 0.9f;
                            break;

                        case "kick":
                            float kEnv = MathF.Pow(1f - progress, 1.5f);
                            float kFreq = 140f * MathF.Exp(-progress * 4f);
                            float kNoise = (float)(Random.Shared.NextDouble() * 2.0 - 1.0) * 0.6f;
                            float kTone = MathF.Sin(sfx.Elapsed * kFreq * 2f * MathF.PI);
                            float kVal = (kTone + kNoise) * kEnv * sfx.Volume;
                            sfxL = kVal;
                            sfxR = kVal;
                            break;

                        case "laser":
                            float lEnv = 1f - progress;
                            float lFreq = 1400f * (1f - progress * 0.85f);
                            float lTone = MathF.Sin(sfx.Elapsed * lFreq * 2f * MathF.PI);
                            float lVal = lTone * lEnv * sfx.Volume;
                            sfxL = lVal * 0.8f;
                            sfxR = lVal * 1.2f;
                            break;

                        case "hit":
                            float hEnv = MathF.Pow(1f - progress, 2f);
                            float hTone = MathF.Sin(sfx.Elapsed * 620f * 2f * MathF.PI) +
                                          MathF.Sin(sfx.Elapsed * 940f * 2f * MathF.PI) * 0.5f;
                            float hNoise = (float)(Random.Shared.NextDouble() * 2.0 - 1.0) * 0.3f;
                            float hVal = (hTone + hNoise) * hEnv * sfx.Volume;
                            sfxL = hVal;
                            sfxR = hVal;
                            break;

                        case "elevator":
                            float ePhase = (sfx.Elapsed * 80f) % 1f;
                            float eVal = (ePhase < 0.5f ? 0.3f : -0.3f) * sfx.Volume;
                            sfxL = eVal;
                            sfxR = eVal;
                            break;

                        case "teleport":
                            float tFreq = 300f + 1200f * MathF.Sin(progress * MathF.PI);
                            float tTone = MathF.Sin(sfx.Elapsed * tFreq * 2f * MathF.PI);
                            float tVal = tTone * (1f - progress * 0.5f) * sfx.Volume;
                            sfxL = tVal * (1f - progress);
                            sfxR = tVal * progress;
                            break;

                        case "anachronon":
                            int noteIdx = (int)(progress * 6);
                            float aFreq = _scaleFreqs[72 + noteIdx * 3];
                            float aTone = MathF.Sin(sfx.Elapsed * aFreq * 2f * MathF.PI);
                            float aVal = aTone * (1f - progress) * sfx.Volume;
                            sfxL = aVal;
                            sfxR = aVal;
                            break;

                        case "explosion":
                            float exEnv = MathF.Pow(1f - progress, 1.2f);
                            float exNoise = (float)(Random.Shared.NextDouble() * 2.0 - 1.0);
                            float exVal = exNoise * exEnv * sfx.Volume;
                            sfxL = exVal;
                            sfxR = exVal;
                            break;
                    }

                    left += sfxL;
                    right += sfxR;
                }
            }

            // Apply Master Volume and Soft Clipping Limiter
            left = Math.Clamp(left * MasterVolume, -1.0f, 1.0f);
            right = Math.Clamp(right * MasterVolume, -1.0f, 1.0f);

            _buffer[i * 2] = (short)(left * 32767f);
            _buffer[i * 2 + 1] = (short)(right * 32767f);
        }
    }

    public void PlaySfx(string type, float volume = 0.5f, float duration = 0.25f)
    {
        var voice = _sfxPool.FirstOrDefault(v => !v.Active) ?? _sfxPool[0];
        voice.Active = true;
        voice.Type = type;
        voice.Volume = volume;
        voice.Duration = duration;
        voice.Elapsed = 0f;
    }

    public void PlayPunch() => PlaySfx("punch", 0.6f, 0.18f);
    public void PlayKick() => PlaySfx("kick", 0.7f, 0.25f);
    public void PlayLaser() => PlaySfx("laser", 0.5f, 0.22f);
    public void PlayHit() => PlaySfx("hit", 0.6f, 0.20f);
    public void PlayElevator() => PlaySfx("elevator", 0.35f, 0.40f);
    public void PlayTeleport() => PlaySfx("teleport", 0.75f, 0.85f);
    public void PlayAnachronon() => PlaySfx("anachronon", 0.8f, 0.90f);
    public void PlayExplosion() => PlaySfx("explosion", 0.85f, 0.60f);

    public void Dispose()
    {
        if (_initialized)
        {
            Raylib.StopAudioStream(_audioStream);
            Raylib.UnloadAudioStream(_audioStream);
            Raylib.CloseAudioDevice();
            _initialized = false;
        }
        GC.SuppressFinalize(this);
    }
}
