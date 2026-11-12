// Arayüz: kodla kurulan uGUI. Emojiler Twemoji görselleri (Resources/Emoji) olarak gösterilir.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VK
{
    public class Hud : MonoBehaviour
    {
        public static Hud I;
        Font font, fontBold, fontTitle; Sprite round, grad, glow;
        RectTransform canvas;
        Text phT, phS, timer, rcName, toastT, b1, b2, sleepT, sleepS, hint;
        Image rcIcon, rcBorder, sleepI, bIcon;
        CanvasGroup toastG, bannerG, actG, cardG, endG, emoG;
        Image cardArt; Action cardCloseAct; Image[] cardSmoke; Image cardPropL, cardPropR, cardRibbon, cardDivIcon; RectTransform cardTop; Button cardClose;
        RectTransform lidTop, lidBot, logBox, actBtns, tagsRoot, endGrid, emoRow;
        Text actT, actD; Image actI;
        InputField input; Image cardIcon; Text cardName, cardTeam, cardDesc; Button cardBtn; Text cardBtnT;
        Text endTitle, endSub;
        public Action<string> onSend; public Action<string> onEmote; public Action onBack, onRoleChip, onAgain, onLobby; public Func<List<string>> voteLog;
        public static readonly Color BG = new Color(0.075f, 0.078f, 0.11f, 0.8f);
        Image phIcon; RectTransform phPill;
        RectTransform chatBtn, inBox, sendBtn, spkBtn, micBtn, emoBtn; Image spkIcon, micSlash; bool chatOpen, soundOn = true, micOn;
        // yazma alanı sohbet düğmesinin yanında açılır/kapanır; diğer düğmeler yana kayar
        void OpenChat(bool open, bool instant = false)
        {
            chatOpen = open;
            inBox.gameObject.SetActive(open); sendBtn.gameObject.SetActive(open);
            float x0 = open ? 352 : 76;
            var targets = new[] { (spkBtn, x0), (micBtn, x0 + 46), (emoBtn, x0 + 92) };
            foreach (var (r, x) in targets) { if (instant) r.anchoredPosition = new Vector2(x, 28); else Tween.Move(r, r.anchoredPosition, new Vector2(x, 28), 0.22f); }
            chatBtn.GetComponent<Image>().color = open ? new Color(0.36f, 0.24f, 0.7f, 0.9f) : BG;
            if (open && !instant) { Scale(inBox, new Vector3(0.6f, 1, 1), Vector3.one, 0.25f); input.ActivateInputField(); }
            if (emoRow != null) emoRow.gameObject.SetActive(false);
        }
        public static readonly Color GOLD = new Color(1f, 0.8f, 0.3f);
        Image cardGlow;

        public static Sprite Emoji(string k) => string.IsNullOrEmpty(k) ? null : Resources.Load<Sprite>("Emoji/" + k);

        public void Build()
        {
            I = this;
            UI.Init();
            font = UI.Body; fontBold = UI.Bold; fontTitle = UI.Title;
            round = UI.Round; grad = MakeGrad(); glow = MakeGlow();
            var cgo = new GameObject("Arayüz", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var cv = cgo.GetComponent<Canvas>(); cv.renderMode = RenderMode.ScreenSpaceOverlay; cv.sortingOrder = 10;
            var sc = cgo.GetComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(844, 390); sc.matchWidthOrHeight = 0.6f;
            canvas = cgo.GetComponent<RectTransform>();
            if (FindFirstObjectByType<EventSystem>() == null) new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            var topShade = Rect("üstGölge", canvas, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -55), new Vector2(0, 110));
            var ti = topShade.gameObject.AddComponent<Image>(); ti.sprite = grad; ti.color = new Color(0.02f, 0.02f, 0.06f, 0.55f); ti.raycastTarget = false; topShade.localScale = new Vector3(1, -1, 1);
            // alt kenar: tüm genişlikte, çok yumuşak geçişli hafif karartma (sert kenar çizgisi kalmaz)
            var botShade = Rect("altGölge", canvas, new Vector2(0, 0), new Vector2(1, 0), Vector2.zero, new Vector2(0, 120)); botShade.pivot = new Vector2(0.5f, 0);
            var bi = botShade.gameObject.AddComponent<Image>(); bi.sprite = grad; bi.color = new Color(0.02f, 0.02f, 0.06f, 0.32f); bi.raycastTarget = false;
            tagsRoot = Rect("etiketler", canvas, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            // göz kapakları (gece)
            lidTop = Panel("lidT", canvas, new Color(0.01f, 0.01f, 0.025f, 1f), new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -0), new Vector2(0, 0)); lidTop.pivot = new Vector2(0.5f, 1);
            lidBot = Panel("lidB", canvas, new Color(0.01f, 0.01f, 0.025f, 1f), new Vector2(0, 0), new Vector2(1, 0), Vector2.zero, Vector2.zero); lidBot.pivot = new Vector2(0.5f, 0);
            sleepI = Img("si", lidBot, null, new Vector2(0.5f, 1), new Vector2(0, 70), new Vector2(46, 46));
            sleepT = Label("st", lidBot, "", 24, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0.5f, 1), new Vector2(0, 30), new Vector2(600, 30));
            sleepT.font = UI.Display;
            sleepS = Label("ss", lidBot, "", 13, FontStyle.Normal, TextAnchor.MiddleCenter, new Vector2(0.5f, 1), new Vector2(0, 8), new Vector2(600, 20)); sleepS.color = new Color(0.6f, 0.64f, 0.8f);

            // üst çubuk
            var back = Panel("geri", canvas, BG, new Vector2(0, 1), new Vector2(0, 1), new Vector2(34, -30), new Vector2(40, 40), true);
            Label("<", back, "<", 24, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 2), new Vector2(40, 40));
            Btn(back, () => onBack?.Invoke());
            var chip = Panel("rol", canvas, BG, new Vector2(0, 1), new Vector2(0, 1), new Vector2(124, -30), new Vector2(118, 40), true);
            rcBorder = chip.GetComponent<Image>();
            rcIcon = Img("ri", chip, null, new Vector2(0, 0.5f), new Vector2(22, 0), new Vector2(24, 24));
            rcName = Label("rn", chip, "", 14, FontStyle.Bold, TextAnchor.MiddleLeft, new Vector2(0, 0.5f), new Vector2(76, 0), new Vector2(80, 30));
            Btn(chip, () => onRoleChip?.Invoke());
            phPill = Panel("evre", canvas, new Color(0.055f, 0.058f, 0.08f, 0.72f), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -31), new Vector2(330, 46), true);
            phPill.GetComponent<Image>().sprite = UI.Pill;
            phIcon = Img("gündüz-gece", phPill, Emoji("sun"), new Vector2(0, 0.5f), new Vector2(26, 0), new Vector2(26, 26));
            phT = Label("ph", phPill, "VAMPİR KÖYLÜ", 19, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0.5f, 1), new Vector2(10, -15), new Vector2(280, 26)); phT.font = UI.Display; phT.fontSize = 22; phT.GetComponent<Shadow>().effectDistance = new Vector2(0, -1.5f);
            phS = Label("phs", phPill, "", 11, FontStyle.Normal, TextAnchor.MiddleCenter, new Vector2(0.5f, 1), new Vector2(10, -34), new Vector2(290, 16)); phS.color = new Color(0.78f, 0.79f, 0.86f);
            var tm = Panel("süre", canvas, BG, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-46, -30), new Vector2(70, 36), true);
            timer = Label("t", tm, "", 20, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 1), new Vector2(70, 36)); timer.font = fontTitle; timer.color = GOLD;
            tm.gameObject.SetActive(false);
            var hist = Panel("geçmiş", canvas, BG, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-104, -30), new Vector2(36, 36), true);
            Img("i", hist, Emoji("scroll"), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(20, 20));
            Btn(hist, ShowVoteLog);

            // sohbet günlüğü
            logBox = Rect("log", canvas, new Vector2(0, 0), new Vector2(0, 0), new Vector2(12, 56), new Vector2(250, 110)); logBox.pivot = Vector2.zero;
            var vl = logBox.gameObject.AddComponent<VerticalLayoutGroup>(); vl.childAlignment = TextAnchor.LowerLeft; vl.spacing = 3; vl.childControlHeight = true; vl.childControlWidth = true; vl.childForceExpandHeight = false; vl.childForceExpandWidth = false;
            // alt çubuk: kare sohbet düğmesi (basınca yazma alanı yanına açılır), hoparlör, mikrofon, emoji
            chatBtn = Panel("sohbet", canvas, BG, Vector2.zero, Vector2.zero, new Vector2(30, 28), new Vector2(40, 40), true);
            Img("i", chatBtn, Emoji("chat"), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(24, 24));
            Btn(chatBtn, () => OpenChat(!chatOpen));
            inBox = Panel("yaz", canvas, BG, Vector2.zero, Vector2.zero, new Vector2(170, 28), new Vector2(224, 40), true);
            var it = Label("metin", inBox, "", 14, FontStyle.Normal, TextAnchor.MiddleLeft, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200, 34));
            var ph = Label("ipucu", inBox, "Bir şey yaz...", 14, FontStyle.Normal, TextAnchor.MiddleLeft, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200, 34)); ph.color = new Color(1, 1, 1, 0.45f);
            input = inBox.gameObject.AddComponent<InputField>(); input.textComponent = it; input.placeholder = ph; input.characterLimit = 120;
            input.onEndEdit.AddListener(s => { if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) Send(); });
            sendBtn = Panel("gönder", canvas, BG, Vector2.zero, Vector2.zero, new Vector2(306, 28), new Vector2(40, 40), true);
            Label(">", sendBtn, ">", 20, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40, 40));
            Btn(sendBtn, Send);
            spkBtn = Panel("hoparlör", canvas, BG, Vector2.zero, Vector2.zero, new Vector2(76, 28), new Vector2(40, 40), true);
            spkIcon = Img("i", spkBtn, Emoji("speakeron"), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(24, 24));
            Btn(spkBtn, () => { soundOn = !soundOn; AudioListener.volume = soundOn ? 1 : 0; spkIcon.sprite = Emoji(soundOn ? "speakeron" : "mute"); Toast(soundOn ? "Ses açık" : "Ses kapalı", 1.4f); });
            micBtn = Panel("mikrofon", canvas, BG, Vector2.zero, Vector2.zero, new Vector2(122, 28), new Vector2(40, 40), true);
            micSlash = Img("i", micBtn, Emoji("micoff"), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(24, 24));
            Btn(micBtn, () => { micOn = !micOn; micSlash.sprite = Emoji(micOn ? "mic" : "micoff"); micBtn.GetComponent<Image>().color = micOn ? new Color(0.16f, 0.5f, 0.36f, 0.85f) : BG; Bridge.Send("{\"type\":\"mic\",\"on\":" + (micOn ? "true" : "false") + "}"); Toast(micOn ? "Mikrofon açık" : "Mikrofon kapalı", 1.4f); });
            emoBtn = Panel("emo", canvas, BG, Vector2.zero, Vector2.zero, new Vector2(168, 28), new Vector2(40, 40), true);
            Img("e", emoBtn, Emoji("laugh"), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(24, 24));
            Btn(emoBtn, () => { emoG.gameObject.SetActive(!emoG.gameObject.activeSelf); emoRow.anchoredPosition = new Vector2(emoBtn.anchoredPosition.x + 136, 72); });
            emoRow = Panel("emojiler", canvas, BG, Vector2.zero, Vector2.zero, new Vector2(170, 72), new Vector2(310, 40), true);
            emoG = emoRow.gameObject.AddComponent<CanvasGroup>();
            string[] emos = { "wave", "up", "down", "laugh", "angry", "think", "scream", "cry", "heart" };
            for (int i = 0; i < emos.Length; i++) { string k = emos[i]; var b = Img("e" + i, emoRow, Emoji(k), new Vector2(0, 0.5f), new Vector2(20 + i * 34, 0), new Vector2(26, 26)); Btn(b.rectTransform, () => { onEmote?.Invoke(k); emoRow.gameObject.SetActive(false); }); }
            emoRow.gameObject.SetActive(false);
            OpenChat(false, true);

            // eylem kartı
            var act = Panel("eylem", canvas, BG, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-136, 56), new Vector2(256, 96), true);
            actG = act.gameObject.AddComponent<CanvasGroup>();
            actI = Img("i", act, null, new Vector2(0, 1), new Vector2(28, -28), new Vector2(30, 30));
            actT = Label("t", act, "", 15, FontStyle.Bold, TextAnchor.MiddleLeft, new Vector2(0, 1), new Vector2(146, -18), new Vector2(206, 22));
            actD = Label("d", act, "", 11, FontStyle.Normal, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(146, -44), new Vector2(206, 32)); actD.color = new Color(0.75f, 0.78f, 0.9f);
            actBtns = Rect("düğmeler", act, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 22), new Vector2(-18, 32));
            var hl = actBtns.gameObject.AddComponent<HorizontalLayoutGroup>(); hl.spacing = 8; hl.childForceExpandWidth = true; hl.childControlWidth = true; hl.childControlHeight = true;
            act.gameObject.SetActive(false);

            // toast, banner, ipucu
            var toast = Panel("toast", canvas, BG, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -84), new Vector2(460, 34), true);
            toastG = toast.gameObject.AddComponent<CanvasGroup>(); toastG.alpha = 0;
            toastT = Label("t", toast, "", 14, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(450, 30));
            var ban = Rect("banner", canvas, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(760, 130));
            bannerG = ban.gameObject.AddComponent<CanvasGroup>(); bannerG.alpha = 0; bannerG.blocksRaycasts = false;
            bIcon = Img("i", ban, null, new Vector2(0.5f, 1), new Vector2(0, -24), new Vector2(46, 46));
            var bGlow = ban.gameObject; var gl0 = Img("parıltı", ban, glow, new Vector2(0.5f, 0.5f), new Vector2(0, -10), new Vector2(620, 200)); gl0.enabled = true; gl0.color = new Color(0, 0, 0, 0.55f); gl0.preserveAspect = false; gl0.transform.SetAsFirstSibling();
            b1 = Label("b1", ban, "", 46, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, -6), new Vector2(760, 56)); b1.font = UI.Display;
            b2 = Label("b2", ban, "", 15, FontStyle.Normal, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, -40), new Vector2(700, 24));
            hint = Label("hint", canvas, "← Kaydırarak bak · Seçmek için kişiye dokun →", 13, FontStyle.Normal, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, -20), new Vector2(500, 26));

            // rol kartı: altın süslü mor çerçeve, üstte ay diski önünde büyük rol ikonu, iki yanında rol eşyaları,
            // isim plakası, takım şeridi, açıklama, ayraç, parlak altın düğme, köşede kapat
            var card = Panel("kartArka", canvas, new Color(0.02f, 0.01f, 0.05f, 0.62f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            cardG = card.gameObject.AddComponent<CanvasGroup>();
            var front = Rect("kart", card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -24), new Vector2(360, 284));
            Sprite kUS(string n) => Resources.Load<Sprite>("UI/" + n);
            Image kSl(string n, Transform par, string sp, Vector2 anchor, Vector2 pos, Vector2 size, float ppu = 1f)
            {
                var im = Img(n, par, kUS(sp), anchor, pos, size); im.preserveAspect = false; if (im.sprite != null && im.sprite.border != Vector4.zero) { im.type = Image.Type.Sliced; im.pixelsPerUnitMultiplier = ppu; }
                return im;
            }
            cardSmoke = new Image[3];
            cardSmoke[0] = kSl("duman sol", front, "smoke", new Vector2(0, 0), new Vector2(10, 30), new Vector2(170, 150));
            cardSmoke[1] = kSl("duman sağ", front, "smoke", new Vector2(1, 0), new Vector2(-10, 30), new Vector2(170, 150));
            cardSmoke[2] = kSl("duman üst", front, "smoke", new Vector2(0.5f, 1), new Vector2(0, 0), new Vector2(330, 170));
            var kFrame = kSl("çerçeve", front, "frame", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(360, 284), 1.6f);
            var kFsh = kFrame.gameObject.AddComponent<Shadow>(); kFsh.effectColor = new Color(0, 0, 0, 0.5f); kFsh.effectDistance = new Vector2(0, -5);
            cardTop = Rect("üst süs", front, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var kMoon = kSl("ay", cardTop, "moon", new Vector2(0.5f, 1), new Vector2(0, 16), new Vector2(124, 124)); kMoon.color = new Color(1, 1, 1, 0.85f);
            cardGlow = Img("hale", cardTop, glow, new Vector2(0.5f, 1), new Vector2(0, 14), new Vector2(210, 170)); cardGlow.enabled = true; cardGlow.preserveAspect = false;
            cardIcon = Img("i", cardTop, null, new Vector2(0.5f, 1), new Vector2(0, 18), new Vector2(112, 112));
            cardPropL = Img("eşya sol", cardTop, null, new Vector2(0.5f, 1), new Vector2(-134, -70), new Vector2(54, 54)); cardPropL.rectTransform.localEulerAngles = new Vector3(0, 0, 12);
            cardPropR = Img("eşya sağ", cardTop, null, new Vector2(0.5f, 1), new Vector2(134, -70), new Vector2(54, 54)); cardPropR.rectTransform.localEulerAngles = new Vector3(0, 0, -10);
            var kPlq = kSl("plaka", front, "plaque", new Vector2(0.5f, 1), new Vector2(0, -74), new Vector2(214, 56), 1.7f);
            cardName = Label("n", kPlq.rectTransform, "", 32, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 1), new Vector2(200, 44)); cardName.font = UI.Display; cardName.color = new Color(1f, 0.93f, 0.8f);
            var kNsh = cardName.GetComponent<Shadow>(); kNsh.effectColor = new Color(0.25f, 0.1f, 0.02f, 0.9f); kNsh.effectDistance = new Vector2(0, -2);
            cardRibbon = kSl("şerit", front, "ribbon_g", new Vector2(0.5f, 1), new Vector2(0, -107), new Vector2(118, 26), 2f);
            cardTeam = Label("tm", cardRibbon.rectTransform, "", 12, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(118, 22));
            cardDesc = Label("d", front, "", 13, FontStyle.Normal, TextAnchor.UpperCenter, new Vector2(0.5f, 1), new Vector2(0, -158), new Vector2(312, 64)); cardDesc.color = new Color(0.95f, 0.93f, 0.98f);
            kSl("ayraç", front, "divider", new Vector2(0.5f, 0), new Vector2(0, 80), new Vector2(256, 14));
            cardDivIcon = Img("ayraç ikon", front, null, new Vector2(0.5f, 0), new Vector2(0, 80), new Vector2(20, 20));
            var kCb = kSl("tamam", front, "btn_gold", new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(214, 46), 1.9f).rectTransform;
            kCb.GetComponent<Image>().raycastTarget = true;
            cardBtnT = Label("t", kCb, "Hazırım", 19, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 1), new Vector2(200, 40)); cardBtnT.color = new Color(0.24f, 0.13f, 0.03f);
            var kBsh = cardBtnT.GetComponent<Shadow>(); kBsh.effectColor = new Color(1f, 0.95f, 0.7f, 0.6f); kBsh.effectDistance = new Vector2(0, -1);
            cardBtn = kCb.gameObject.AddComponent<Button>();
            Img("taş", front, kUS("gem"), new Vector2(0.5f, 0), new Vector2(0, 2), new Vector2(30, 30));
            var kCx = Img("kapat", front, kUS("close"), new Vector2(1, 1), new Vector2(-4, -4), new Vector2(34, 34)); kCx.raycastTarget = true;
            cardClose = kCx.gameObject.AddComponent<Button>();
            // kullanıcının hazır kart görselleri (Resources/Cards/<rol>.png): düğmeler resmin üstünde görünmez dokunma alanları
            cardArt = Img("kart resmi", front, null, new Vector2(0.5f, 0.5f), new Vector2(0, 24), new Vector2(520, 390)); cardArt.preserveAspect = true;
            void Hit(string n, Vector2 c, Vector2 sz)
            {
                var r = Rect(n, cardArt.rectTransform, c, c, Vector2.zero, new Vector2(520 * sz.x, 390 * sz.y));
                var im = r.gameObject.AddComponent<Image>(); im.color = new Color(0, 0, 0, 0); im.raycastTarget = true;
                r.gameObject.AddComponent<Button>().onClick.AddListener(() => cardCloseAct?.Invoke());
            }
            Hit("kapat düğmesi", new Vector2(0.5f, 0.22f), new Vector2(0.4f, 0.13f));
            Hit("çarpı", new Vector2(0.76f, 0.86f), new Vector2(0.1f, 0.14f));
            cardArt.gameObject.SetActive(false);
            card.gameObject.SetActive(false);

            // bitiş ekranı: BuildEnd (ShowEnd çağrılınca kurulur)
            endG = null;
            Eyes(false);
        }

        // ---------------------------------------------------------------- yapı taşları
        // cam panel: yuvarlak köşe, üstte hafif aydınlık, ince açık kenar çizgisi
        static Sprite MakeRound()
        {
            int s = 64; float r = 18; var t = new Texture2D(s, s, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            {
                float cx = Mathf.Clamp(x + 0.5f, r, s - r), cy = Mathf.Clamp(y + 0.5f, r, s - r);
                float d = Mathf.Sqrt((x + 0.5f - cx) * (x + 0.5f - cx) + (y + 0.5f - cy) * (y + 0.5f - cy));
                float a = Mathf.Clamp01(r - d + 0.5f);
                float edge = Mathf.Clamp01(1.6f - Mathf.Abs(r - 1.2f - d)) * (d > r - 3 ? 1 : 0);
                float v = y / (float)s; float lum = Mathf.Lerp(0.86f, 1.06f, v);
                float c = Mathf.Min(1.3f, lum + edge * 0.35f);
                t.SetPixel(x, y, new Color(c, c, c, a));
            }
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(22, 22, 22, 22));
        }
        static Sprite MakeGrad()
        {
            var t = new Texture2D(4, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < 64; y++) for (int x = 0; x < 4; x++) { float u = y / 63f; t.SetPixel(x, y, new Color(1, 1, 1, Mathf.Pow(1 - u, 1.6f))); }
            t.Apply(); return Sprite.Create(t, new Rect(0, 0, 4, 64), new Vector2(0.5f, 0.5f));
        }
        static Sprite MakeGlow()
        {
            int s = 64; var t = new Texture2D(s, s, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++) { float d = Mathf.Sqrt((x - 31.5f) * (x - 31.5f) + (y - 31.5f) * (y - 31.5f)) / 31.5f; float a = Mathf.Clamp01(1 - d); t.SetPixel(x, y, new Color(1, 1, 1, a * a)); }
            t.Apply(); return Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f));
        }
        static RectTransform Rect(string n, Transform p, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size)
        {
            var g = new GameObject(n, typeof(RectTransform)); var r = g.GetComponent<RectTransform>(); r.SetParent(p, false);
            r.anchorMin = aMin; r.anchorMax = aMax; r.anchoredPosition = pos; r.sizeDelta = size; return r;
        }
        RectTransform Panel(string n, Transform p, Color c, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size, bool rounded = false)
        {
            var r = Rect(n, p, aMin, aMax, pos, size); var im = r.gameObject.AddComponent<Image>(); im.color = c;
            if (rounded)
            {
                im.sprite = round; im.type = Image.Type.Sliced; im.pixelsPerUnitMultiplier = 2.2f; var sh = r.gameObject.AddComponent<Shadow>(); sh.effectColor = new Color(0, 0, 0, 0.35f); sh.effectDistance = new Vector2(0, -2.5f);
                if (c.a > 0.3f) { var o = UI.Img(r, UI.Outline, UI.Stroke, "kenar"); o.rectTransform.Fill(); }
            }
            return r;
        }
        Image Img(string n, Transform p, Sprite s, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var r = Rect(n, p, anchor, anchor, pos, size); var im = r.gameObject.AddComponent<Image>(); im.sprite = s; im.preserveAspect = true; im.enabled = s != null; im.raycastTarget = false; return im;
        }
        Text Label(string n, Transform p, string text, int size, FontStyle st, TextAnchor al, Vector2 anchor, Vector2 pos, Vector2 box)
        {
            var r = Rect(n, p, anchor, anchor, pos, box); var t = r.gameObject.AddComponent<Text>();
            t.font = st == FontStyle.Bold ? fontBold : font; t.fontSize = size; t.fontStyle = FontStyle.Normal; t.alignment = al; t.text = text; t.color = Color.white; t.supportRichText = true; t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
            var sh = r.gameObject.AddComponent<Shadow>(); sh.effectColor = new Color(0, 0, 0, 0.55f); sh.effectDistance = new Vector2(1, -1);
            return t;
        }
        void Btn(RectTransform r, Action f) { var b = r.gameObject.GetComponent<Button>(); if (b == null) b = r.gameObject.AddComponent<Button>(); b.onClick.AddListener(() => { Sfx.Play("tap"); Punch(r, 0.9f); f(); }); }

        void Send() { var t = input.text.Trim(); input.text = ""; if (t.Length > 0) onSend?.Invoke(t); OpenChat(false); }

        // ---------------------------------------------------------------- oyunun kullandıkları
        float phaseEnd; public bool timerOn;
        public void Phase(string t, string s, float seconds = 0)
        {
            bool changed = phT.text != t; phT.text = t; phS.text = s ?? ""; SetTimer(seconds);
            bool night = t.StartsWith("GECE"); var ic = Emoji(t.StartsWith("MUHTAR") ? "crown" : night ? "moon" : t.StartsWith("GÜNDÜZ") || t.StartsWith("SABAH") ? "sun" : "village");
            if (phIcon.sprite != ic) { phIcon.sprite = ic; phIcon.enabled = ic != null; Scale(phIcon.rectTransform, Vector3.zero, Vector3.one, 0.4f); }
            phPill.GetComponent<Image>().color = night ? new Color(0.07f, 0.06f, 0.16f, 0.8f) : new Color(0.055f, 0.058f, 0.08f, 0.72f);
            if (changed) Scale(phPill, new Vector3(0.92f, 0.92f, 1), Vector3.one, 0.3f);
        }
        public void Sub(string s) { phS.text = s; }
        public string PhaseTitle => phT.text; public string PhaseSub => phS.text;
        public void SetTimer(float seconds) { timerOn = seconds > 0; timer.transform.parent.gameObject.SetActive(timerOn); phaseEnd = Time.time + seconds; }
        public void SetTimerEnd(float end) { timerOn = end > 0; timer.transform.parent.gameObject.SetActive(timerOn); phaseEnd = end; }

        public class Btn_ { public string t; public string style = "on"; public bool dis; public Action f; }
        public void ShowAction(string icon, string title, string desc, params Btn_[] btns)
        {
            bool wasOn = actG.gameObject.activeSelf;
            actG.gameObject.SetActive(true); if (!wasOn) { Scale((RectTransform)actG.transform, Vector3.one * 0.86f, Vector3.one, 0.28f); Fade(actG, 0, 1, 0.2f); Sfx.Play("pop"); }
            actI.sprite = Emoji(icon); actI.enabled = actI.sprite != null; actT.text = title; actD.text = desc;
            // düğmesizse ince şerit (üst orta), düğmeliyse kart (sağ alt)
            var ar = (RectTransform)actG.transform; bool compact = btns.Length == 0;
            if (compact)
            {
                ar.anchorMin = ar.anchorMax = new Vector2(0.5f, 1); ar.anchoredPosition = new Vector2(0, -80); ar.sizeDelta = new Vector2(420, 34);
                actI.rectTransform.anchorMin = actI.rectTransform.anchorMax = new Vector2(0, 0.5f); actI.rectTransform.anchoredPosition = new Vector2(22, 0); actI.rectTransform.sizeDelta = new Vector2(24, 24);
                actT.gameObject.SetActive(false);
                actD.rectTransform.anchorMin = actD.rectTransform.anchorMax = new Vector2(0, 0.5f); actD.rectTransform.anchoredPosition = new Vector2(230, 0); actD.rectTransform.sizeDelta = new Vector2(380, 36);
                actD.alignment = TextAnchor.MiddleLeft;
                var first = desc.IndexOf('\n'); if (first > 0) actD.text = desc.Substring(0, first) + "  ·  <size=11>tekrar dokun: geri al</size>";
            }
            else
            {
                ar.anchorMin = ar.anchorMax = new Vector2(1, 0); ar.anchoredPosition = new Vector2(-136, 56); ar.sizeDelta = new Vector2(256, 96);
                actI.rectTransform.anchorMin = actI.rectTransform.anchorMax = new Vector2(0, 1); actI.rectTransform.anchoredPosition = new Vector2(24, -24); actI.rectTransform.sizeDelta = new Vector2(26, 26);
                actT.gameObject.SetActive(true);
                actD.rectTransform.anchorMin = actD.rectTransform.anchorMax = new Vector2(0, 1); actD.rectTransform.anchoredPosition = new Vector2(146, -44); actD.rectTransform.sizeDelta = new Vector2(206, 32);
                actD.alignment = TextAnchor.UpperLeft;
            }
            foreach (Transform c in actBtns) Destroy(c.gameObject);
            foreach (var b in btns)
            {
                Color col = b.style == "on" ? UI.Gold : b.style == "r" ? new Color(0.82f, 0.27f, 0.32f) : b.style == "g" ? new Color(0.16f, 0.66f, 0.47f) : b.style == "p" ? UI.Purple : new Color(1, 1, 1, 0.1f);
                if (b.dis) col.a *= 0.35f;
                var r = Panel("b", actBtns, col, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(80, 30), true); r.GetComponent<Image>().sprite = UI.Pill;
                var t = Label("t", r, b.t, 12, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(140, 34));
                t.color = b.style == "on" ? new Color(0.1f, 0.08f, 0.04f) : Color.white;
                var bb = b; if (!b.dis && b.f != null) Btn(r, () => bb.f());
            }
        }
        public void HideAction() { actG.gameObject.SetActive(false); }

        public async Task Banner(string icon, string t1, string t2, float sec)
        {
            bIcon.sprite = Emoji(icon); bIcon.enabled = bIcon.sprite != null; b1.text = t1; b2.text = t2 ?? "";
            var br = (RectTransform)bannerG.transform; Sfx.Play("whoosh");
            for (float a = 0; a < 1; a += Time.deltaTime * 4.5f) { float e = 1 - Mathf.Pow(1 - a, 3); bannerG.alpha = e; br.localScale = Vector3.one * Mathf.Lerp(1.25f, 1f, e); bIcon.rectTransform.localScale = Vector3.one * EaseBack(Mathf.Clamp01(a * 1.3f)); await Awaitable.NextFrameAsync(); }
            bannerG.alpha = 1; br.localScale = Vector3.one; bIcon.rectTransform.localScale = Vector3.one;
            await Game.Wait(sec);
            for (float a = 1; a > 0; a -= Time.deltaTime * 4) { bannerG.alpha = a; br.localScale = Vector3.one * Mathf.Lerp(0.94f, 1f, a); await Awaitable.NextFrameAsync(); }
            bannerG.alpha = 0; br.localScale = Vector3.one;
        }

        // ölen kişinin rolü: madalyon dönerek açılır, arkasında dönen ışık hüzmeleri, kıvılcımlar, rol adı ve takım şeridi
        public async Task RoleReveal(string icon, string role, Color c, string who, bool vamp)
        {
            var root = Rect("rol açıklama", canvas, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero); root.SetAsLastSibling();
            var g = root.gameObject.AddComponent<CanvasGroup>(); g.blocksRaycasts = false; g.alpha = 0;
            var dim = root.gameObject.AddComponent<Image>(); dim.sprite = UI.Soft; dim.color = new Color(0, 0, 0, 0.45f); dim.raycastTarget = false;
            var mid = Rect("orta", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 22), new Vector2(10, 10));
            var rays = Rect("hüzmeler", mid, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(10, 10));
            for (int i = 0; i < 12; i++)
            {
                var ray = Img("h" + i, rays, UI.Pill, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(18, 250)); ray.preserveAspect = false;
                ray.color = new Color(Mathf.Lerp(c.r, 1, 0.4f), Mathf.Lerp(c.g, 0.9f, 0.4f), Mathf.Lerp(c.b, 0.6f, 0.4f), 0.16f); ray.rectTransform.localEulerAngles = new Vector3(0, 0, i * 15);
            }
            var halo = Img("hale", mid, UI.Glow, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300, 300)); halo.color = new Color(c.r, c.g, c.b, 0.75f); halo.preserveAspect = false;
            var med = Rect("madalyon", mid, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(132, 132));
            var ring = Img("altın", med, UI.Circle, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(132, 132)); ring.color = new Color(0.86f, 0.64f, 0.24f);
            var ring2 = Img("altın iç", med, UI.Circle, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(122, 122)); ring2.color = new Color(1f, 0.86f, 0.5f);
            var disc = Img("zemin", med, UI.Circle, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(112, 112)); disc.color = vamp ? new Color(0.3f, 0.06f, 0.12f) : new Color(0.17f, 0.09f, 0.28f);
            var inner = Img("iç ışık", med, UI.Glow, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(110, 110)); inner.color = new Color(c.r, c.g, c.b, 0.55f); inner.preserveAspect = false;
            var q = Label("?", med, "?", 58, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 2), new Vector2(100, 100)); q.font = UI.Display; q.color = new Color(1f, 0.86f, 0.5f);
            var ic = Img("rol", med, Emoji(icon), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(86, 86)); ic.enabled = false;
            var whoT = Label("kim", mid, (who == "Senin" ? "Senin rolün" : who + " · rolü"), 14, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 92), new Vector2(400, 22)); whoT.color = new Color(0.92f, 0.9f, 1f);
            var nameT = Label("ad", mid, role, 40, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, -92), new Vector2(420, 48)); nameT.font = UI.Display;
            nameT.color = new Color(Mathf.Lerp(c.r, 1, 0.35f), Mathf.Lerp(c.g, 1, 0.35f), Mathf.Lerp(c.b, 1, 0.35f));
            var nsh = nameT.GetComponent<Shadow>(); nsh.effectColor = new Color(0, 0, 0, 0.85f); nsh.effectDistance = new Vector2(0, -3);
            var rib = Img("şerit", mid, Resources.Load<Sprite>(vamp ? "UI/ribbon_r" : "UI/ribbon_g"), new Vector2(0.5f, 0.5f), new Vector2(0, -126), new Vector2(124, 27)); rib.preserveAspect = false;
            if (rib.sprite != null && rib.sprite.border != Vector4.zero) { rib.type = Image.Type.Sliced; rib.pixelsPerUnitMultiplier = 2f; }
            Label("takım", rib.rectTransform, vamp ? "VAMPİRLER" : "KÖY", 12, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(124, 22));
            var sparks = new List<(RectTransform r, Vector2 v, float spin)>();
            var rnd = new System.Random();
            Sfx.Play("whoosh");
            bool flipped = false, burst = false; float T = 0;
            while (T < 3.1f)
            {
                T += Time.deltaTime;
                g.alpha = T < 0.25f ? T / 0.25f : T > 2.7f ? Mathf.Clamp01((3.1f - T) / 0.4f) : 1;
                rays.localEulerAngles = new Vector3(0, 0, T * 22f);
                rays.localScale = Vector3.one * Mathf.Lerp(0.3f, 1.05f, Tween.OutCubic(Mathf.Clamp01((T - 0.45f) / 0.5f)));
                halo.rectTransform.localScale = Vector3.one * (0.85f + Mathf.Sin(T * 5) * 0.06f);
                // madalyon: düşerek gelir, iki tur döner, rol yüzü açılır
                float drop = Mathf.Clamp01(T / 0.35f);
                med.anchoredPosition = new Vector2(0, Mathf.Lerp(60, 0, Tween.OutBack(drop)));
                float spinK = Mathf.Clamp01((T - 0.15f) / 0.75f), ang = spinK * Mathf.PI * 4;
                med.localScale = new Vector3(Mathf.Max(0.03f, Mathf.Abs(Mathf.Cos(ang))), 1, 1) * (T < 0.9f ? 1 : 1 + Mathf.Sin(Mathf.Clamp01((T - 0.9f) / 0.3f) * Mathf.PI) * 0.12f);
                if (!flipped && spinK > 0.88f) { flipped = true; q.enabled = false; ic.enabled = true; Sfx.Play("magic"); }
                if (!burst && T > 0.9f)
                {
                    burst = true; Sfx.Play("pop", 0.9f, 0.8f);
                    for (int i = 0; i < 16; i++)
                    {
                        var s = Img("kıvılcım", mid, Emoji(i % 3 == 0 ? "sparkles" : "sparkle"), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(18, 18));
                        float a = (float)rnd.NextDouble() * Mathf.PI * 2, sp = 120 + (float)rnd.NextDouble() * 160;
                        sparks.Add((s.rectTransform, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * sp, ((float)rnd.NextDouble() - 0.5f) * 400));
                    }
                }
                foreach (var sp in sparks)
                {
                    float life = T - 0.9f; sp.r.anchoredPosition = sp.v * life * (1 - life * 0.35f); sp.r.localEulerAngles = new Vector3(0, 0, sp.spin * life);
                    sp.r.localScale = Vector3.one * Mathf.Clamp01(1.2f - life * 0.75f);
                }
                // yazılar
                float tk = Mathf.Clamp01((T - 0.95f) / 0.35f);
                nameT.color = new Color(nameT.color.r, nameT.color.g, nameT.color.b, tk); nameT.rectTransform.anchoredPosition = new Vector2(0, Mathf.Lerp(-110, -92, Tween.OutCubic(tk)));
                rib.color = new Color(1, 1, 1, Mathf.Clamp01((T - 1.15f) / 0.3f)); rib.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.6f, 1, Tween.OutBack(Mathf.Clamp01((T - 1.15f) / 0.35f)));
                whoT.color = new Color(whoT.color.r, whoT.color.g, whoT.color.b, Mathf.Clamp01((T - 0.2f) / 0.3f));
                await Awaitable.NextFrameAsync();
            }
            Destroy(root.gameObject);
        }

        float toastUntil;
        public void Toast(string t, float sec = 2.4f) { toastT.text = t; toastG.alpha = 1; toastUntil = Time.time + sec; Scale((RectTransform)toastG.transform, new Vector3(0.9f, 0.9f, 1), Vector3.one, 0.25f); Sfx.Play("notify"); }

        public void Log(string name, string text, string cls = "")
        {
            // ince, yarı saydam satırlar; metin uzunluğuna göre genişler
            var row = Rect("satır", logBox, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(300, 20));
            var bgi = row.gameObject.AddComponent<Image>(); bgi.sprite = UI.Pill; bgi.type = Image.Type.Sliced; bgi.pixelsPerUnitMultiplier = 3.2f; bgi.raycastTarget = false;
            bgi.color = cls == "sys" ? new Color(0.3f, 0.22f, 0.05f, 0.5f) : cls == "wolf" ? new Color(0.35f, 0.05f, 0.1f, 0.5f) : new Color(0.04f, 0.04f, 0.07f, 0.42f);
            var col = cls == "wolf" ? "#FF8FA3" : cls == "sys" ? "#F5C542" : "#B9A8FF";
            var t = Label("t", row, (string.IsNullOrEmpty(name) ? "" : $"<color={col}><b>{Esc(name)}</b></color>  ") + Esc(text), 10, FontStyle.Normal, TextAnchor.MiddleLeft, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(226, 15));
            t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Truncate;
            if (cls == "sys") t.color = new Color(1f, 0.92f, 0.7f);
            // genişlik metne göre elle hesaplanır (yerleşim grubu tek kaynak)
            t.rectTransform.anchorMin = Vector2.zero; t.rectTransform.anchorMax = Vector2.one; t.rectTransform.offsetMin = new Vector2(8, 0); t.rectTransform.offsetMax = new Vector2(-8, 0);
            bool two = (name ?? "").Length + text.Length > 40;
            float w = two ? 240 : Mathf.Clamp(t.preferredWidth + 18, 50, 240);
            if (two) t.horizontalOverflow = HorizontalWrapMode.Wrap;
            var le = row.gameObject.AddComponent<LayoutElement>(); le.preferredWidth = w; le.minWidth = w; le.preferredHeight = le.minHeight = two ? 28 : 17;
            logRows.Add((row, Time.time));
            while (logBox.childCount > 4) { var first = logBox.GetChild(0); logRows.RemoveAll(x => x.r == first); DestroyImmediate(first.gameObject); }
            row.gameObject.AddComponent<CanvasGroup>();
            Scale(row, new Vector3(0.94f, 0.94f, 1), Vector3.one, 0.22f);
        }
        static string Esc(string s) => s.Replace("<", "(").Replace(">", ")");

        public void Eyes(bool closed, string icon = "moon", string text = "", string small = "")
        {
            eyesClosed = closed; sleepI.sprite = Emoji(icon); sleepI.enabled = closed && sleepI.sprite != null; sleepT.text = closed ? text : ""; sleepS.text = closed ? small : "";
        }
        bool eyesClosed; float lid;

        public void RoleChip(string icon, string name, Color c) { rcIcon.sprite = Emoji(icon); rcIcon.enabled = rcIcon.sprite != null; rcName.text = name; rcBorder.color = new Color(c.r * 0.35f, c.g * 0.35f, c.b * 0.35f, 0.9f); }

        public void ShowCard(string icon, string name, string team, Color c, string desc, string btn, Action onClose)
        {
            cardIcon.sprite = Emoji(icon); cardIcon.enabled = cardIcon.sprite != null; cardName.text = name; cardTeam.text = team; cardTeam.color = Color.white; cardDesc.text = desc; cardBtnT.text = btn;
            bool vamp = team.StartsWith("VAMP");
            cardRibbon.sprite = Resources.Load<Sprite>(vamp ? "UI/ribbon_r" : "UI/ribbon_g");
            var props = icon switch { "vampir" => ("batw", "poison"), "kahin" => ("crystal", "telescope"), "koruma" => ("shield", "swords"), "cadi" => ("heal", "book"), "avci" => ("rifle", "axes"), "melek" => ("wings", "crown"), _ => ("axes", "lantern") };
            cardPropL.sprite = Emoji(props.Item1); cardPropR.sprite = Emoji(props.Item2); cardPropL.enabled = cardPropL.sprite != null; cardPropR.enabled = cardPropR.sprite != null;
            cardDivIcon.sprite = Emoji(props.Item1); cardDivIcon.enabled = cardDivIcon.sprite != null;
            var smokeC = vamp ? new Color(0.9f, 0.2f, 0.3f, 0.42f) : new Color(Mathf.Lerp(c.r, 0.62f, 0.5f), Mathf.Lerp(c.g, 0.3f, 0.5f), Mathf.Lerp(c.b, 0.95f, 0.5f), 0.4f);
            foreach (var sm in cardSmoke) sm.color = smokeC;
            Action close = () => { Sfx.Play("tap"); cardG.gameObject.SetActive(false); onClose?.Invoke(); };
            cardCloseAct = close;
            var art = Resources.Load<Sprite>("Cards/" + icon);
            cardArt.sprite = art; cardArt.enabled = art != null;
            foreach (Transform ch in cardArt.transform.parent) if (ch != cardArt.transform && ch.name != "arka") ch.gameObject.SetActive(art == null);
            cardArt.gameObject.SetActive(false);
            cardBtn.onClick.RemoveAllListeners(); cardBtn.onClick.AddListener(() => close());
            cardClose.onClick.RemoveAllListeners(); cardClose.onClick.AddListener(() => close());
            cardGlow.color = new Color(c.r, c.g, c.b, 0.55f);
            cardG.gameObject.SetActive(true); Fade(cardG, 0, 1, 0.25f);
            var front = (RectTransform)cardTop.parent;
            // kart arkası: iki kez dönerek karışır, sonra rol yüzü açılır
            var back = front.Find("arka") as RectTransform;
            if (back == null)
            {
                back = UI.R("arka", front).Fill();
                var bf = back.gameObject.AddComponent<Image>(); bf.sprite = Resources.Load<Sprite>("UI/frame"); bf.type = Image.Type.Sliced; bf.pixelsPerUnitMultiplier = 1.6f;
                UI.Img(back, UI.Glow, UI.A(UI.Lilac, 0.5f), "ışık").rectTransform.Fill(40, 40, 40, 40);
                UI.Icon(back, Emoji("crystal"), 70).rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(0, 14), new Vector2(70, 70));
                UI.Txt(back, "Rolün belirleniyor", 13, UI.Lilac, UI.Bold, TextAnchor.MiddleCenter).rectTransform.At(new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(260, 20), new Vector2(0.5f, 0));
            }
            back.gameObject.SetActive(true); back.SetAsLastSibling(); cardTop.gameObject.SetActive(false); Sfx.Play("whoosh");
            bool flipped = false;
            Tween.Run(front, 1.25f, k =>
            {
                if (!front) return;
                float ang = k * Mathf.PI * 3; float sx = Mathf.Abs(Mathf.Cos(ang));
                front.localScale = new Vector3(Mathf.Max(0.02f, sx), 1 + Mathf.Sin(k * Mathf.PI) * 0.06f, 1);
                if (!flipped && k > 0.84f) { flipped = true; back.gameObject.SetActive(false); if (art != null) { cardArt.gameObject.SetActive(true); Scale(cardArt.rectTransform, Vector3.one * 0.9f, Vector3.one, 0.35f); } else { cardTop.gameObject.SetActive(true); Scale(cardIcon.rectTransform, Vector3.zero, Vector3.one, 0.4f); } Sfx.Play("magic"); }
            }, x => x, 0, () => { if (front) front.localScale = Vector3.one; });
        }
        public void HideCard() { cardG.gameObject.SetActive(false); }

        public void InputState(bool enabled, string placeholder)
        {
            input.interactable = enabled; ((Text)input.placeholder).text = placeholder;
        }

        public class EndEntry { public string icon, name, role, extra; public Color color; public bool alive, win, mvp, me; public Look look; }
        // sinematik sonuç ekranı: kazanan takım, MVP, ödüller, istatistikler
        public void ShowEnd(string title, string sub, List<EndEntry> list, Store.MatchResult res, Color team, string stats)
        {
            // oyun arayüzü (üst çubuk, sohbet, isim etiketleri) sonuç ekranının arkasında kalmasın
            for (int i = 0; i < canvas.childCount; i++) canvas.GetChild(i).gameObject.SetActive(false);
            var end = UI.R("bitiş", canvas).Fill(); end.SetAsLastSibling();
            endG = end.gameObject.AddComponent<CanvasGroup>(); Tween.Fade(endG, 0, 1, 0.5f);
            var dim = UI.Img(end, UI.Grad, new Color(0.02f, 0.02f, 0.04f, 0.9f), "alt karartma"); dim.rectTransform.anchorMin = Vector2.zero; dim.rectTransform.anchorMax = new Vector2(1, 0.75f); dim.rectTransform.offsetMin = dim.rectTransform.offsetMax = Vector2.zero;
            var dimT = UI.Img(end, UI.Grad, new Color(0.02f, 0.02f, 0.04f, 0.75f), "üst karartma"); dimT.rectTransform.anchorMin = new Vector2(0, 0.6f); dimT.rectTransform.anchorMax = Vector2.one; dimT.rectTransform.offsetMin = dimT.rectTransform.offsetMax = Vector2.zero; dimT.rectTransform.localScale = new Vector3(1, -1, 1);
            var safe = UI.R("güvenli", end); safe.gameObject.AddComponent<SafeArea>();
            // başlık
            var glowT = UI.Img(safe, UI.Glow, UI.A(team, 0.35f), "ışık"); glowT.rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, 10), new Vector2(560, 150), new Vector2(0.5f, 1));
            var ribbon = UI.Badge(safe, res.win ? "ZAFER" : "YENİLGİ", res.win ? UI.A(UI.Good, 0.25f) : UI.A(UI.Bad, 0.22f), res.win ? UI.Good : UI.Bad, 11);
            ribbon.At(new Vector2(0.5f, 1), new Vector2(0, -12), new Vector2(90, 20), new Vector2(0.5f, 1));
            var tt = UI.Txt(safe, title, 40, team, UI.Display, TextAnchor.MiddleCenter).Shadowed(0.6f); tt.rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, -34), new Vector2(700, 46), new Vector2(0.5f, 1));
            Tween.Scale(tt.rectTransform, 0.6f, 1, 0.55f, 0.1f);
            UI.Txt(safe, sub + "  ·  <color=#9A9CB0>" + stats + "</color>", 12, UI.Fg, null, TextAnchor.MiddleCenter).rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, -80), new Vector2(700, 18), new Vector2(0.5f, 1));
            // MVP kartı
            var mvp = list.Find(e => e.mvp);
            var mc = UI.Panel(safe, "mvp", UI.A(UI.Anthracite, 0.9f)).At(new Vector2(0, 0), new Vector2(18, 60), new Vector2(170, 196), new Vector2(0, 0));
            Tween.Scale(mc, 0.7f, 1, 0.45f, 0.5f);
            if (mvp != null)
            {
                UI.Img(mc, UI.Glow, UI.A(UI.Gold, 0.4f), "ışık").rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, 0), new Vector2(170, 140), new Vector2(0.5f, 1));
                UI.Badge(mc, "MVP", UI.Gold, new Color(0.16f, 0.11f, 0.02f), 11).At(new Vector2(0.5f, 1), new Vector2(0, -8), new Vector2(50, 20), new Vector2(0.5f, 1));
                var av = UI.Avatar(mc, 86, Studio.I != null && mvp.look != null ? Studio.I.Portrait(mvp.look, 160) : null, mvp.look?.frame, out _); ((RectTransform)av.transform.parent.parent).At(new Vector2(0.5f, 1), new Vector2(0, -32), new Vector2(86, 86), new Vector2(0.5f, 1));
                UI.Icon(mc, Emoji("crown"), 26).rectTransform.At(new Vector2(0.5f, 1), new Vector2(30, -26), new Vector2(26, 26), new Vector2(0.5f, 1));
                UI.Txt(mc, mvp.name, 18, null, UI.Display, TextAnchor.MiddleCenter).rectTransform.At(new Vector2(0.5f, 0), new Vector2(0, 52), new Vector2(160, 22), new Vector2(0.5f, 0));
                var rl = UI.R("rol", mc).At(new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(150, 18), new Vector2(0.5f, 0)); UI.HRow(rl, 4, TextAnchor.MiddleCenter);
                UI.Icon(rl, Emoji(mvp.icon), 16).rectTransform.LE(16, 16); var rt0 = UI.Txt(rl, mvp.role, 11, mvp.color, UI.Bold, TextAnchor.MiddleCenter); rt0.rectTransform.LE(-1, 16);
                UI.Txt(mc, "Maçın oyuncusu", 9, UI.Muted, null, TextAnchor.MiddleCenter).rectTransform.At(new Vector2(0.5f, 0), new Vector2(0, 12), new Vector2(150, 14), new Vector2(0.5f, 0));
            }
            // takımlar
            var mid = UI.Panel(safe, "takımlar", UI.A(UI.Ink, 0.78f)).At(new Vector2(0.5f, 0), new Vector2(-10, 60), new Vector2(392, 196), new Vector2(0.5f, 0));
            Tween.Scale(mid, 0.8f, 1, 0.4f, 0.35f);
            var grid = UI.R("ızgara", mid).Fill(10, 10, 10, 10);
            var gl = grid.gameObject.AddComponent<GridLayoutGroup>(); gl.cellSize = new Vector2(120, 54); gl.spacing = new Vector2(6, 6); gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount; gl.constraintCount = 3; gl.childAlignment = TextAnchor.UpperCenter;
            int n = 0;
            foreach (var e in list.OrderByDescending(x => x.win).ThenByDescending(x => x.alive))
            {
                var c = UI.Panel(grid, "oyuncu", e.win ? UI.A(team, 0.16f) : new Color(1, 1, 1, e.alive ? 0.05f : 0.025f), false, UI.RoundSmall);
                if (e.me) UI.Img(c, UI.Outline, UI.Lilac, "sen").rectTransform.Fill(-1, -1, -1, -1);
                var av = UI.Avatar(c, 34, Studio.I != null && e.look != null ? Studio.I.Portrait(e.look, 96) : null, e.look?.frame, out _); ((RectTransform)av.transform.parent.parent).At(new Vector2(0, 0.5f), new Vector2(6, 0), new Vector2(34, 34), new Vector2(0, 0.5f));
                if (!e.alive) av.color = new Color(0.55f, 0.55f, 0.6f, 0.8f);
                UI.Txt(c, e.name, 11, e.alive ? UI.Fg : UI.Muted, UI.Bold).rectTransform.At(new Vector2(0, 1), new Vector2(46, -6), new Vector2(72, 16), new Vector2(0, 1));
                var rr = UI.R("rol", c).At(new Vector2(0, 0), new Vector2(46, 6), new Vector2(74, 16), new Vector2(0, 0)); UI.HRow(rr, 3, TextAnchor.MiddleLeft);
                UI.Icon(rr, Emoji(e.icon), 13).rectTransform.LE(13, 13); UI.Txt(rr, e.role, 9, e.color, UI.Bold).rectTransform.LE(-1, 14);
                if (!e.alive) UI.Icon(c, Emoji("skull"), 12).rectTransform.At(new Vector2(1, 1), new Vector2(-4, -4), new Vector2(12, 12));
                Tween.Scale(c, 0.6f, 1, 0.35f, 0.5f + n++ * 0.06f);
            }
            // ödüller
            var rw = UI.Panel(safe, "ödüller", UI.A(UI.Anthracite, 0.9f)).At(new Vector2(1, 0), new Vector2(-18, 60), new Vector2(200, 196), new Vector2(1, 0));
            Tween.Scale(rw, 0.7f, 1, 0.45f, 0.65f);
            UI.Txt(rw, "Ödüller", 13, null, UI.Bold).rectTransform.At(new Vector2(0, 1), new Vector2(14, -10), new Vector2(170, 18), new Vector2(0, 1));
            var xpT = UI.Txt(rw, "+0 XP", 22, UI.Lilac, UI.Title); xpT.rectTransform.At(new Vector2(0, 1), new Vector2(14, -32), new Vector2(170, 30), new Vector2(0, 1)); Tween.Count(xpT, 0, res.xp, 1.1f, 0.9f, "+{0} XP");
            var bar = UI.ProgressBar(rw, UI.Purple, 7); bar.r.At(new Vector2(0, 1), new Vector2(14, -68), new Vector2(172, 7), new Vector2(0, 1));
            float tk = Store.P.xp / (float)Profile.XpFor(Store.P.level); Tween.Run(bar.r, 1.2f, k => bar.Set(tk * k), Tween.OutCubic, 1.0f);
            UI.Txt(rw, "Seviye " + Store.P.level + (res.levelUps > 0 ? "  <color=#F7C74A>SEVİYE ATLADIN!</color>" : ""), 10, UI.Muted, UI.Bold).rectTransform.At(new Vector2(0, 1), new Vector2(14, -78), new Vector2(180, 16), new Vector2(0, 1));
            var crow = UI.R("altın", rw).At(new Vector2(0, 1), new Vector2(14, -100), new Vector2(170, 26), new Vector2(0, 1)); UI.HRow(crow, 6, TextAnchor.MiddleLeft);
            UI.Icon(crow, Emoji("coin"), 22).rectTransform.LE(22, 22); var cT = UI.Txt(crow, "+0", 18, UI.Gold, UI.Title); cT.rectTransform.LE(120, 26); Tween.Count(cT, 0, res.coins, 1f, 1.3f, "+{0} altın");
            if (res.mvp) UI.Badge(rw, "MVP bonusu dahil", UI.A(UI.Gold, 0.18f), UI.Gold, 9).At(new Vector2(0, 1), new Vector2(14, -128), new Vector2(120, 18), new Vector2(0, 1));
            if (res.ach.Count > 0)
            {
                var a = res.ach[0];
                var ar = UI.Panel(rw, "başarım", UI.A(UI.Gold, 0.12f), false, UI.RoundSmall).At(new Vector2(0.5f, 0), new Vector2(0, 8), new Vector2(180, 36), new Vector2(0.5f, 0));
                UI.Icon(ar, Emoji(a.icon), 20).rectTransform.At(new Vector2(0, 0.5f), new Vector2(8, 0), new Vector2(20, 20), new Vector2(0, 0.5f));
                UI.Txt(ar, "<color=#F7C74A>Başarım:</color> " + a.name + (res.ach.Count > 1 ? $" +{res.ach.Count - 1}" : ""), 10, null, UI.Bold).rectTransform.At(new Vector2(0, 0.5f), new Vector2(34, 0), new Vector2(140, 30), new Vector2(0, 0.5f));
                Tween.Scale(ar, 0.4f, 1, 0.45f, 2.2f); Invoke(nameof(AchSound), 2.2f);
            }
            // düğmeler
            var brow = UI.R("düğmeler", safe).At(new Vector2(0.5f, 0), new Vector2(0, 12), new Vector2(360, 38), new Vector2(0.5f, 0)); UI.HRow(brow, 10, TextAnchor.MiddleCenter, null, true);
            UI.Button(brow, "Lobiye dön", UI.Style.Secondary, () => (onLobby ?? onBack)?.Invoke(), Emoji("village"), 14).r.LE(-1, 38, 1);
            UI.Button(brow, "Tekrar oyna", UI.Style.Primary, () => onAgain?.Invoke(), Emoji("torch"), 14).r.LE(-1, 38, 1);
            Tween.Fade(brow.gameObject.AddComponent<CanvasGroup>(), 0, 1, 0.4f, 1.6f);
        }
        void AchSound() { Sfx.Play("fanfare", 0.8f); }

        // oylama sonucu: çubuklar dolarak sayılır
        public class VoteRow { public string name; public int votes; public Look look; }
        public async Task VoteResults(string title, List<VoteRow> rows, Color col)
        {
            if (rows.Count == 0) return;
            var pan = UI.Panel(canvas, "oy sonucu", new Color(0.055f, 0.058f, 0.08f, 0.9f)).At(new Vector2(0.5f, 1), new Vector2(0, -60), new Vector2(300, 40 + Mathf.Min(5, rows.Count) * 28), new Vector2(0.5f, 1));
            var cg = pan.gameObject.AddComponent<CanvasGroup>(); Tween.Fade(cg, 0, 1, 0.2f); Tween.Scale(pan, 0.85f, 1, 0.3f); Sfx.Play("whoosh", 0.6f);
            UI.Txt(pan, title, 12, UI.Muted, UI.Bold, TextAnchor.MiddleCenter).rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, -8), new Vector2(280, 18), new Vector2(0.5f, 1));
            int max = rows.Max(x => x.votes);
            for (int i = 0; i < Mathf.Min(5, rows.Count); i++)
            {
                var v = rows[i]; float y = -34 - i * 28; bool top = v.votes == max;
                var av = UI.Avatar(pan, 22, Studio.I != null && v.look != null ? Studio.I.Portrait(v.look, 96) : null, v.look?.frame, out _); ((RectTransform)av.transform.parent.parent).At(new Vector2(0, 1), new Vector2(12, y), new Vector2(22, 22), new Vector2(0, 1));
                UI.Txt(pan, v.name, 11, top ? UI.Fg : UI.Muted, UI.Bold).rectTransform.At(new Vector2(0, 1), new Vector2(40, y - 2), new Vector2(80, 18), new Vector2(0, 1));
                var b = UI.ProgressBar(pan, top ? col : new Color(1, 1, 1, 0.3f), 10); b.r.At(new Vector2(0, 1), new Vector2(122, y - 6), new Vector2(130, 10), new Vector2(0, 1));
                float target = v.votes / (float)Mathf.Max(1, max); var bb = b; Tween.Run(b.r, 0.6f, k => bb.Set(target * k), Tween.OutCubic, 0.15f + i * 0.12f);
                var cnt = UI.Txt(pan, "0", 13, top ? col : UI.Muted, UI.Title, TextAnchor.MiddleRight); cnt.rectTransform.At(new Vector2(1, 1), new Vector2(-12, y - 1), new Vector2(30, 20), new Vector2(1, 1)); Tween.Count(cnt, 0, v.votes, 0.6f, 0.15f + i * 0.12f);
            }
            await Game.Wait(2.2f);
            Tween.Fade(cg, 1, 0, 0.25f, 0, () => { if (pan) Destroy(pan.gameObject); });
        }

        // oylama geçmişi
        RectTransform logPanel;
        void ShowVoteLog()
        {
            if (logPanel != null) { Destroy(logPanel.gameObject); logPanel = null; return; }
            var lines = voteLog?.Invoke() ?? new List<string>();
            logPanel = UI.Panel(canvas, "oy geçmişi", new Color(0.055f, 0.058f, 0.08f, 0.94f)).At(new Vector2(1, 1), new Vector2(-12, -54), new Vector2(300, 46 + Mathf.Max(1, lines.Count) * 34), new Vector2(1, 1));
            Tween.Scale(logPanel, 0.9f, 1, 0.25f);
            UI.Txt(logPanel, "Oylama geçmişi", 13, null, UI.Bold).rectTransform.At(new Vector2(0, 1), new Vector2(14, -10), new Vector2(200, 18), new Vector2(0, 1));
            UI.Txt(logPanel, "kapat", 10, UI.Muted, null, TextAnchor.MiddleRight).rectTransform.At(new Vector2(1, 1), new Vector2(-12, -10), new Vector2(60, 18), new Vector2(1, 1));
            UI.Click(logPanel, () => { if (logPanel) Destroy(logPanel.gameObject); logPanel = null; });
            if (lines.Count == 0) UI.Txt(logPanel, "Henüz gündüz oylaması yapılmadı.", 11, UI.Muted).rectTransform.At(new Vector2(0, 1), new Vector2(14, -36), new Vector2(270, 18), new Vector2(0, 1));
            for (int i = 0; i < lines.Count; i++)
            {
                var t = UI.Txt(logPanel, lines[i], 11, UI.Fg); t.rectTransform.At(new Vector2(0, 1), new Vector2(14, -34 - i * 34), new Vector2(272, 32), new Vector2(0, 1));
            }
        }

        // kozmetik çerçeve: ismin arkasında renkli hafif hap
        public void TagFrame(Tag t, string frameId)
        {
            var it = Catalog.Get(frameId); if (it == null) return;
            var bg = UI.Img(t.name.transform, UI.Pill, UI.A(it.tint, 0.28f), "çerçeve"); bg.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(Mathf.Min(150, t.name.preferredWidth + 22), 22)); bg.transform.SetAsFirstSibling();
            var o = UI.Img(bg.rectTransform, UI.OutlinePill, UI.A(it.tint, 0.8f), "kenar"); o.rectTransform.Fill();
            // metin üstte kalsın: arka planı ayrı bir kardeşe taşı
            bg.transform.SetParent(t.r, true); bg.transform.SetAsFirstSibling();
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
            // çerçevesiz isim: harflerin çevresinde siyah dış çizgi
            DestroyImmediate(tg.name.GetComponent<Shadow>());
            var ol = tg.name.gameObject.AddComponent<Outline>(); ol.effectColor = new Color(0, 0, 0, 0.95f); ol.effectDistance = new Vector2(1.4f, -1.4f);
            var ol2 = tg.name.gameObject.AddComponent<Outline>(); ol2.effectColor = new Color(0, 0, 0, 0.6f); ol2.effectDistance = new Vector2(-1.2f, 1.2f);
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
        public void Bubble(Tag t, string text) { t.bubble.text = text; t.bubbleBox.gameObject.SetActive(true); t.bubbleUntil = Time.time + 3.8f; Scale(t.bubbleBox, new Vector3(0.6f, 0.6f, 1), Vector3.one, 0.25f); Sfx.Play("pop", 0.6f, 1.2f); }
        public void PunchTag(Tag t) { Scale(t.r, Vector3.one * 1.25f, Vector3.one, 0.3f); }
        public void Emote(Tag t, string key) { t.emo.sprite = Emoji(key); t.emo.enabled = true; t.emoUntil = Time.time + 2.4f; Scale(t.emo.rectTransform, Vector3.zero, Vector3.one, 0.35f); }
        public void Seer(Tag t, string text, float sec) { t.seer.text = text; t.seerBox.gameObject.SetActive(true); t.seerUntil = Time.time + sec; }

        public void PlaceTag(Tag t, Vector3 world, Camera cam, bool dead, bool selected, string[] iconKeys, string badge, bool wolfBadge, bool mate = false)
        {
            var sp = cam.WorldToScreenPoint(world);
            bool vis = sp.z > 0 && sp.x > -Screen.width * 0.1f && sp.x < Screen.width * 1.1f;
            t.r.gameObject.SetActive(vis); if (!vis) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(tagsRoot, sp, null, out var lp);
            t.r.anchoredPosition = lp;
            // vampirler birbirinin adını koyu kırmızı görür
            t.name.color = dead ? new Color(1, 1, 1, 0.45f) : selected ? new Color(0.96f, 0.77f, 0.26f) : mate ? new Color(0.72f, 0.06f, 0.1f) : Color.white;
            for (int i = 0; i < t.icons.Count; i++) { var k = i < iconKeys.Length ? iconKeys[i] : null; var s = Emoji(k); if (t.icons[i].sprite != s) t.icons[i].sprite = s; t.icons[i].enabled = s != null; }
            bool hasB = !string.IsNullOrEmpty(badge); if (t.badgeBox.gameObject.activeSelf != hasB) t.badgeBox.gameObject.SetActive(hasB);
            if (hasB) { if (t.badge.text != badge) { Scale(t.badgeBox, Vector3.one * 1.45f, Vector3.one, 0.3f); Sfx.Play("pop", 0.35f, 1.5f); } t.badge.text = badge; t.badgeBox.GetComponent<Image>().color = wolfBadge ? new Color(1f, 0.3f, 0.43f, 0.95f) : new Color(0.96f, 0.77f, 0.26f, 0.95f); }
            if (t.bubbleBox.gameObject.activeSelf && Time.time > t.bubbleUntil) t.bubbleBox.gameObject.SetActive(false);
            if (t.emo.enabled && Time.time > t.emoUntil) t.emo.enabled = false;
            if (t.seerBox.gameObject.activeSelf && Time.time > t.seerUntil) t.seerBox.gameObject.SetActive(false);
        }

        // sohbet satırları bir süre yeni mesaj gelmezse yavaşça kaybolur (3B alan açık kalsın)
        readonly List<(RectTransform r, float t)> logRows = new List<(RectTransform r, float t)>();
        void FadeLog()
        {
            for (int i = logRows.Count - 1; i >= 0; i--)
            {
                var (r, t0) = logRows[i]; if (r == null) { logRows.RemoveAt(i); continue; }
                float age = Time.time - t0, a = age < 7f ? 1f : 1f - (age - 7f) / 1.5f;
                var g = r.GetComponent<CanvasGroup>(); if (g != null) g.alpha = Mathf.Clamp01(a);
                if (a <= 0) { logRows.RemoveAt(i); Destroy(r.gameObject); }
            }
        }
        public void HideHint() { if (hint != null) hint.enabled = false; }

        void Update() { try { UpdateInner(); } catch (System.Exception e) { ZuLog.Err("Hud.Update", e); } }
        void UpdateInner()
        {
            if (timerOn)
            {
                float left = Mathf.Max(0, phaseEnd - Time.time) * Game.SPEED; int s = Mathf.CeilToInt(left);
                timer.text = (s / 60) + ":" + (s % 60).ToString("00"); timer.color = left < 10 ? new Color(1f, 0.4f, 0.45f) : GOLD;
                if (s != lastSec) { lastSec = s; if (s <= 5 && s > 0) { Sfx.Play("tick"); Scale(timer.rectTransform, Vector3.one * 1.3f, Vector3.one, 0.25f); } }
            }
            FadeLog();
            if (toastG.alpha > 0 && Time.time > toastUntil) toastG.alpha = Mathf.Max(0, toastG.alpha - Time.deltaTime * 3);
            lid = Mathf.MoveTowards(lid, eyesClosed ? 1 : 0, Time.deltaTime * 2.2f);
            float h = canvas.rect.height * 0.62f * lid; // iki kapak tam kapandığında ekranın tamamı kararır
            lidTop.sizeDelta = new Vector2(0, h); lidBot.sizeDelta = new Vector2(0, h);
            // "uyanıyor" yazısı ve ikonu, gözler kapandığında ekranın tam ortasında
            float mid = h - canvas.rect.height * 0.5f; // alt kapağın üst kenarından ekran ortasına
            sleepI.rectTransform.anchoredPosition = new Vector2(0, -mid + 52); sleepT.rectTransform.anchoredPosition = new Vector2(0, -mid + 4); sleepS.rectTransform.anchoredPosition = new Vector2(0, -mid - 24);
            lidTop.gameObject.SetActive(lid > 0.001f); lidBot.gameObject.SetActive(lid > 0.001f);
            if (hint != null && hint.enabled && Time.time > 9) hint.enabled = false;
            // animasyonlar
            for (int i = tws.Count - 1; i >= 0; i--)
            {
                var w = tws[i]; if (w.r == null && w.g == null) { tws.RemoveAt(i); continue; }
                float u = (Time.time - w.t0) / w.dur; if (u < 0) continue; u = Mathf.Clamp01(u);
                if (w.r != null) w.r.localScale = Vector3.LerpUnclamped(w.from, w.to, EaseBack(u));
                if (w.g != null) w.g.alpha = Mathf.Lerp(w.a0, w.a1, u);
                if (u >= 1) tws.RemoveAt(i);
            }
            if (cardGlow != null && cardGlow.gameObject.activeInHierarchy) cardGlow.rectTransform.localEulerAngles = new Vector3(0, 0, Time.time * 20);
        }
        int lastSec = -1;
        class Tw { public RectTransform r; public CanvasGroup g; public float t0, dur, a0, a1; public Vector3 from, to; }
        readonly List<Tw> tws = new List<Tw>();
        public void Scale(RectTransform r, Vector3 from, Vector3 to, float dur, float delay = 0)
        {
            if (r == null) return; tws.RemoveAll(x => x.r == r); r.localScale = from;
            tws.Add(new Tw { r = r, from = from, to = to, dur = dur, t0 = Time.time + delay });
        }
        void Fade(CanvasGroup g, float a, float b, float dur) { g.alpha = a; tws.Add(new Tw { g = g, a0 = a, a1 = b, dur = dur, t0 = Time.time }); }
        void Punch(RectTransform r, float s) { Scale(r, Vector3.one * s, Vector3.one, 0.2f); }
        static float EaseBack(float u) { const float c1 = 1.70158f, c3 = c1 + 1; float x = u - 1; return 1 + c3 * x * x * x + c1 * x * x; }
    }
}
