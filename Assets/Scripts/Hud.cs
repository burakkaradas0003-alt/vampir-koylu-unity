// Arayüz: kodla kurulan uGUI. Emojiler Twemoji görselleri (Resources/Emoji) olarak gösterilir.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VK
{
    public class Hud : MonoBehaviour
    {
        public static Hud I;
        Font font; Sprite round;
        RectTransform canvas;
        Text phT, phS, timer, rcName, toastT, b1, b2, sleepT, sleepS, hint;
        Image rcIcon, rcBorder, sleepI, bIcon;
        CanvasGroup toastG, bannerG, actG, cardG, endG, emoG;
        RectTransform lidTop, lidBot, logBox, actBtns, tagsRoot, endGrid, emoRow;
        Text actT, actD; Image actI;
        InputField input; Image cardIcon; Text cardName, cardTeam, cardDesc; Button cardBtn; Text cardBtnT;
        Text endTitle, endSub;
        public Action<string> onSend; public Action<string> onEmote; public Action onBack, onRoleChip, onAgain;
        public static readonly Color BG = new Color(0.05f, 0.06f, 0.13f, 0.82f);

        public static Sprite Emoji(string k) => string.IsNullOrEmpty(k) ? null : Resources.Load<Sprite>("Emoji/" + k);

        public void Build()
        {
            I = this;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            round = MakeRound();
            var cgo = new GameObject("Arayüz", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var cv = cgo.GetComponent<Canvas>(); cv.renderMode = RenderMode.ScreenSpaceOverlay; cv.sortingOrder = 10;
            var sc = cgo.GetComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(844, 390); sc.matchWidthOrHeight = 0.6f;
            canvas = cgo.GetComponent<RectTransform>();
            if (FindFirstObjectByType<EventSystem>() == null) new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            tagsRoot = Rect("etiketler", canvas, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            // göz kapakları (gece)
            lidTop = Panel("lidT", canvas, new Color(0.01f, 0.01f, 0.03f, 0.97f), new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -0), new Vector2(0, 0)); lidTop.pivot = new Vector2(0.5f, 1);
            lidBot = Panel("lidB", canvas, new Color(0.01f, 0.01f, 0.03f, 0.97f), new Vector2(0, 0), new Vector2(1, 0), Vector2.zero, Vector2.zero); lidBot.pivot = new Vector2(0.5f, 0);
            sleepI = Img("si", lidBot, null, new Vector2(0.5f, 1), new Vector2(0, 70), new Vector2(46, 46));
            sleepT = Label("st", lidBot, "", 20, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0.5f, 1), new Vector2(0, 30), new Vector2(600, 30));
            sleepS = Label("ss", lidBot, "", 13, FontStyle.Normal, TextAnchor.MiddleCenter, new Vector2(0.5f, 1), new Vector2(0, 8), new Vector2(600, 20)); sleepS.color = new Color(0.6f, 0.64f, 0.8f);

            // üst çubuk
            var back = Panel("geri", canvas, BG, new Vector2(0, 1), new Vector2(0, 1), new Vector2(34, -30), new Vector2(40, 40), true);
            Label("<", back, "‹", 26, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 2), new Vector2(40, 40));
            Btn(back, () => onBack?.Invoke());
            var chip = Panel("rol", canvas, BG, new Vector2(0, 1), new Vector2(0, 1), new Vector2(124, -30), new Vector2(118, 40), true);
            rcBorder = chip.GetComponent<Image>();
            rcIcon = Img("ri", chip, null, new Vector2(0, 0.5f), new Vector2(22, 0), new Vector2(24, 24));
            rcName = Label("rn", chip, "", 14, FontStyle.Bold, TextAnchor.MiddleLeft, new Vector2(0, 0.5f), new Vector2(76, 0), new Vector2(80, 30));
            Btn(chip, () => onRoleChip?.Invoke());
            phT = Label("ph", canvas, "VAMPİR KÖYLÜ", 22, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0.5f, 1), new Vector2(0, -22), new Vector2(500, 30));
            phS = Label("phs", canvas, "", 13, FontStyle.Normal, TextAnchor.MiddleCenter, new Vector2(0.5f, 1), new Vector2(0, -44), new Vector2(560, 20));
            var tm = Panel("süre", canvas, BG, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-46, -30), new Vector2(70, 36), true);
            timer = Label("t", tm, "", 18, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(70, 36)); timer.color = new Color(0.96f, 0.77f, 0.26f);
            tm.gameObject.SetActive(false);

            // sohbet günlüğü
            logBox = Rect("log", canvas, new Vector2(0, 0), new Vector2(0, 0), new Vector2(12, 62), new Vector2(330, 200)); logBox.pivot = Vector2.zero;
            var vl = logBox.gameObject.AddComponent<VerticalLayoutGroup>(); vl.childAlignment = TextAnchor.LowerLeft; vl.spacing = 4; vl.childControlHeight = true; vl.childControlWidth = false; vl.childForceExpandHeight = false; vl.childForceExpandWidth = false;
            // yazma alanı
            var emoBtn = Panel("emo", canvas, BG, Vector2.zero, Vector2.zero, new Vector2(32, 28), new Vector2(40, 40), true);
            Img("e", emoBtn, Emoji("laugh"), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(24, 24));
            Btn(emoBtn, () => emoG.gameObject.SetActive(!emoG.gameObject.activeSelf));
            var inBox = Panel("yaz", canvas, BG, Vector2.zero, Vector2.zero, new Vector2(170, 28), new Vector2(224, 40), true);
            var it = Label("metin", inBox, "", 14, FontStyle.Normal, TextAnchor.MiddleLeft, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200, 34));
            var ph = Label("ipucu", inBox, "Bir şey yaz…", 14, FontStyle.Normal, TextAnchor.MiddleLeft, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200, 34)); ph.color = new Color(1, 1, 1, 0.45f);
            input = inBox.gameObject.AddComponent<InputField>(); input.textComponent = it; input.placeholder = ph; input.characterLimit = 120;
            input.onEndEdit.AddListener(s => { if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) Send(); });
            var send = Panel("gönder", canvas, BG, Vector2.zero, Vector2.zero, new Vector2(306, 28), new Vector2(40, 40), true);
            Label("›", send, "➤", 18, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40, 40));
            Btn(send, Send);
            emoRow = Panel("emojiler", canvas, BG, Vector2.zero, Vector2.zero, new Vector2(170, 72), new Vector2(310, 40), true);
            emoG = emoRow.gameObject.AddComponent<CanvasGroup>();
            string[] emos = { "wave", "up", "down", "laugh", "angry", "think", "scream", "cry", "heart" };
            for (int i = 0; i < emos.Length; i++) { string k = emos[i]; var b = Img("e" + i, emoRow, Emoji(k), new Vector2(0, 0.5f), new Vector2(20 + i * 34, 0), new Vector2(26, 26)); Btn(b.rectTransform, () => { onEmote?.Invoke(k); emoRow.gameObject.SetActive(false); }); }
            emoRow.gameObject.SetActive(false);

            // eylem kartı
            var act = Panel("eylem", canvas, BG, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-170, 76), new Vector2(316, 132), true);
            actG = act.gameObject.AddComponent<CanvasGroup>();
            actI = Img("i", act, null, new Vector2(0, 1), new Vector2(30, -30), new Vector2(34, 34));
            actT = Label("t", act, "", 17, FontStyle.Bold, TextAnchor.MiddleLeft, new Vector2(0, 1), new Vector2(170, -22), new Vector2(240, 24));
            actD = Label("d", act, "", 12, FontStyle.Normal, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(170, -56), new Vector2(240, 42)); actD.color = new Color(0.75f, 0.78f, 0.9f);
            actBtns = Rect("düğmeler", act, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 26), new Vector2(-20, 38));
            var hl = actBtns.gameObject.AddComponent<HorizontalLayoutGroup>(); hl.spacing = 8; hl.childForceExpandWidth = true; hl.childControlWidth = true; hl.childControlHeight = true;
            act.gameObject.SetActive(false);

            // toast, banner, ipucu
            var toast = Panel("toast", canvas, BG, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -84), new Vector2(460, 34), true);
            toastG = toast.gameObject.AddComponent<CanvasGroup>(); toastG.alpha = 0;
            toastT = Label("t", toast, "", 14, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(450, 30));
            var ban = Rect("banner", canvas, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(760, 130));
            bannerG = ban.gameObject.AddComponent<CanvasGroup>(); bannerG.alpha = 0; bannerG.blocksRaycasts = false;
            bIcon = Img("i", ban, null, new Vector2(0.5f, 1), new Vector2(0, -24), new Vector2(46, 46));
            b1 = Label("b1", ban, "", 34, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, -8), new Vector2(760, 46));
            b2 = Label("b2", ban, "", 15, FontStyle.Normal, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, -40), new Vector2(700, 24));
            hint = Label("hint", canvas, "← Kaydırarak bak · Seçmek için kişiye dokun →", 13, FontStyle.Normal, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, -20), new Vector2(500, 26));

            // rol kartı
            var card = Panel("kartArka", canvas, new Color(0, 0, 0, 0.6f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            cardG = card.gameObject.AddComponent<CanvasGroup>();
            var front = Panel("kart", card, new Color(0.08f, 0.09f, 0.18f, 0.98f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300, 300), true);
            cardIcon = Img("i", front, null, new Vector2(0.5f, 1), new Vector2(0, -56), new Vector2(72, 72));
            cardName = Label("n", front, "", 26, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0.5f, 1), new Vector2(0, -116), new Vector2(280, 34));
            cardTeam = Label("tm", front, "", 13, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0.5f, 1), new Vector2(0, -140), new Vector2(280, 20));
            cardDesc = Label("d", front, "", 13, FontStyle.Normal, TextAnchor.UpperCenter, new Vector2(0.5f, 1), new Vector2(0, -196), new Vector2(260, 84)); cardDesc.color = new Color(0.8f, 0.83f, 0.94f);
            var cb = Panel("tamam", front, new Color(0.96f, 0.77f, 0.26f), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(160, 38), true);
            cardBtnT = Label("t", cb, "Hazırım", 15, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(160, 38)); cardBtnT.color = new Color(0.1f, 0.08f, 0.04f);
            cardBtn = cb.gameObject.AddComponent<Button>();
            card.gameObject.SetActive(false);

            // bitiş ekranı
            var end = Panel("bitiş", canvas, new Color(0.02f, 0.03f, 0.08f, 0.9f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            endG = end.gameObject.AddComponent<CanvasGroup>();
            endTitle = Label("t", end, "", 32, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(760, 44));
            endSub = Label("s", end, "", 15, FontStyle.Normal, TextAnchor.MiddleCenter, new Vector2(0.5f, 1), new Vector2(0, -72), new Vector2(760, 22));
            endGrid = Rect("ızgara", end, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(720, 200));
            var gl = endGrid.gameObject.AddComponent<GridLayoutGroup>(); gl.cellSize = new Vector2(134, 92); gl.spacing = new Vector2(8, 8); gl.childAlignment = TextAnchor.MiddleCenter;
            var again = Panel("tekrar", end, new Color(0.96f, 0.77f, 0.26f), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-90, 36), new Vector2(160, 40), true);
            Label("t", again, "Tekrar oyna", 15, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(160, 40)).color = new Color(0.1f, 0.08f, 0.04f);
            Btn(again, () => onAgain?.Invoke());
            var quit = Panel("çık", end, BG, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(90, 36), new Vector2(160, 40), true);
            Label("t", quit, "Çık", 15, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(160, 40));
            Btn(quit, () => onBack?.Invoke());
            end.gameObject.SetActive(false);
            Eyes(false);
        }

        // ---------------------------------------------------------------- yapı taşları
        static Sprite MakeRound()
        {
            int s = 32; var t = new Texture2D(s, s, TextureFormat.RGBA32, false);
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            {
                float dx = Mathf.Max(0, Mathf.Abs(x - 15.5f) - 6), dy = Mathf.Max(0, Mathf.Abs(y - 15.5f) - 6);
                t.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(10f - Mathf.Sqrt(dx * dx + dy * dy))));
            }
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(12, 12, 12, 12));
        }
        static RectTransform Rect(string n, Transform p, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size)
        {
            var g = new GameObject(n, typeof(RectTransform)); var r = g.GetComponent<RectTransform>(); r.SetParent(p, false);
            r.anchorMin = aMin; r.anchorMax = aMax; r.anchoredPosition = pos; r.sizeDelta = size; return r;
        }
        RectTransform Panel(string n, Transform p, Color c, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size, bool rounded = false)
        {
            var r = Rect(n, p, aMin, aMax, pos, size); var im = r.gameObject.AddComponent<Image>(); im.color = c;
            if (rounded) { im.sprite = round; im.type = Image.Type.Sliced; }
            return r;
        }
        Image Img(string n, Transform p, Sprite s, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var r = Rect(n, p, anchor, anchor, pos, size); var im = r.gameObject.AddComponent<Image>(); im.sprite = s; im.preserveAspect = true; im.enabled = s != null; im.raycastTarget = false; return im;
        }
        Text Label(string n, Transform p, string text, int size, FontStyle st, TextAnchor al, Vector2 anchor, Vector2 pos, Vector2 box)
        {
            var r = Rect(n, p, anchor, anchor, pos, box); var t = r.gameObject.AddComponent<Text>();
            t.font = font; t.fontSize = size; t.fontStyle = st; t.alignment = al; t.text = text; t.color = Color.white; t.supportRichText = true; t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
            var sh = r.gameObject.AddComponent<Shadow>(); sh.effectColor = new Color(0, 0, 0, 0.55f); sh.effectDistance = new Vector2(1, -1);
            return t;
        }
        static void Btn(RectTransform r, Action f) { var b = r.gameObject.GetComponent<Button>(); if (b == null) b = r.gameObject.AddComponent<Button>(); b.onClick.AddListener(() => f()); }

        void Send() { var t = input.text.Trim(); input.text = ""; if (t.Length > 0) onSend?.Invoke(t); }

        // ---------------------------------------------------------------- oyunun kullandıkları
        float phaseEnd; public bool timerOn;
        public void Phase(string t, string s, float seconds = 0) { phT.text = t; phS.text = s ?? ""; SetTimer(seconds); }
        public void Sub(string s) { phS.text = s; }
        public string PhaseTitle => phT.text; public string PhaseSub => phS.text;
        public void SetTimer(float seconds) { timerOn = seconds > 0; timer.transform.parent.gameObject.SetActive(timerOn); phaseEnd = Time.time + seconds; }
        public void SetTimerEnd(float end) { timerOn = end > 0; timer.transform.parent.gameObject.SetActive(timerOn); phaseEnd = end; }

        public class Btn_ { public string t; public string style = "on"; public bool dis; public Action f; }
        public void ShowAction(string icon, string title, string desc, params Btn_[] btns)
        {
            actG.gameObject.SetActive(true); actI.sprite = Emoji(icon); actI.enabled = actI.sprite != null; actT.text = title; actD.text = desc;
            foreach (Transform c in actBtns) Destroy(c.gameObject);
            foreach (var b in btns)
            {
                Color col = b.style == "on" ? new Color(0.96f, 0.77f, 0.26f) : b.style == "r" ? new Color(0.85f, 0.25f, 0.3f) : b.style == "g" ? new Color(0.2f, 0.7f, 0.45f) : b.style == "p" ? new Color(0.55f, 0.4f, 0.9f) : new Color(1, 1, 1, 0.12f);
                if (b.dis) col.a *= 0.35f;
                var r = Panel("b", actBtns, col, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(80, 34), true);
                var t = Label("t", r, b.t, 13, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(140, 34));
                t.color = b.style == "on" ? new Color(0.1f, 0.08f, 0.04f) : Color.white;
                var bb = b; if (!b.dis && b.f != null) Btn(r, () => bb.f());
            }
        }
        public void HideAction() { actG.gameObject.SetActive(false); }

        public async Task Banner(string icon, string t1, string t2, float sec)
        {
            bIcon.sprite = Emoji(icon); bIcon.enabled = bIcon.sprite != null; b1.text = t1; b2.text = t2 ?? "";
            for (float a = 0; a < 1; a += Time.deltaTime * 5) { bannerG.alpha = a; await Awaitable.NextFrameAsync(); }
            bannerG.alpha = 1;
            await Game.Wait(sec);
            for (float a = 1; a > 0; a -= Time.deltaTime * 4) { bannerG.alpha = a; await Awaitable.NextFrameAsync(); }
            bannerG.alpha = 0;
        }

        float toastUntil;
        public void Toast(string t, float sec = 2.4f) { toastT.text = t; toastG.alpha = 1; toastUntil = Time.time + sec; }

        public void Log(string name, string text, string cls = "")
        {
            var row = Panel("satır", logBox, new Color(0.05f, 0.06f, 0.13f, cls == "sys" ? 0.75f : 0.55f), Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(320, 24), true);
            var col = cls == "wolf" ? "#FF8FA3" : cls == "sys" ? "#F5C542" : "#B9A8FF";
            var t = Label("t", row, (string.IsNullOrEmpty(name) ? "" : $"<color={col}><b>{Esc(name)}</b></color> ") + Esc(text), 12, FontStyle.Normal, TextAnchor.MiddleLeft, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(306, 22));
            if (cls == "sys") t.color = new Color(1f, 0.92f, 0.7f);
            var le = row.gameObject.AddComponent<LayoutElement>(); le.preferredWidth = 320; le.minHeight = 24;
            while (logBox.childCount > 6) DestroyImmediate(logBox.GetChild(0).gameObject);
        }
        static string Esc(string s) => s.Replace("<", "‹").Replace(">", "›");

        public void Eyes(bool closed, string icon = "moon", string text = "", string small = "")
        {
            eyesClosed = closed; sleepI.sprite = Emoji(icon); sleepI.enabled = closed && sleepI.sprite != null; sleepT.text = closed ? text : ""; sleepS.text = closed ? small : "";
        }
        bool eyesClosed; float lid;

        public void RoleChip(string icon, string name, Color c) { rcIcon.sprite = Emoji(icon); rcIcon.enabled = true; rcName.text = name; rcBorder.color = new Color(c.r * 0.35f, c.g * 0.35f, c.b * 0.35f, 0.9f); }

        public void ShowCard(string icon, string name, string team, Color c, string desc, string btn, Action onClose)
        {
            cardIcon.sprite = Emoji(icon); cardName.text = name; cardTeam.text = team; cardTeam.color = c; cardDesc.text = desc; cardBtnT.text = btn;
            cardBtn.onClick.RemoveAllListeners(); cardBtn.onClick.AddListener(() => { cardG.gameObject.SetActive(false); onClose?.Invoke(); });
            cardG.gameObject.SetActive(true);
        }
        public void HideCard() { cardG.gameObject.SetActive(false); }

        public void InputState(bool enabled, string placeholder)
        {
            input.interactable = enabled; ((Text)input.placeholder).text = placeholder;
        }

        public class EndEntry { public string icon, name, role, extra; public Color color; public bool alive, win; }
        public void ShowEnd(string title, string sub, List<EndEntry> list)
        {
            endTitle.text = title; endSub.text = sub;
            foreach (Transform c in endGrid) Destroy(c.gameObject);
            foreach (var e in list)
            {
                var r = Panel("k", endGrid, e.win ? new Color(0.35f, 0.28f, 0.08f, 0.9f) : new Color(1, 1, 1, 0.07f), Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(134, 92), true);
                Img("i", r, Emoji(e.icon), new Vector2(0.5f, 1), new Vector2(0, -20), new Vector2(26, 26));
                Label("n", r, e.name, 13, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0.5f, 1), new Vector2(0, -44), new Vector2(130, 18));
                var rl = Label("r", r, e.role + e.extra, 12, FontStyle.Normal, TextAnchor.MiddleCenter, new Vector2(0.5f, 1), new Vector2(0, -61), new Vector2(130, 16)); rl.color = e.color;
                var st = Label("s", r, e.alive ? "Hayatta" : "Öldü", 11, FontStyle.Normal, TextAnchor.MiddleCenter, new Vector2(0.5f, 1), new Vector2(0, -77), new Vector2(130, 14)); st.color = new Color(0.6f, 0.64f, 0.78f);
                if (!e.alive) r.GetComponent<Image>().color = new Color(1, 1, 1, 0.04f);
            }
            endG.gameObject.SetActive(true);
        }

        // ---------------------------------------------------------------- isim etiketleri
        public class Tag
        {
            public RectTransform r; public Text name, bubble, badge, seer; public RectTransform bubbleBox, badgeBox, seerBox; public Image emo; public List<Image> icons = new List<Image>();
            public float bubbleUntil, emoUntil, seerUntil; public string iconKey = "";
        }
        public Tag MakeTag(string n)
        {
            var tg = new Tag();
            tg.r = Rect("etiket", tagsRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(10, 10));
            tg.name = Label("n", tg.r, n, 15, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(160, 22));
            for (int i = 0; i < 3; i++) tg.icons.Add(Img("ic" + i, tg.r, null, new Vector2(0.5f, 0.5f), new Vector2(-60 - i * 20, 0), new Vector2(18, 18)));
            tg.badgeBox = Panel("rozet", tg.r, new Color(0.96f, 0.77f, 0.26f, 0.95f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(64, 0), new Vector2(42, 22), true);
            tg.badge = Label("t", tg.badgeBox, "", 13, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(42, 22)); tg.badge.color = new Color(0.12f, 0.08f, 0.02f);
            tg.badgeBox.gameObject.SetActive(false);
            tg.bubbleBox = Panel("balon", tg.r, new Color(1, 1, 1, 0.95f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 34), new Vector2(190, 40), true);
            tg.bubble = Label("t", tg.bubbleBox, "", 12, FontStyle.Normal, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(178, 36)); tg.bubble.color = new Color(0.1f, 0.1f, 0.16f);
            tg.bubble.GetComponent<Shadow>().enabled = false;
            tg.bubbleBox.gameObject.SetActive(false);
            tg.emo = Img("emo", tg.r, null, new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(34, 34));
            tg.seerBox = Panel("kâhin", tg.r, new Color(0.18f, 0.12f, 0.35f, 0.95f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -26), new Vector2(170, 26), true);
            tg.seer = Label("t", tg.seerBox, "", 12, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(166, 24));
            tg.seerBox.gameObject.SetActive(false);
            return tg;
        }
        public void Bubble(Tag t, string text) { t.bubble.text = text; t.bubbleBox.gameObject.SetActive(true); t.bubbleUntil = Time.time + 3.8f; }
        public void Emote(Tag t, string key) { t.emo.sprite = Emoji(key); t.emo.enabled = true; t.emoUntil = Time.time + 2.4f; }
        public void Seer(Tag t, string text, float sec) { t.seer.text = text; t.seerBox.gameObject.SetActive(true); t.seerUntil = Time.time + sec; }

        public void PlaceTag(Tag t, Vector3 world, Camera cam, bool dead, bool selected, string[] iconKeys, string badge, bool wolfBadge)
        {
            var sp = cam.WorldToScreenPoint(world);
            bool vis = sp.z > 0 && sp.x > -Screen.width * 0.1f && sp.x < Screen.width * 1.1f;
            t.r.gameObject.SetActive(vis); if (!vis) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(tagsRoot, sp, null, out var lp);
            t.r.anchoredPosition = lp;
            t.name.color = dead ? new Color(1, 1, 1, 0.45f) : selected ? new Color(0.96f, 0.77f, 0.26f) : Color.white;
            for (int i = 0; i < t.icons.Count; i++) { var k = i < iconKeys.Length ? iconKeys[i] : null; var s = Emoji(k); if (t.icons[i].sprite != s) t.icons[i].sprite = s; t.icons[i].enabled = s != null; }
            bool hasB = !string.IsNullOrEmpty(badge); if (t.badgeBox.gameObject.activeSelf != hasB) t.badgeBox.gameObject.SetActive(hasB);
            if (hasB) { t.badge.text = badge; t.badgeBox.GetComponent<Image>().color = wolfBadge ? new Color(1f, 0.3f, 0.43f, 0.95f) : new Color(0.96f, 0.77f, 0.26f, 0.95f); }
            if (t.bubbleBox.gameObject.activeSelf && Time.time > t.bubbleUntil) t.bubbleBox.gameObject.SetActive(false);
            if (t.emo.enabled && Time.time > t.emoUntil) t.emo.enabled = false;
            if (t.seerBox.gameObject.activeSelf && Time.time > t.seerUntil) t.seerBox.gameObject.SetActive(false);
        }

        public void HideHint() { if (hint != null) hint.enabled = false; }

        void Update()
        {
            if (timerOn)
            {
                float left = Mathf.Max(0, phaseEnd - Time.time) * Game.SPEED; int s = Mathf.CeilToInt(left);
                timer.text = (s / 60) + ":" + (s % 60).ToString("00"); timer.color = left < 10 ? new Color(1f, 0.4f, 0.45f) : new Color(0.96f, 0.77f, 0.26f);
            }
            if (toastG.alpha > 0 && Time.time > toastUntil) toastG.alpha = Mathf.Max(0, toastG.alpha - Time.deltaTime * 3);
            lid = Mathf.MoveTowards(lid, eyesClosed ? 1 : 0, Time.deltaTime * 2.2f);
            float h = canvas.rect.height * 0.58f * lid;
            lidTop.sizeDelta = new Vector2(0, h); lidBot.sizeDelta = new Vector2(0, h);
            lidTop.gameObject.SetActive(lid > 0.001f); lidBot.gameObject.SetActive(lid > 0.001f);
            if (hint != null && hint.enabled && Time.time > 9) hint.enabled = false;
        }
    }
}
