// Ses: dışarıdan dosya gerektirmeden açılışta sentezlenen ortam döngüleri ve efektler.
// Gündüz: rüzgâr + kuşlar · Gece: cırcır böcekleri + rüzgâr + baykuş · Ateş: çıtırtı + uğultu (3B)
// Efektler: dokunma, seçim, geçiş, gece gongu, sabah çanı, ölüm, muhtar fanfarı, kazanma/kaybetme, tik-tak.
using System.Collections.Generic;
using UnityEngine;

namespace VK
{
    public class Sfx : MonoBehaviour
    {
        public static Sfx I;
        const int SR = 22050;
        readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        readonly Dictionary<string, float> vol = new Dictionary<string, float>();
        AudioSource day, night, fire, oneShot;
        System.Random rnd = new System.Random(5);
        float Rn() => (float)rnd.NextDouble() * 2 - 1;
        float R01() => (float)rnd.NextDouble();
        public float master = 0.9f;

        public static void Play(string name, float v = 1f, float pitch = 1f)
        {
            if (I == null || !I.clips.TryGetValue(name, out var c)) return;
            I.oneShot.pitch = pitch;
            I.oneShot.PlayOneShot(c, v * I.master * (I.vol.TryGetValue(name, out var b) ? b : 1f));
        }

        public void Build(Vector3 firePos)
        {
            I = this;
            oneShot = gameObject.AddComponent<AudioSource>(); oneShot.spatialBlend = 0; oneShot.playOnAwake = false;
            day = Loop("day", DayLoop(), 0f); night = Loop("night", NightLoop(), 0f);
            var fg = new GameObject("Ateş sesi"); fg.transform.position = firePos;
            fire = fg.AddComponent<AudioSource>(); fire.clip = MakeClip("fire", FireLoop()); fire.loop = true; fire.spatialBlend = 0.75f;
            fire.rolloffMode = AudioRolloffMode.Linear; fire.minDistance = 2; fire.maxDistance = 22; fire.volume = 0.4f; fire.Play();

            Add("tap", Tap(), 0.45f); Add("select", Pluck(523.25f, 0.35f), 0.55f); Add("unselect", Pluck(392f, 0.25f), 0.4f);
            Add("whoosh", Whoosh(0.7f), 0.35f); Add("night", Gong(), 0.7f); Add("day", Chime(new[] { 1046.5f, 1318.5f, 1568f, 2093f }, 0.09f), 0.45f);
            Add("death", Death(), 0.7f); Add("fanfare", Fanfare(), 0.5f); Add("win", Arp(new[] { 523.25f, 659.25f, 783.99f, 1046.5f, 1318.5f }, 0.11f, 1.6f), 0.55f);
            Add("lose", Arp(new[] { 659.25f, 587.33f, 523.25f, 440f, 349.23f }, 0.16f, 1.8f), 0.5f); Add("tick", Tick(), 0.35f);
            Add("notify", Chime(new[] { 880f, 1318.5f }, 0.08f), 0.3f); Add("pop", Pop(), 0.22f); Add("heal", Chime(new[] { 784f, 1046.5f, 1568f }, 0.07f), 0.4f);
            Add("magic", Shimmer(), 0.4f); Add("heart", Pluck(659.25f, 0.5f), 0.4f);
        }

        AudioSource Loop(string name, float[] data, float v)
        {
            var s = gameObject.AddComponent<AudioSource>(); s.clip = MakeClip(name, data); s.loop = true; s.volume = v; s.spatialBlend = 0; s.Play(); return s;
        }
        void Add(string n, float[] d, float v) { clips[n] = MakeClip(n, d); vol[n] = v; }
        static AudioClip MakeClip(string n, float[] d)
        {
            var c = AudioClip.Create(n, d.Length, 1, SR, false); c.SetData(d, 0); return c;
        }

        // gece/gündüz geçişinde atmosferler yumuşakça karışır; gece ateş daha belirgin
        float todNow;
        public void SetTod(float k) { todNow = k; }
        void Update()
        {
            if (day == null) return;
            float k = todNow;
            day.volume = Mathf.MoveTowards(day.volume, 0.32f * (1 - k) * master, Time.deltaTime * 0.5f);
            night.volume = Mathf.MoveTowards(night.volume, 0.38f * k * master, Time.deltaTime * 0.5f);
            fire.volume = Mathf.Lerp(0.3f, 0.55f, k) * master;
        }

