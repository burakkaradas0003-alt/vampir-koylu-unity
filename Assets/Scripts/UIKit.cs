// Zuplay tasarım sistemi: tüm ekranlar (menü, lobi, mağaza, oyun içi) aynı renkleri, köşeleri,
// yazı tiplerini, düğmeleri ve geçiş animasyonlarını buradan alır.
// Dil: koyu füme + antrasit zemin, mor vurgu, ince açık kenarlı cam paneller, yumuşak gölgeler.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VK
{
    public static class UI
    {
        // ---------------------------------------------------------------- renkler
        public static readonly Color Ink = new Color(0.055f, 0.058f, 0.08f);          // en koyu füme
        public static readonly Color Anthracite = new Color(0.09f, 0.094f, 0.125f);
        public static readonly Color Glass = new Color(0.075f, 0.078f, 0.11f, 0.78f);
        public static readonly Color GlassLight = new Color(0.14f, 0.14f, 0.2f, 0.72f);
        public static readonly Color Stroke = new Color(0.93f, 0.72f, 0.3f, 0.8f); // tüm düğme ve panel kenarları altın sarısı
        public static readonly Color Purple = new Color(0.545f, 0.36f, 0.965f);       // #8B5CF6
        public static readonly Color PurpleDeep = new Color(0.36f, 0.2f, 0.75f);
        public static readonly Color Lilac = new Color(0.72f, 0.62f, 1f);
        public static readonly Color Gold = new Color(0.97f, 0.78f, 0.29f);
        public static readonly Color Fg = new Color(0.95f, 0.95f, 0.97f);
        public static readonly Color Muted = new Color(0.62f, 0.63f, 0.72f);
        public static readonly Color Good = new Color(0.2f, 0.83f, 0.6f);
        public static readonly Color Bad = new Color(0.97f, 0.44f, 0.44f);
        public static Color A(Color c, float a) { c.a = a; return c; }

        // ---------------------------------------------------------------- kaynaklar
        public static Font Body, Bold, Title, Display; // Display: başlıklar için gotik yazı (Pirata One)
        public static Sprite Round, RoundSmall, Pill, Outline, OutlinePill, Circle, Ring, Grad, GradH, Glow, Soft;
        static bool ready;
        public static void Init()
        {
            if (ready) return; ready = true;
            var legacy = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Body = Resources.Load<Font>("Fonts/Body") ?? legacy; Bold = Resources.Load<Font>("Fonts/BodyBold") ?? legacy; Title = Resources.Load<Font>("Fonts/Title") ?? Bold; Display = Resources.Load<Font>("Fonts/Display") ?? Title;
            Round = RoundRect(64, 18, 0, true); RoundSmall = RoundRect(32, 9, 0, true); Pill = RoundRect(64, 31, 0, false);
            Outline = RoundRect(64, 18, 2.6f, false); OutlinePill = RoundRect(64, 31, 2.6f, false);
            Circle = Disc(128, 0); Ring = Disc(128, 5f); Grad = Gradient(false); GradH = Gradient(true); Glow = GlowS(); Soft = SoftRect();
        }

        static Sprite RoundRect(int s, float r, float border, bool shade)
        {
            var t = new Texture2D(s, s, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
                {
                    float cx = Mathf.Clamp(x + 0.5f, r, s - r), cy = Mathf.Clamp(y + 0.5f, r, s - r);
                    float d = Mathf.Sqrt((x + 0.5f - cx) * (x + 0.5f - cx) + (y + 0.5f - cy) * (y + 0.5f - cy));
                    float a = Mathf.Clamp01(r - d + 0.5f);
                    if (border > 0) a *= Mathf.Clamp01(d - (r - border) + 0.5f) + (cx == x + 0.5f && cy == y + 0.5f ? 0 : 0);
                    if (border > 0)
                    {
                        // düz kenarlarda da çizgi: kenara olan uzaklık
                        float e = Mathf.Min(Mathf.Min(x + 0.5f, s - x - 0.5f), Mathf.Min(y + 0.5f, s - y - 0.5f));
                        float edgeA = Mathf.Clamp01(border - e + 0.5f);
                        bool corner = (x + 0.5f < r || x + 0.5f > s - r) && (y + 0.5f < r || y + 0.5f > s - r);
                        a = corner ? Mathf.Clamp01(r - d + 0.5f) * Mathf.Clamp01(d - (r - border) + 0.5f) : edgeA;
                    }
                    float lum = shade ? Mathf.Lerp(0.9f, 1.05f, y / (float)s) : 1;
                    t.SetPixel(x, y, new Color(lum, lum, lum, a));
                }
            t.Apply();
            float b = Mathf.Ceil(r) + 2;
            return Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
        }
        static Sprite Disc(int s, float border)
        {
            var t = new Texture2D(s, s, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            float R = s / 2f;
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
                {
                    float d = Mathf.Sqrt((x + 0.5f - R) * (x + 0.5f - R) + (y + 0.5f - R) * (y + 0.5f - R));
                    float a = Mathf.Clamp01(R - 0.5f - d + 0.5f);
                    if (border > 0) a *= Mathf.Clamp01(d - (R - 0.5f - border) + 0.5f);
                    t.SetPixel(x, y, new Color(1, 1, 1, a));
                }
            t.Apply(); return Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f));
        }
        static Sprite Gradient(bool horizontal)
        {
            var t = new Texture2D(horizontal ? 64 : 4, horizontal ? 4 : 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < t.height; y++) for (int x = 0; x < t.width; x++) { float u = horizontal ? x / 63f : y / 63f; t.SetPixel(x, y, new Color(1, 1, 1, Mathf.Pow(1 - u, 1.6f))); }
            t.Apply(); return Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f));
        }
        static Sprite GlowS()
        {
            int s = 64; var t = new Texture2D(s, s, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++) { float d = Mathf.Sqrt((x - 31.5f) * (x - 31.5f) + (y - 31.5f) * (y - 31.5f)) / 31.5f; float a = Mathf.Clamp01(1 - d); t.SetPixel(x, y, new Color(1, 1, 1, a * a)); }
            t.Apply(); return Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f));
        }
        // yumuşak gölge: kenarları bulanık dikdörtgen (dilimlenebilir)
        static Sprite SoftRect()
        {
            int s = 64; var t = new Texture2D(s, s, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
                {
                    float e = Mathf.Min(Mathf.Min(x + 0.5f, s - x - 0.5f), Mathf.Min(y + 0.5f, s - y - 0.5f));
                    float a = Mathf.SmoothStep(0, 1, Mathf.Clamp01(e / 24f));
                    t.SetPixel(x, y, new Color(1, 1, 1, a));
                }
            t.Apply(); return Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(26, 26, 26, 26));
        }

        // ---------------------------------------------------------------- yerleşim
        public static RectTransform R(string n, Transform p)
        {
            var g = new GameObject(n, typeof(RectTransform)); var r = (RectTransform)g.transform; r.SetParent(p, false); return r;
        }
        public static RectTransform Fill(this RectTransform r, float l = 0, float b = 0, float rt = 0, float t = 0)
        { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = new Vector2(l, b); r.offsetMax = new Vector2(-rt, -t); return r; }
        public static RectTransform At(this RectTransform r, Vector2 anchor, Vector2 pos, Vector2 size, Vector2? pivot = null)
        { r.anchorMin = r.anchorMax = anchor; r.pivot = pivot ?? anchor; r.anchoredPosition = pos; r.sizeDelta = size; return r; }
        public static LayoutElement LE(this RectTransform r, float w = -1, float h = -1, float flexW = -1, float flexH = -1)
        {
            var le = r.GetComponent<LayoutElement>() ?? r.gameObject.AddComponent<LayoutElement>();
            if (w >= 0) le.preferredWidth = le.minWidth = w; if (h >= 0) le.preferredHeight = le.minHeight = h; if (flexW >= 0) le.flexibleWidth = flexW; if (flexH >= 0) le.flexibleHeight = flexH; return le;
        }
        public static HorizontalLayoutGroup HRow(RectTransform r, float spacing = 8, TextAnchor al = TextAnchor.MiddleLeft, RectOffset pad = null, bool expandW = false)
        {
            var h = r.gameObject.AddComponent<HorizontalLayoutGroup>(); h.spacing = spacing; h.childAlignment = al; h.childControlWidth = true; h.childControlHeight = true;
            h.childForceExpandWidth = expandW; h.childForceExpandHeight = false; if (pad != null) h.padding = pad; return h;
        }
        public static VerticalLayoutGroup VCol(RectTransform r, float spacing = 8, TextAnchor al = TextAnchor.UpperLeft, RectOffset pad = null, bool expandW = true)
        {
            var v = r.gameObject.AddComponent<VerticalLayoutGroup>(); v.spacing = spacing; v.childAlignment = al; v.childControlWidth = true; v.childControlHeight = true;
            v.childForceExpandWidth = expandW; v.childForceExpandHeight = false; if (pad != null) v.padding = pad; return v;
        }
        public static RectOffset Pad(int l, int r, int t, int b) => new RectOffset(l, r, t, b);

        // ---------------------------------------------------------------- bileşenler
        public static Image Img(Transform p, Sprite s, Color c, string n = "img")
        {
            var r = R(n, p); var im = r.gameObject.AddComponent<Image>(); im.sprite = s; im.color = c; im.raycastTarget = false;
            if (s == Round || s == RoundSmall || s == Pill || s == Outline || s == OutlinePill || s == Soft) { im.type = Image.Type.Sliced; im.pixelsPerUnitMultiplier = s == RoundSmall ? 1 : 2.2f; }
            return im;
        }
        public static Image Icon(Transform p, Sprite s, float size, string n = "ikon")
        {
            var im = Img(p, s, Color.white, n); im.preserveAspect = true; im.rectTransform.sizeDelta = new Vector2(size, size); im.enabled = s != null; return im;
        }
        // cam panel: yumuşak gölge + koyu yarı saydam zemin + ince açık kenar
        public static RectTransform Panel(Transform p, string n = "panel", Color? bg = null, bool shadow = true, Sprite shape = null, bool stroke = true)
        {
            var r = R(n, p); shape = shape ?? Round;
            if (shadow) { var sh = Img(r, Soft, new Color(0, 0, 0, 0.42f), "gölge"); sh.rectTransform.Fill(-14, -18, -14, -10); }
            var bgI = r.gameObject.AddComponent<Image>(); bgI.sprite = shape; bgI.type = Image.Type.Sliced; bgI.pixelsPerUnitMultiplier = 2.2f; bgI.color = bg ?? Glass;
            if (stroke) { var o = Img(r, shape == Pill ? OutlinePill : Outline, Stroke, "kenar"); o.rectTransform.Fill(); }
            return r;
        }
        public static Text Txt(Transform p, string s, int size, Color? c = null, Font f = null, TextAnchor al = TextAnchor.MiddleLeft, string n = "yazı")
        {
            var r = R(n, p); var t = r.gameObject.AddComponent<Text>(); t.font = f ?? Body; t.fontSize = size; t.color = c ?? Fg; t.alignment = al; t.text = s;
            t.supportRichText = true; t.raycastTarget = false; t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow; t.lineSpacing = 0.95f;
            return t;
        }
        public static Text Shadowed(this Text t, float a = 0.5f) { var s = t.gameObject.AddComponent<Shadow>(); s.effectColor = new Color(0, 0, 0, a); s.effectDistance = new Vector2(0, -1.5f); return t; }

        public enum Style { Primary, Secondary, Gold, Ghost, Danger, Good }
        public class Btn { public RectTransform r; public Image bg; public Text label; public Image icon; public Button b; public Style style; public bool on = true;
            public Btn Label(string s) { if (label != null) label.text = s; return this; }
            public Btn Enabled(bool e) { on = e; b.interactable = e; var cg = r.GetComponent<CanvasGroup>() ?? r.gameObject.AddComponent<CanvasGroup>(); cg.alpha = e ? 1 : 0.42f; return this; }
            public Btn Restyle(Style s) { style = s; Paint(this); return this; }
        }
        public static Btn Button(Transform p, string label, Style st, Action onClick, Sprite icon = null, int fontSize = 14, string sound = "tap")
        {
            var r = R("düğme", p);
            var b = new Btn { r = r, style = st };
            var sh = Img(r, Soft, new Color(0, 0, 0, st == Style.Ghost ? 0 : 0.35f), "gölge"); sh.rectTransform.Fill(-8, -12, -8, -4);
            b.bg = r.gameObject.AddComponent<Image>(); b.bg.sprite = Pill; b.bg.type = Image.Type.Sliced; b.bg.pixelsPerUnitMultiplier = 2.2f;
            var hl = Img(r, Pill, new Color(1, 1, 1, 0.12f), "parlak"); hl.rectTransform.Fill(2, 0, 2, 0); hl.rectTransform.anchorMin = new Vector2(0, 0.52f); hl.rectTransform.offsetMin = new Vector2(3, 0); hl.rectTransform.offsetMax = new Vector2(-3, -2);
            var o = Img(r, OutlinePill, Stroke, "kenar"); o.rectTransform.Fill();
            var row = R("içerik", r).Fill(12, 0, 12, 0); HRow(row, 6, TextAnchor.MiddleCenter);
            if (icon != null) { b.icon = Icon(row, icon, fontSize + 6); b.icon.rectTransform.LE(fontSize + 6, fontSize + 6); }
            if (!string.IsNullOrEmpty(label)) { b.label = Txt(row, label, fontSize, null, Bold, TextAnchor.MiddleCenter); b.label.horizontalOverflow = HorizontalWrapMode.Overflow; b.label.rectTransform.LE(-1, 24); }
            b.b = r.gameObject.AddComponent<Button>(); b.b.transition = Selectable.Transition.None; b.b.targetGraphic = b.bg;
            b.b.onClick.AddListener(() => { if (!b.on) return; if (sound != null) Sfx.Play(sound); Tween.Punch(r, 0.92f); onClick?.Invoke(); });
            Paint(b);
            return b;
        }
        static void Paint(Btn b)
        {
            Color bg, fg = Fg;
            switch (b.style)
            {
                case Style.Primary: bg = Purple; break;
                case Style.Gold: bg = Gold; fg = new Color(0.16f, 0.11f, 0.02f); break;
                case Style.Good: bg = new Color(0.16f, 0.66f, 0.47f); break;
                case Style.Danger: bg = new Color(0.82f, 0.27f, 0.32f); break;
                case Style.Ghost: bg = new Color(1, 1, 1, 0.04f); fg = Muted; break;
                default: bg = new Color(1, 1, 1, 0.09f); break;
            }
            b.bg.color = bg; if (b.label != null) b.label.color = fg;
        }

        // seçilebilir hap (filtre/kategori)
        public static Btn Chip(Transform p, string label, bool on, Action f, Sprite icon = null)
        {
            var b = Button(p, label, on ? Style.Primary : Style.Secondary, f, icon, 12);
            return b;
        }
        public static RectTransform Badge(Transform p, string s, Color bg, Color fg, int size = 11)
        {
            var r = R("rozet", p); var im = r.gameObject.AddComponent<Image>(); im.sprite = Pill; im.type = Image.Type.Sliced; im.pixelsPerUnitMultiplier = 2.2f; im.color = bg; im.raycastTarget = false;
            var t = Txt(r, s, size, fg, Bold, TextAnchor.MiddleCenter); t.rectTransform.Fill(8, 0, 8, 0); t.horizontalOverflow = HorizontalWrapMode.Overflow;
            var cs = r.gameObject.AddComponent<ContentSizeFitter>(); cs.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            var h = HRow(r, 0, TextAnchor.MiddleCenter, Pad(8, 8, 2, 2)); h.childControlWidth = true;
            return r;
        }
        // ilerleme çubuğu: doluluk 0..1
        public class Bar { public RectTransform r; public Image fill; public float v; public void Set(float k) { v = Mathf.Clamp01(k); fill.rectTransform.anchorMax = new Vector2(Mathf.Max(0.02f, v), 1); fill.enabled = v > 0.001f; } }
        public static Bar ProgressBar(Transform p, Color c, float h = 8)
        {
            var r = R("çubuk", p); var bg = r.gameObject.AddComponent<Image>(); bg.sprite = Pill; bg.type = Image.Type.Sliced; bg.pixelsPerUnitMultiplier = 4f; bg.color = new Color(1, 1, 1, 0.08f); bg.raycastTarget = false;
            var f = Img(r, Pill, c, "dolu"); f.pixelsPerUnitMultiplier = 4f; f.rectTransform.anchorMin = Vector2.zero; f.rectTransform.anchorMax = new Vector2(0.5f, 1); f.rectTransform.offsetMin = f.rectTransform.offsetMax = Vector2.zero;
            r.LE(-1, h);
            var b = new Bar { r = r, fill = f }; b.Set(0); return b;
        }
        public static ScrollRect Scroll(Transform p, bool vertical, out RectTransform content, float spacing = 8, RectOffset pad = null)
        {
            var r = R("kaydır", p); var sr = r.gameObject.AddComponent<ScrollRect>();
            var vp = R("görünüm", r).Fill(); vp.gameObject.AddComponent<RectMask2D>(); var vpi = vp.gameObject.AddComponent<Image>(); vpi.color = new Color(0, 0, 0, 0.001f);
            content = R("içerik", vp);
            if (vertical) { content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(0.5f, 1); content.sizeDelta = Vector2.zero; VCol(content, spacing, TextAnchor.UpperLeft, pad ?? Pad(2, 2, 2, 2)); content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize; }
            else { content.anchorMin = new Vector2(0, 0); content.anchorMax = new Vector2(0, 1); content.pivot = new Vector2(0, 0.5f); content.sizeDelta = Vector2.zero; var h = HRow(content, spacing, TextAnchor.MiddleLeft, pad ?? Pad(2, 2, 2, 2)); h.childForceExpandHeight = false; content.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize; }
            sr.viewport = vp; sr.content = content; sr.horizontal = !vertical; sr.vertical = vertical; sr.movementType = ScrollRect.MovementType.Elastic; sr.scrollSensitivity = 18; sr.inertia = true; sr.decelerationRate = 0.12f;
            return sr;
        }
        public static GridLayoutGroup Grid(RectTransform content, Vector2 cell, Vector2 spacing, int columns)
        {
            var old = content.GetComponent<VerticalLayoutGroup>(); if (old != null) UnityEngine.Object.DestroyImmediate(old);
            var g = content.gameObject.AddComponent<GridLayoutGroup>(); g.cellSize = cell; g.spacing = spacing; g.constraint = GridLayoutGroup.Constraint.FixedColumnCount; g.constraintCount = columns; g.padding = Pad(2, 2, 2, 6);
            return g;
        }
        public static void Clear(Transform t) { for (int i = t.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(t.GetChild(i).gameObject); }
        public static void Click(RectTransform r, Action f, string sound = "tap")
        {
            var b = r.GetComponent<Button>() ?? r.gameObject.AddComponent<Button>(); b.transition = Selectable.Transition.None;
            if (r.GetComponent<Graphic>() == null) { var im = r.gameObject.AddComponent<Image>(); im.color = new Color(0, 0, 0, 0.001f); }
            b.onClick.AddListener(() => { if (sound != null) Sfx.Play(sound); Tween.Punch(r, 0.95f); f(); });
        }

        // çerçeveli yuvarlak portre (kozmetik çerçeve rengiyle)
        public static RawImage Avatar(Transform p, float size, Texture tex, string frameId, out Image ring)
        {
            var r = R("avatar", p); r.sizeDelta = new Vector2(size, size);
            var bg = Img(r, Circle, new Color(0.17f, 0.15f, 0.27f), "zemin"); bg.rectTransform.Fill();
            var gl = Img(r, Glow, A(Purple, 0.35f), "ışık"); gl.rectTransform.Fill(size * 0.1f, size * 0.1f, size * 0.1f, size * 0.25f);
            var mask = R("maske", r).Fill(2, 2, 2, 2); var mi = mask.gameObject.AddComponent<Image>(); mi.sprite = Circle; mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var raw = R("portre", mask).Fill(); var ri = raw.gameObject.AddComponent<RawImage>(); ri.texture = tex; ri.raycastTarget = false; ri.enabled = tex != null;
            ring = Img(r, Ring, FrameColor(frameId), "çerçeve"); ring.rectTransform.Fill(-1, -1, -1, -1);
            return ri;
        }
        public static Color FrameColor(string frameId) { var it = Catalog.Get(frameId); return it != null ? it.tint : new Color(1, 1, 1, 0.25f); }

        public static Canvas Canvas(string n, int order)
        {
            Init();
            var go = new GameObject(n, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var cv = go.GetComponent<Canvas>(); cv.renderMode = RenderMode.ScreenSpaceOverlay; cv.sortingOrder = order;
            var sc = go.GetComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(844, 390); sc.matchWidthOrHeight = 0.6f;
            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() == null) new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            return cv;
        }
    }

    // çentik / yuvarlak köşe güvenli alanı (iOS, Android)
    public class SafeArea : MonoBehaviour
    {
        Rect last; RectTransform r;
        void Awake() { r = (RectTransform)transform; Apply(); }
        void Update() { if (Screen.safeArea != last) Apply(); }
        void Apply()
        {
            last = Screen.safeArea; if (Screen.width <= 0 || Screen.height <= 0) return;
            var min = last.position; var max = last.position + last.size;
            r.anchorMin = new Vector2(min.x / Screen.width, min.y / Screen.height); r.anchorMax = new Vector2(max.x / Screen.width, max.y / Screen.height);
            r.offsetMin = r.offsetMax = Vector2.zero;
        }
    }

    // kısa ve akıcı arayüz animasyonları (ölçek, saydamlık, kaydırma, sayı sayma)
    public class Tween : MonoBehaviour
    {
        static Tween I;
        class T { public UnityEngine.Object key; public float t0, dur; public Action<float> step; public Action done; public Func<float, float> ease; }
        readonly List<T> list = new List<T>();
        static Tween Get() { if (I == null) { var g = new GameObject("Animasyon"); DontDestroyOnLoad(g); I = g.AddComponent<Tween>(); } return I; }
        public static float OutCubic(float u) => 1 - Mathf.Pow(1 - u, 3);
        public static float OutBack(float u) { const float c1 = 1.70158f, c3 = c1 + 1; float x = u - 1; return 1 + c3 * x * x * x + c1 * x * x; }
        public static float InOut(float u) => u < 0.5f ? 4 * u * u * u : 1 - Mathf.Pow(-2 * u + 2, 3) / 2;
        public static void Run(UnityEngine.Object key, float dur, Action<float> step, Func<float, float> ease = null, float delay = 0, Action done = null)
        {
            var me = Get(); if (key != null) me.list.RemoveAll(x => x.key == key);
            me.list.Add(new T { key = key, t0 = Time.unscaledTime + delay, dur = Mathf.Max(0.01f, dur), step = step, ease = ease ?? OutCubic, done = done });
            if (delay <= 0) step(0);
        }
        void Update()
        {
            float now = Time.unscaledTime;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (i >= list.Count) continue;
                var t = list[i]; if (t.key == null && !ReferenceEquals(t.key, null)) { list.RemoveAt(i); continue; }
                float u = (now - t.t0) / t.dur; if (u < 0) continue; u = Mathf.Clamp01(u);
                try { t.step(t.ease(u)); } catch { list.Remove(t); continue; }
                if (u >= 1) { list.Remove(t); t.done?.Invoke(); }
            }
        }
        public static void Scale(RectTransform r, float from, float to, float dur = 0.3f, float delay = 0, bool back = true)
        { if (r == null) return; r.localScale = Vector3.one * from; Run(r, dur, k => { if (r) r.localScale = Vector3.one * Mathf.LerpUnclamped(from, to, k); }, back ? (Func<float, float>)OutBack : OutCubic, delay); }
        public static void Punch(RectTransform r, float s = 0.92f) => Scale(r, s, 1, 0.22f);
        public static void Fade(CanvasGroup g, float from, float to, float dur = 0.25f, float delay = 0, Action done = null)
        { if (g == null) return; g.alpha = from; Run(g, dur, k => { if (g) g.alpha = Mathf.Lerp(from, to, k); }, OutCubic, delay, done); }
        public static void Move(RectTransform r, Vector2 from, Vector2 to, float dur = 0.3f, float delay = 0)
        { if (r == null) return; r.anchoredPosition = from; var key = r.GetComponent<Transform>(); Run(r.gameObject, dur, k => { if (r) r.anchoredPosition = Vector2.LerpUnclamped(from, to, k); }, OutCubic, delay); }
        public static void Count(Text t, int from, int to, float dur = 0.9f, float delay = 0, string fmt = "{0}")
        { if (t == null) return; Run(t, dur, k => { if (t) t.text = string.Format(fmt, Mathf.RoundToInt(Mathf.Lerp(from, to, k))); }, OutCubic, delay); }
        // ekran geçişi: hafif kayarak ve belirerek gelir
        public static void ShowScreen(RectTransform r, float dx = 26)
        {
            var cg = r.GetComponent<CanvasGroup>() ?? r.gameObject.AddComponent<CanvasGroup>(); r.gameObject.SetActive(true);
            var p = r.anchoredPosition; Fade(cg, 0, 1, 0.22f);
            Run(r.gameObject, 0.28f, k => { if (r) r.anchoredPosition = new Vector2(Mathf.Lerp(dx, 0, k), p.y); }, OutCubic);
        }
    }
}