        // ---------------------------------------------------------------- sentez yardımcıları
        static float[] Buf(float sec) => new float[(int)(sec * SR)];
        static float[] Trim(float[] a, float fade)
        {
            int n = (int)(fade * SR), L = a.Length; var o = new float[L - n];
            for (int i = 0; i < o.Length; i++) o[i] = a[i];
            for (int i = 0; i < n; i++) { float t = i / (float)n; o[i] = a[i] * t + a[L - n + i] * (1 - t); }
            return o;
        }
        static void Norm(float[] a, float peak)
        {
            float m = 0.0001f; foreach (var v in a) m = Mathf.Max(m, Mathf.Abs(v));
            float k = peak / m; for (int i = 0; i < a.Length; i++) a[i] *= k;
        }

        float[] DayLoop()
        {
            var a = Buf(13); float lp = 0, lp2 = 0;
            for (int i = 0; i < a.Length; i++)
            {
                float t = i / (float)SR;
                lp += (Rn() - lp) * 0.02f; lp2 += (lp - lp2) * 0.05f;
                float gust = 0.55f + 0.45f * Mathf.Sin(t * 0.7f) * Mathf.Sin(t * 0.23f + 1);
                a[i] = lp2 * 3.2f * gust;
            }
            // kuşlar: kısa, perdesi kayan cıvıltı grupları
            for (int g = 0; g < 14; g++)
            {
                float st = R01() * 12f, f0 = 2600 + R01() * 2200; int n = 2 + rnd.Next(4); float amp = 0.12f + R01() * 0.12f;
                for (int c = 0; c < n; c++)
                {
                    float cs = st + c * (0.11f + R01() * 0.06f), dur = 0.06f + R01() * 0.08f, sweep = (R01() - 0.3f) * 1800;
                    int s0 = (int)(cs * SR), len = (int)(dur * SR); float ph = 0;
                    for (int j = 0; j < len && s0 + j < a.Length; j++)
                    {
                        float u = j / (float)len, f = f0 + sweep * u + Mathf.Sin(u * 40) * 120;
                        ph += 2 * Mathf.PI * f / SR;
                        a[s0 + j] += Mathf.Sin(ph) * Mathf.Sin(Mathf.PI * u) * amp;
                    }
                }
            }
            Norm(a, 0.8f); return Trim(a, 1f);
        }

        float[] NightLoop()
        {
            var a = Buf(13); float lp = 0, lp2 = 0;
            for (int i = 0; i < a.Length; i++) { lp += (Rn() - lp) * 0.012f; lp2 += (lp - lp2) * 0.03f; a[i] = lp2 * 2.2f; }
            // cırcır böcekleri: farklı perdelerde, darbeli yüksek ton
            for (int k = 0; k < 5; k++)
            {
                float f = 3900 + R01() * 900, rate = 13 + R01() * 6, off = R01() * 3, amp = 0.05f + R01() * 0.05f;
                for (int i = 0; i < a.Length; i++)
                {
                    float t = i / (float)SR + off;
                    float cycle = t % 1.1f; if (cycle > 0.45f) continue;          // cırr… sus
                    float pulse = Mathf.Max(0, Mathf.Sin(t * rate * 2 * Mathf.PI)); pulse *= pulse * pulse;
                    a[i] += Mathf.Sin(t * f * 2 * Mathf.PI) * pulse * amp;
                }
            }
            // baykuş: iki "huu"
            foreach (float st in new[] { 4.2f, 4.75f, 9.6f })
            {
                int s0 = (int)(st * SR), len = (int)(0.42f * SR); float ph = 0;
                for (int j = 0; j < len && s0 + j < a.Length; j++)
                {
                    float u = j / (float)len, f = 360 + Mathf.Sin(u * Mathf.PI) * 30 + Mathf.Sin(u * 60) * 4;
                    ph += 2 * Mathf.PI * f / SR;
                    float env = Mathf.Sin(Mathf.PI * Mathf.Min(1, u * 1.3f)); env *= env;
                    a[s0 + j] += (Mathf.Sin(ph) + 0.3f * Mathf.Sin(ph * 2)) * env * 0.12f;
                }
            }
            Norm(a, 0.75f); return Trim(a, 1f);
        }

        float[] FireLoop()
        {
            var a = Buf(9); float lp = 0, lp2 = 0;
            for (int i = 0; i < a.Length; i++) { lp += (Rn() - lp) * 0.05f; lp2 += (lp - lp2) * 0.08f; a[i] = lp2 * 1.6f * (0.8f + 0.2f * Mathf.Sin(i / (float)SR * 3.1f)); }
            // çıtırtılar
            int n = 260;
            for (int c = 0; c < n; c++)
            {
                int s0 = rnd.Next(a.Length), len = (int)((0.003f + R01() * 0.02f) * SR); float amp = Mathf.Pow(R01(), 2.2f) * 0.9f; float hp = 0, prev = 0;
                for (int j = 0; j < len && s0 + j < a.Length; j++)
                {
                    float x = Rn(); hp = 0.6f * (hp + x - prev); prev = x;
                    a[s0 + j] += hp * amp * Mathf.Exp(-j / (len * 0.25f));
                }
            }
            Norm(a, 0.8f); return Trim(a, 0.8f);
        }

        float[] Tap()
        {
            var a = Buf(0.07f); float ph = 0;
            for (int i = 0; i < a.Length; i++) { float u = i / (float)a.Length, f = 1100 - 500 * u; ph += 2 * Mathf.PI * f / SR; a[i] = Mathf.Sin(ph) * Mathf.Exp(-u * 7); }
            return a;
        }
        float[] Pluck(float f, float dur)  // Karplus-Strong tel sesi
        {
            var a = Buf(dur + 0.3f); int N = Mathf.Max(2, (int)(SR / f)); var ring = new float[N];
            for (int i = 0; i < N; i++) ring[i] = Rn();
            for (int i = 0; i < a.Length; i++) { int j = i % N, k = (i + 1) % N; float v = 0.497f * (ring[j] + ring[k]); a[i] = ring[j]; ring[j] = v; }
            float[] b = new float[a.Length]; float lp = 0; for (int i = 0; i < a.Length; i++) { lp += (a[i] - lp) * 0.5f; b[i] = lp; }
            Norm(b, 0.9f); return b;
        }
        float[] Whoosh(float dur)
        {
            var a = Buf(dur); float lp = 0, lp2 = 0;
            for (int i = 0; i < a.Length; i++)
            {
                float u = i / (float)a.Length, cut = 0.02f + 0.25f * Mathf.Sin(Mathf.PI * u);
                lp += (Rn() - lp) * cut; lp2 += (lp - lp2) * cut;
                a[i] = lp2 * Mathf.Sin(Mathf.PI * u) * Mathf.Sin(Mathf.PI * u);
            }
            Norm(a, 0.8f); return a;
        }
        float[] Gong()
        {
            var a = Buf(4.2f); float f = 98; float[] parts = { 1, 2.02f, 2.74f, 3.76f, 5.4f }, amps = { 1, 0.6f, 0.45f, 0.25f, 0.15f };
            for (int i = 0; i < a.Length; i++)
            {
                float t = i / (float)SR, v = 0;
                for (int p = 0; p < parts.Length; p++) v += Mathf.Sin(2 * Mathf.PI * f * parts[p] * t + p) * amps[p] * Mathf.Exp(-t * (0.7f + p * 0.45f));
                float att = Mathf.Min(1, t * 60);
                v += Mathf.Sin(2 * Mathf.PI * 49 * t) * 0.5f * Mathf.Sin(Mathf.PI * Mathf.Min(1, t / 4.2f)); // alçak uğultu
                a[i] = v * att;
            }
            Norm(a, 0.85f); return a;
        }
        float[] Bell(float f, float dur, float amp)
        {
            var a = Buf(dur);
            for (int i = 0; i < a.Length; i++) { float t = i / (float)SR; a[i] = (Mathf.Sin(2 * Mathf.PI * f * t) + 0.35f * Mathf.Sin(2 * Mathf.PI * f * 2.76f * t) * Mathf.Exp(-t * 6)) * Mathf.Exp(-t * 3.2f) * amp * Mathf.Min(1, t * 400); }
            return a;
        }
        float[] Chime(float[] notes, float gap)
        {
            var a = Buf(gap * notes.Length + 1.3f);
            for (int n = 0; n < notes.Length; n++) { var b = Bell(notes[n], 1.3f, 0.7f); int s0 = (int)(n * gap * SR); for (int i = 0; i < b.Length && s0 + i < a.Length; i++) a[s0 + i] += b[i]; }
            Norm(a, 0.8f); return a;
        }
        float[] Arp(float[] notes, float gap, float tail)
        {
            var a = Buf(gap * notes.Length + tail);
            for (int n = 0; n < notes.Length; n++)
            {
                int s0 = (int)(n * gap * SR); float f = notes[n];
                for (int i = 0; s0 + i < a.Length; i++)
                {
                    float t = i / (float)SR; float env = Mathf.Exp(-t * 2.2f) * Mathf.Min(1, t * 200);
                    a[s0 + i] += (Mathf.Sin(2 * Mathf.PI * f * t) + 0.3f * Mathf.Sin(4 * Mathf.PI * f * t) + 0.12f * Mathf.Sin(6 * Mathf.PI * f * t)) * env * 0.5f;
                }
            }
            Norm(a, 0.8f); return a;
        }
        float[] Death()
        {
            var a = Buf(2.2f); float ph = 0, lp = 0;
            float[] chord = { 110f, 130.81f, 155.56f, 116.54f };
            for (int i = 0; i < a.Length; i++)
            {
                float t = i / (float)SR;
                float f = 70 * Mathf.Exp(-t * 3) + 38; ph += 2 * Mathf.PI * f / SR;
                float thump = Mathf.Sin(ph) * Mathf.Exp(-t * 4) * 1.1f;
                float sw = 0; foreach (var c in chord) sw += Mathf.Sin(2 * Mathf.PI * c * t) + 0.4f * Mathf.Sin(4 * Mathf.PI * c * t + 1);
                float swell = sw * 0.18f * Mathf.Sin(Mathf.PI * Mathf.Min(1, t / 2.2f));
                lp += (Rn() - lp) * 0.08f;
                a[i] = thump + swell + lp * Mathf.Exp(-t * 6) * 0.5f;
            }
            Norm(a, 0.85f); return a;
        }
        float[] Fanfare()
        {
            float[] notes = { 392f, 523.25f, 659.25f, 783.99f }; var a = Buf(2.0f);
            for (int n = 0; n < notes.Length; n++)
            {
                int s0 = (int)(n * 0.12f * SR); float f = notes[n], lp = 0;
                for (int i = 0; s0 + i < a.Length; i++)
                {
                    float t = i / (float)SR, saw = 2 * ((f * t) % 1f) - 1;
                    lp += (saw - lp) * 0.18f;
                    float env = Mathf.Min(1, t * 30) * (n == notes.Length - 1 ? Mathf.Exp(-t * 1.3f) : Mathf.Exp(-t * 5));
                    a[s0 + i] += lp * env * 0.4f;
                }
            }
            Norm(a, 0.8f); return a;
        }
        float[] Tick()
        {
            var a = Buf(0.05f);
            for (int i = 0; i < a.Length; i++) { float t = i / (float)SR; a[i] = (Mathf.Sin(2 * Mathf.PI * 1500 * t) + 0.5f * Mathf.Sin(2 * Mathf.PI * 2350 * t)) * Mathf.Exp(-t * 120); }
            Norm(a, 0.8f); return a;
        }
        float[] Pop()
        {
            var a = Buf(0.09f); float ph = 0;
            for (int i = 0; i < a.Length; i++) { float u = i / (float)a.Length; ph += 2 * Mathf.PI * (420 + 700 * u) / SR; a[i] = Mathf.Sin(ph) * Mathf.Sin(Mathf.PI * u); }
            return a;
        }
        float[] Shimmer()
        {
            var a = Buf(1.4f);
            for (int n = 0; n < 9; n++)
            {
                float f = 1400 + n * 260 + R01() * 120; int s0 = (int)(n * 0.05f * SR);
                for (int i = 0; s0 + i < a.Length; i++) { float t = i / (float)SR; a[s0 + i] += Mathf.Sin(2 * Mathf.PI * f * t) * Mathf.Exp(-t * 3.5f) * Mathf.Min(1, t * 300) * 0.25f; }
            }
            Norm(a, 0.7f); return a;
        }
    }
}
