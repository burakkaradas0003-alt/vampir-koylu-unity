// Ana menü: lobi, karakter özelleştirme, mağaza/envanter, görevler/başarımlar, profil.
// 3B köy sahnesi arka planda yaşar: lobide oyuncular ateşin çevresine gelip oturur.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VK
{
    public class Menu : MonoBehaviour
    {
        public static Menu I;
        public Action onStart;
        World W; Canvas canvas; RectTransform root, content, rail, toastR; Text toastT; CanvasGroup toastG, fadeG;
        readonly Dictionary<string, RectTransform> screens = new Dictionary<string, RectTransform>();
        readonly Dictionary<string, UI.Btn> railBtns = new Dictionary<string, UI.Btn>();
        readonly Dictionary<string, Image> railDots = new Dictionary<string, Image>();
        string current; CanvasGroup dimG;
        Text coinsT, nameT, levelT; UI.Bar xpBar; RawImage meAvatar; Image meRing;

        public static Menu Make(World w) { var g = new GameObject("Menü"); I = g.AddComponent<Menu>(); I.W = w; I.Build(); return I; }

        void Build()
        {
            canvas = UI.Canvas("Menü Arayüzü", 20);
            var cr = (RectTransform)canvas.transform;
            // okunabilirlik için kenarlarda yumuşak karartma (3B sahne ortada açık kalır)
            var sl = UI.Img(cr, UI.GradH, new Color(0.03f, 0.03f, 0.05f, 0.75f), "sol karartma"); sl.rectTransform.anchorMin = Vector2.zero; sl.rectTransform.anchorMax = new Vector2(0.42f, 1); sl.rectTransform.offsetMin = sl.rectTransform.offsetMax = Vector2.zero;
            var sr = UI.Img(cr, UI.GradH, new Color(0.03f, 0.03f, 0.05f, 0.8f), "sağ karartma"); sr.rectTransform.anchorMin = new Vector2(0.5f, 0); sr.rectTransform.anchorMax = Vector2.one; sr.rectTransform.offsetMin = sr.rectTransform.offsetMax = Vector2.zero; sr.rectTransform.localScale = new Vector3(-1, 1, 1);
            var tp = UI.Img(cr, UI.Grad, new Color(0.03f, 0.03f, 0.05f, 0.6f), "üst karartma"); tp.rectTransform.anchorMin = new Vector2(0, 1); tp.rectTransform.anchorMax = Vector2.one; tp.rectTransform.pivot = new Vector2(0.5f, 1); tp.rectTransform.sizeDelta = new Vector2(0, 90); tp.rectTransform.localScale = new Vector3(1, -1, 1);
            // lobi dışındaki ekranlarda sahne hafifçe kararır (okunabilirlik)
            var dimI = UI.Img(cr, null, new Color(0.03f, 0.03f, 0.05f, 0.62f), "karartma"); dimI.rectTransform.Fill(); dimG = dimI.gameObject.AddComponent<CanvasGroup>(); dimG.alpha = 0; dimG.blocksRaycasts = false;
            root = UI.R("güvenli alan", cr); root.gameObject.AddComponent<SafeArea>();
            BuildRail(); BuildTopBar();
            content = UI.R("içerik", root).Fill(86, 10, 12, 56);
            screens["lobi"] = BuildLobby(); screens["karakter"] = BuildCharacter(); screens["magaza"] = BuildShop(); screens["gorev"] = BuildQuests(); screens["profil"] = BuildProfile();
            foreach (var s in screens.Values) s.gameObject.SetActive(false);
            content.SetAsFirstSibling(); // şerit ve üst çubuk sahnenin üstünde kalsın
            // bildirim ve geçiş perdesi
            toastR = UI.Panel(root, "bildirim", UI.A(UI.Anthracite, 0.95f), true, UI.Pill).At(new Vector2(0, 0), new Vector2(96, 16), new Vector2(380, 36), new Vector2(0, 0));
            toastG = toastR.gameObject.AddComponent<CanvasGroup>(); toastG.alpha = 0; toastG.blocksRaycasts = false;
            toastT = UI.Txt(toastR, "", 13, null, UI.Bold, TextAnchor.MiddleCenter); toastT.rectTransform.Fill(14, 0, 14, 0);
            var fade = UI.Img(cr, null, Color.black, "perde"); fade.rectTransform.Fill(); fadeG = fade.gameObject.AddComponent<CanvasGroup>(); fadeG.alpha = 1; fadeG.blocksRaycasts = false;
            Tween.Fade(fadeG, 1, 0, 0.6f);
            Store.Changed += RefreshTop; RefreshTop();
            Show("lobi");
            W.SetTOD(0, 0.01f);
        }
        void OnDestroy() { Store.Changed -= RefreshTop; }

        // ---------------------------------------------------------------- sol menü şeridi
        static readonly (string id, string label, string icon)[] TABS = { ("lobi", "Lobi", "village"), ("karakter", "Karakter", "tunic"), ("magaza", "Mağaza", "chest"), ("gorev", "Görevler", "scroll"), ("profil", "Profil", "hood") };
        void BuildRail()
        {
            rail = UI.Panel(root, "şerit", UI.A(UI.Ink, 0.82f)).At(new Vector2(0, 0.5f), new Vector2(10, 0), new Vector2(66, 360), new Vector2(0, 0.5f));
            rail.anchorMin = new Vector2(0, 0); rail.anchorMax = new Vector2(0, 1); rail.offsetMin = new Vector2(10, 10); rail.offsetMax = new Vector2(76, -10);
            var col = UI.R("sekmeler", rail).Fill(0, 50, 0, 8); UI.VCol(col, 4, TextAnchor.UpperCenter, UI.Pad(6, 6, 4, 4));
            foreach (var t in TABS)
            {
                var id = t.id;
                var cell = UI.R(t.id, col); cell.LE(-1, 54);
                var bg = UI.Img(cell, UI.Tile ?? UI.Round, Color.white, "vurgu"); bg.type = Image.Type.Sliced; bg.pixelsPerUnitMultiplier = 2.4f; bg.rectTransform.Fill(); bg.raycastTarget = true;
                var art = Resources.Load<Sprite>("Rail/" + t.id); // kullanıcının sekme görselleri (olduğu gibi)
                var ic = UI.Icon(cell, art ?? Hud.Emoji(t.icon), art ? 36 : 26); if (art) ic.rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, -1), new Vector2(36, 36), new Vector2(0.5f, 1)); else ic.rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, -6), new Vector2(26, 26), new Vector2(0.5f, 1));
                var lb = UI.Txt(cell, t.label, 10, UI.Muted, UI.Bold, TextAnchor.MiddleCenter); lb.rectTransform.At(new Vector2(0.5f, 0), new Vector2(0, 5), new Vector2(60, 14), new Vector2(0.5f, 0));
                var dot = UI.Img(cell, UI.Circle, UI.Gold, "nokta"); dot.rectTransform.At(new Vector2(1, 1), new Vector2(-6, -6), new Vector2(8, 8)); dot.enabled = false; railDots[id] = dot;
                UI.Click(cell, () => Show(id));
                railBtns[id] = new UI.Btn { r = cell, bg = bg, label = lb, icon = ic };
            }
            var exit = UI.R("çık", rail).At(new Vector2(0.5f, 0), new Vector2(0, 8), new Vector2(54, 44), new Vector2(0.5f, 0));
            var exb = UI.Img(exit, UI.Tile ?? UI.Round, Color.white, "zemin"); exb.type = Image.Type.Sliced; exb.pixelsPerUnitMultiplier = 2.4f; exb.rectTransform.Fill(); exb.raycastTarget = true;
            UI.Icon(exit, Hud.Emoji("door"), 18).rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, -4), new Vector2(18, 18), new Vector2(0.5f, 1));
            var ex = UI.Txt(exit, "Çık", 10, UI.Muted, UI.Bold, TextAnchor.MiddleCenter); ex.rectTransform.At(new Vector2(0.5f, 0), new Vector2(0, 3), new Vector2(50, 14), new Vector2(0.5f, 0));
            UI.Click(exit, () => Bridge.Send("{\"type\":\"exit\"}"));
        }

        // ---------------------------------------------------------------- üst çubuk
        Text screenTitle; Image titleCrown, titleLine;
        void BuildTopBar()
        {
            var bar = UI.R("üst", root); bar.anchorMin = new Vector2(0, 1); bar.anchorMax = new Vector2(1, 1); bar.pivot = new Vector2(0.5f, 1); bar.offsetMin = new Vector2(86, -50); bar.offsetMax = new Vector2(-12, -8);
            // oyuncu kartı
            var me = UI.Panel(bar, "ben", UI.A(UI.Ink, 0.7f), false, UI.Pill).At(new Vector2(0, 0.5f), Vector2.zero, new Vector2(214, 42), new Vector2(0, 0.5f));
            meAvatar = UI.Avatar(me, 34, null, "", out meRing); meAvatar.transform.parent.parent.GetComponent<RectTransform>().At(new Vector2(0, 0.5f), new Vector2(4, 0), new Vector2(34, 34), new Vector2(0, 0.5f));
            nameT = UI.Txt(me, "", 13, null, UI.Bold); nameT.rectTransform.At(new Vector2(0, 1), new Vector2(46, -5), new Vector2(120, 18), new Vector2(0, 1));
            levelT = UI.Txt(me, "", 10, UI.Lilac, UI.Bold, TextAnchor.MiddleRight); levelT.rectTransform.At(new Vector2(1, 1), new Vector2(-12, -6), new Vector2(60, 16), new Vector2(1, 1));
            xpBar = UI.ProgressBar(me, UI.Purple, 5); xpBar.r.At(new Vector2(0, 0), new Vector2(46, 9), new Vector2(156, 5), new Vector2(0, 0));
            UI.Click(me, () => Show("profil"));
            screenTitle = UI.Txt(bar, "", 25, null, UI.Serif, TextAnchor.MiddleCenter); screenTitle.rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(20, -2), new Vector2(320, 34)); screenTitle.GoldTitle();
            titleCrown = UI.Icon(bar, Hud.Emoji("crown"), 16); titleCrown.rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(20, 19), new Vector2(16, 16));
            titleLine = UI.Img(bar, UI.OrnLine, Color.white, "başlık süsü"); titleLine.rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(20, -21), new Vector2(240, 15)); titleLine.raycastTarget = false;
            // altın
            var gem = UI.Panel(bar, "elmas", UI.A(UI.Ink, 0.7f), false, UI.Pill).At(new Vector2(1, 0.5f), Vector2.zero, new Vector2(104, 34), new Vector2(1, 0.5f));
            UI.Icon(gem, Hud.Emoji("ruby"), 20).rectTransform.At(new Vector2(0, 0.5f), new Vector2(10, 0), new Vector2(20, 20), new Vector2(0, 0.5f));
            UI.Txt(gem, "0", 15, new Color(0.6f, 0.85f, 1f), UI.Title, TextAnchor.MiddleLeft).rectTransform.At(new Vector2(0, 0.5f), new Vector2(36, 1), new Vector2(40, 26), new Vector2(0, 0.5f));
            var gplus = UI.Img(gem, UI.Circle, UI.Purple, "artı"); gplus.rectTransform.At(new Vector2(1, 0.5f), new Vector2(-5, 0), new Vector2(24, 24), new Vector2(1, 0.5f));
            UI.Txt(gplus.rectTransform, "+", 16, null, UI.Bold, TextAnchor.MiddleCenter).rectTransform.Fill(0, 2, 0, 0);
            UI.Click(gem, () => Toast("Elmaslar yakında: özel kostümler ve sezon kartı için"));
            var coin = UI.Panel(bar, "altın", UI.A(UI.Ink, 0.7f), false, UI.Pill).At(new Vector2(1, 0.5f), new Vector2(-112, 0), new Vector2(118, 34), new Vector2(1, 0.5f));
            UI.Icon(coin, Hud.Emoji("coin"), 20).rectTransform.At(new Vector2(0, 0.5f), new Vector2(8, 0), new Vector2(20, 20), new Vector2(0, 0.5f));
            coinsT = UI.Txt(coin, "0", 15, UI.Gold, UI.Title, TextAnchor.MiddleLeft); coinsT.rectTransform.At(new Vector2(0, 0.5f), new Vector2(34, 1), new Vector2(76, 26), new Vector2(0, 0.5f));
            var plus = UI.Img(coin, UI.Circle, UI.Purple, "artı"); plus.rectTransform.At(new Vector2(1, 0.5f), new Vector2(-5, 0), new Vector2(24, 24), new Vector2(1, 0.5f));
            UI.Txt(plus.rectTransform, "+", 16, null, UI.Bold, TextAnchor.MiddleCenter).rectTransform.Fill(0, 2, 0, 0);
            UI.Click(coin, () => Toast("Zu altın, maç ve görev ödülleriyle kazanılır. Zuplay hesabınla eşitlenecek."));
        }
        int shownCoins = -1;
        void RefreshTop()
        {
            var p = Store.P; if (p == null || nameT == null) return;
            nameT.text = p.name; levelT.text = "Sv. " + p.level; xpBar.Set(p.xp / (float)Profile.XpFor(p.level));
            if (shownCoins < 0) coinsT.text = p.coins.ToString(); else if (shownCoins != p.coins) Tween.Count(coinsT, shownCoins, p.coins, 0.7f);
            shownCoins = p.coins;
            meAvatar.texture = Studio.I != null ? Studio.I.Portrait(p.look, 96) : null; meAvatar.enabled = meAvatar.texture != null; meRing.color = UI.FrameColor(p.look.frame);
            int cl = Store.Claimable() + SeasonClaimable(); if (railDots.TryGetValue("gorev", out var d)) d.enabled = cl > 0;
        }

        public void Toast(string s, float sec = 2.6f)
        {
            toastT.text = s; Sfx.Play("notify", 0.7f);
            Tween.Fade(toastG, toastG.alpha, 1, 0.18f); Tween.Scale(toastR, 0.9f, 1, 0.3f);
            CancelInvoke(nameof(HideToast)); Invoke(nameof(HideToast), sec);
        }
        void HideToast() { Tween.Fade(toastG, toastG.alpha, 0, 0.3f); }

        static readonly Dictionary<string, string> TITLES = new Dictionary<string, string> { { "lobi", "" }, { "karakter", "Karakter" }, { "magaza", "Mağaza" }, { "gorev", "Görevler" }, { "profil", "Profil" } };
        public void Show(string id)
        {
            if (current == id) return;
            if (current != null && screens.TryGetValue(current, out var old)) old.gameObject.SetActive(false);
            if (current == "karakter" && id != "karakter") CommitDraftPreviewOff();
            bool toChar = id == "karakter";
            current = id; screenTitle.text = TITLES[id]; bool hasT = !string.IsNullOrEmpty(TITLES[id]); titleCrown.enabled = hasT; titleLine.enabled = hasT && UI.OrnLine != null;
            foreach (var kv in railBtns) { bool on = kv.Key == id; kv.Value.bg.sprite = on && UI.TileOn != null ? UI.TileOn : (UI.Tile ?? UI.Round); kv.Value.bg.color = Color.white; kv.Value.label.color = on ? UI.Fg : UI.Muted; }
            var s = screens[id]; Tween.ShowScreen(s);
            Studio.I?.HideShowcase();
            if (id == "karakter") OpenCharacter();
            if (id == "magaza") RefreshShop();
            if (id == "gorev") RefreshQuests();
            if (id == "profil") RefreshProfile();
            if (id == "lobi") RefreshLobby();
            camTarget = id == "lobi" ? 0 : 1;
            Tween.Fade(dimG, dimG.alpha, id == "lobi" || id == "karakter" ? 0 : 1, 0.3f);
        }

        // ---------------------------------------------------------------- 3B kamera
        float camT, camTarget, orbit;
        void Update() { try { UpdateInner(); } catch (System.Exception e) { ZuLog.Err("Menu.Update", e); } }
        void UpdateInner()
        {
            camT = Mathf.MoveTowards(camT, camTarget, Time.deltaTime * 1.5f);
            orbit += Time.deltaTime * 0.035f;
            float a = orbit + 0.6f, k = Tween.InOut(camT);
            var lobbyPos = new Vector3(Mathf.Cos(a) * 9.5f, 3.4f, Mathf.Sin(a) * 9.5f);
            var otherPos = new Vector3(Mathf.Cos(a) * 12.5f, 5.2f, Mathf.Sin(a) * 12.5f);
            var cam = W.cam.transform; cam.position = Vector3.Lerp(lobbyPos, otherPos, k);
            var look = new Vector3(0, 0.7f, 0) + Quaternion.Euler(0, -a * Mathf.Rad2Deg, 0) * new Vector3(0, 0, 0);
            // sahnenin merkezi sağdaki panelin solunda kalsın
            var right = Vector3.Cross(Vector3.up, (look - cam.position).normalized);
            cam.rotation = Quaternion.LookRotation(look + right * 2.4f - cam.position);
            W.cam.fieldOfView = 52;
            LobbyTick();
        }

        // ================================================================ LOBİ
        class Slot { public string name; public Look look; public bool host, me, ready; public Rig rig; public float readyAt; public RectTransform row; }
        readonly List<Slot> slots = new List<Slot>();
        int mode = 9; RectTransform lobbyList; Text countT, roomCode; UI.Btn startBtn; UI.Bar countBar; float nextJoin = 1.2f, nextLeave = 14; readonly System.Random lr = new System.Random();
        UI.Btn modeA, modeB; string code;
        static readonly string[] NAMES = { "Mira", "Deniz", "Ece", "Kaan", "Selin", "Arda", "Zeynep", "Emre", "Lale", "Can", "Duru", "Efe" };

        RectTransform BuildLobby()
        {
            var s = UI.R("lobi", content).Fill();
            code = (1000 + lr.Next(9000)).ToString();
            var pan = UI.Panel(s, "oda").At(new Vector2(1, 0.5f), Vector2.zero, new Vector2(300, 0), new Vector2(1, 0.5f)); pan.anchorMin = new Vector2(1, 0); pan.anchorMax = new Vector2(1, 1); pan.sizeDelta = new Vector2(300, 0);
            // başlık
            var t = UI.Txt(pan, "Vampir Köylü", 22, null, UI.Display); t.rectTransform.At(new Vector2(0, 1), new Vector2(16, -10), new Vector2(170, 26), new Vector2(0, 1));
            var codeB = UI.Panel(pan, "kod", new Color(1, 1, 1, 0.06f), false, UI.Pill).At(new Vector2(1, 1), new Vector2(-12, -12), new Vector2(98, 24), new Vector2(1, 1));
            roomCode = UI.Txt(codeB, "Oda #" + code, 11, UI.Lilac, UI.Bold, TextAnchor.MiddleCenter); roomCode.rectTransform.Fill();
            UI.Click(codeB, () => { GUIUtility.systemCopyBuffer = "zuplay://vk/" + code; Toast("Oda kodu kopyalandı: #" + code); });
            countT = UI.Txt(pan, "", 11, UI.Muted, UI.Bold); countT.rectTransform.At(new Vector2(0, 1), new Vector2(16, -38), new Vector2(160, 16), new Vector2(0, 1));
            countBar = UI.ProgressBar(pan, UI.Purple, 4); countBar.r.At(new Vector2(1, 1), new Vector2(-14, -44), new Vector2(110, 4), new Vector2(1, 1));
            // mod ve harita
            var mrow = UI.R("mod", pan).At(new Vector2(0, 1), new Vector2(12, -58), new Vector2(276, 26), new Vector2(0, 1)); UI.HRow(mrow, 6, TextAnchor.MiddleLeft, null, true);
            modeA = UI.Chip(mrow, "Klasik · 9", true, () => SetMode(9)); modeB = UI.Chip(mrow, "Hızlı · 7", false, () => SetMode(7));
            var map = UI.Panel(pan, "harita", new Color(1, 1, 1, 0.05f), false, UI.RoundSmall).At(new Vector2(0, 1), new Vector2(12, -90), new Vector2(276, 30), new Vector2(0, 1));
            UI.Icon(map, Hud.Emoji("oldmap"), 16).rectTransform.At(new Vector2(0, 0.5f), new Vector2(8, 0), new Vector2(16, 16), new Vector2(0, 0.5f));
            UI.Txt(map, "Harita: <b>Köy Meydanı</b>", 11).rectTransform.At(new Vector2(0, 0.5f), new Vector2(30, 0), new Vector2(150, 20), new Vector2(0, 0.5f));
            var soon = UI.Txt(map, "Sisli Orman · yakında", 10, UI.Muted, null, TextAnchor.MiddleRight); soon.rectTransform.At(new Vector2(1, 0.5f), new Vector2(-8, 0), new Vector2(120, 20), new Vector2(1, 0.5f));
            UI.Click(map, () => Toast("Yeni harita yakında geliyor"));
            // oda ayarı: ölünce rol açıklansın
            var rset = UI.Panel(pan, "rol ayarı", new Color(1, 1, 1, 0.05f), false, UI.RoundSmall).At(new Vector2(0, 1), new Vector2(12, -124), new Vector2(276, 28), new Vector2(0, 1));
            UI.Icon(rset, Hud.Emoji("skull"), 16).rectTransform.At(new Vector2(0, 0.5f), new Vector2(8, 0), new Vector2(16, 16), new Vector2(0, 0.5f));
            UI.Txt(rset, "Ölenin rolü açıklansın", 11).rectTransform.At(new Vector2(0, 0.5f), new Vector2(30, 0), new Vector2(170, 20), new Vector2(0, 0.5f));
            var sw = UI.Panel(rset, "anahtar", Color.white, false, UI.Pill).At(new Vector2(1, 0.5f), new Vector2(-8, 0), new Vector2(38, 18), new Vector2(1, 0.5f));
            var knob = UI.Img(sw, UI.Circle, Color.white, "düğme"); knob.rectTransform.At(new Vector2(0, 0.5f), Vector2.zero, new Vector2(14, 14));
            void Paint() { bool on = Game.RevealRoles; sw.GetComponent<Image>().color = on ? new Color(0.2f, 0.7f, 0.42f) : new Color(1, 1, 1, 0.18f); knob.rectTransform.anchoredPosition = new Vector2(on ? 22 : 2, 0); }
            Paint();
            UI.Click(rset, () => { Game.RevealRoles = !Game.RevealRoles; Paint(); Toast(Game.RevealRoles ? "Ölen kişinin rolü herkese gösterilecek" : "Roller gizli kalacak"); });
            // oyuncular
            var sc = UI.Scroll(pan, true, out lobbyList, 4); ((RectTransform)sc.transform).Fill(10, 52, 10, 158);
            // alt düğmeler
            var brow = UI.R("düğmeler", pan).At(new Vector2(0.5f, 0), new Vector2(0, 10), new Vector2(276, 36), new Vector2(0.5f, 0)); UI.HRow(brow, 8, TextAnchor.MiddleCenter, null, true);
            var inv = UI.Button(brow, "Davet et", UI.Style.Secondary, Invite, Hud.Emoji("letter"), 13); inv.r.LE(-1, 36, 1);
            startBtn = UI.Button(brow, "Başlat", UI.Style.Primary, StartGame, Hud.Emoji("torch"), 14, "whoosh"); startBtn.r.LE(-1, 36, 1.4f);
            // ben (oda sahibi)
            AddSlot(new Slot { name = Store.P.name, look = Store.P.look.Clone(), host = true, me = true, ready = true }, false);
            return s;
        }
        void SetMode(int m)
        {
            if (mode == m) return; mode = m; modeA.Restyle(m == 9 ? UI.Style.Primary : UI.Style.Secondary); modeB.Restyle(m == 7 ? UI.Style.Primary : UI.Style.Secondary);
            while (slots.Count > mode) RemoveSlot(slots[slots.Count - 1]);
            RefreshLobby(); Toast(m == 9 ? "Klasik mod: 9 oyuncu, tüm roller" : "Hızlı mod: 7 oyuncu, kısa maç");
        }
        Vector3 SeatPos(int i, int n) { float a = Mathf.PI / 2 + i / (float)n * Mathf.PI * 2; return new Vector3(Mathf.Cos(a) * 4.3f, 0, Mathf.Sin(a) * 4.3f); }
        void AddSlot(Slot sl, bool fx = true)
        {
            slots.Add(sl); int i = slots.Count - 1;
            sl.rig = Rig.Make(sl.look, 0.96f + (i * 37 % 9) / 100f);
            var p = SeatPos(i, 9); sl.rig.transform.position = p; sl.rig.transform.LookAt(Vector3.zero);
            if (fx)
            {
                var tr = sl.rig.transform; tr.localScale = Vector3.one * 0.01f;
                Tween.Run(sl.rig, 0.45f, k => { if (tr) tr.localScale = Vector3.one * Mathf.Max(0.01f, k); }, Tween.OutBack);
                W.Burst(p + Vector3.up * 0.8f, UI.Lilac, 22, 0.7f, 0.12f, 1.1f, 0, 0.6f); Sfx.Play("pop", 0.8f, 1.1f);
                sl.rig.wave = 1.4f;
                if (current == "lobi") Toast(sl.name + " odaya katıldı");
            }
            sl.readyAt = Time.time + 1.5f + (float)lr.NextDouble() * 4;
            RefreshLobby();
        }
        void RemoveSlot(Slot sl)
        {
            int idx = slots.IndexOf(sl); if (idx < 0 || sl.me) return;
            slots.RemoveAt(idx);
            var rig = sl.rig; var tr = rig.transform; W.Burst(tr.position + Vector3.up * 0.8f, new Color(0.7f, 0.75f, 0.9f), 14, 0.5f, 0.12f, 0.9f);
            Tween.Run(rig, 0.3f, k => { if (tr) tr.localScale = Vector3.one * Mathf.Max(0.01f, 1 - k); }, Tween.OutCubic, 0, () => { if (rig) Destroy(rig.gameObject); });
            // kalanlar koltuklarına kayar
            for (int i = 0; i < slots.Count; i++) { var r2 = slots[i].rig; if (r2 == null) continue; r2.transform.position = SeatPos(i, 9); r2.transform.LookAt(Vector3.zero); }
            if (current == "lobi") Toast(sl.name + " odadan ayrıldı");
            RefreshLobby();
        }
        void LobbyTick()
        {
            if (slots.Count == 0) return;
            float t = Time.time; bool changed = false;
            foreach (var s in slots)
            {
                if (!s.ready && t > s.readyAt) { s.ready = true; changed = true; if (s.row != null) Tween.Punch(s.row, 0.95f); }
                if (s.rig != null && s.rig.wave > 0) { s.rig.wave = Mathf.Max(0, s.rig.wave - Time.deltaTime * 0.6f); s.rig.waveWiggle = Mathf.Sin(t * 10) * 0.3f; }
                if (s.rig != null) { s.rig.face.smile = 0.6f; }
            }
            if (current == "lobi" && starting == false)
            {
                if (slots.Count < mode && t > nextJoin) { nextJoin = t + 1.6f + (float)lr.NextDouble() * 2.6f; JoinBot(); }
                else if (slots.Count >= 4 && t > nextLeave) { nextLeave = t + 20 + (float)lr.NextDouble() * 20; var bots = slots.Where(x => !x.me).ToList(); if (bots.Count > 0) RemoveSlot(bots[lr.Next(bots.Count)]); nextJoin = t + 2.5f; }
            }
            if (changed) RefreshLobby();
        }
        void JoinBot()
        {
            var used = new HashSet<string>(slots.Select(s => s.name));
            var free = NAMES.Where(n => !used.Contains(n)).ToList(); if (free.Count == 0) return;
            AddSlot(new Slot { name = free[lr.Next(free.Count)], look = Catalog.Random(new System.Random(lr.Next())) });
        }
        void RefreshLobby()
        {
            if (lobbyList == null) return;
            UI.Clear(lobbyList);
            for (int i = 0; i < mode; i++)
            {
                if (i < slots.Count) slots[i].row = PlayerRow(lobbyList, slots[i]);
                else EmptyRow(lobbyList);
            }
            int ready = slots.Count(s => s.ready);
            countT.text = $"{slots.Count}/{mode} oyuncu · {ready} hazır"; countBar.Set(slots.Count / (float)mode);
            startBtn.Label(slots.Count >= mode ? "Başlat" : $"Başlat ({slots.Count}/{mode})");
        }
        RectTransform PlayerRow(Transform p, Slot s)
        {
            var r = UI.Panel(p, "oyuncu", s.me ? UI.A(UI.Purple, 0.16f) : new Color(1, 1, 1, 0.04f), false, UI.RoundSmall); r.LE(-1, 40);
            var av = UI.Avatar(r, 30, Studio.I?.Portrait(s.look, 96), s.look.frame, out _); ((RectTransform)av.transform.parent.parent).At(new Vector2(0, 0.5f), new Vector2(6, 0), new Vector2(30, 30), new Vector2(0, 0.5f));
            var nm = UI.Txt(r, s.name + (s.me ? " <color=#9A9CB0>(sen)</color>" : ""), 12, null, UI.Bold); nm.rectTransform.At(new Vector2(0, 1), new Vector2(44, -4), new Vector2(150, 17), new Vector2(0, 1));
            var title = Catalog.Get(s.look.title); var tt = UI.Txt(r, title != null ? title.name : "Köylü", 9, UI.Muted); tt.rectTransform.At(new Vector2(0, 0), new Vector2(44, 4), new Vector2(150, 14), new Vector2(0, 0));
            if (s.host) UI.Icon(r, Hud.Emoji("crown"), 14).rectTransform.At(new Vector2(0, 0.5f), new Vector2(26, 11), new Vector2(14, 14), new Vector2(0, 0.5f));
            var b = UI.Badge(r, s.ready ? "Hazır" : "Bekliyor", s.ready ? UI.A(UI.Good, 0.22f) : new Color(1, 1, 1, 0.06f), s.ready ? UI.Good : UI.Muted, 10);
            b.At(new Vector2(1, 0.5f), new Vector2(-8, 0), new Vector2(64, 20), new Vector2(1, 0.5f));
            return r;
        }
        void EmptyRow(Transform p)
        {
            var r = UI.Panel(p, "boş", new Color(1, 1, 1, 0.015f), false, UI.RoundSmall); r.LE(-1, 40);
            var c = UI.Img(r, UI.Ring, new Color(1, 1, 1, 0.16f), "halka"); c.rectTransform.At(new Vector2(0, 0.5f), new Vector2(6, 0), new Vector2(30, 30), new Vector2(0, 0.5f));
            UI.Txt(c.rectTransform, "+", 16, UI.Muted, UI.Bold, TextAnchor.MiddleCenter).rectTransform.Fill(0, 2, 0, 0);
            UI.Txt(r, "Boş koltuk", 11, UI.Muted).rectTransform.At(new Vector2(0, 0.5f), new Vector2(44, 0), new Vector2(120, 18), new Vector2(0, 0.5f));
            UI.Txt(r, "Davet et", 10, UI.Lilac, UI.Bold, TextAnchor.MiddleRight).rectTransform.At(new Vector2(1, 0.5f), new Vector2(-10, 0), new Vector2(80, 18), new Vector2(1, 0.5f));
            UI.Click(r, Invite);
        }
        void Invite()
        {
            GUIUtility.systemCopyBuffer = "zuplay://vk/" + code;
            Bridge.Send("{\"type\":\"invite\",\"room\":\"" + code + "\"}");
            Toast("Davet bağlantısı kopyalandı · arkadaşlarına gönder");
        }
        bool starting;
        void StartGame()
        {
            if (starting) return; starting = true;
            while (slots.Count < mode) JoinBot();
            Game.Players = mode;
            Game.Lineup = slots.Select(s => new Game.Seat { name = s.name, look = s.me ? Store.P.look.Clone() : s.look }).ToList();
            fadeG.blocksRaycasts = true;
            Tween.Fade(fadeG, 0, 1, 0.45f, 0.35f, () =>
            {
                foreach (var s in slots) if (s.rig != null) Destroy(s.rig.gameObject);
                slots.Clear(); Studio.I?.HideShowcase();
                canvas.gameObject.SetActive(false);
                onStart?.Invoke();
                Destroy(gameObject);
            });
        }

        // ================================================================ KARAKTER
        Look draft; Cat cat = Cat.Hair; RawImage show; RectTransform catRow, itemGrid, colorRow; Text footT; UI.Btn saveBtn;
        // Agrou tarzı: solda köy evinin önünde duran 3B karakter, sağda bölümlere ayrılmış seçenekler
        RectTransform charList, charTabs; Text nameTag, selT; string charTab;
        RectTransform BuildCharacter()
        {
            var s = UI.R("karakter", content); s.anchorMin = Vector2.zero; s.anchorMax = Vector2.one; s.offsetMin = new Vector2(-86, -10); s.offsetMax = new Vector2(0, 56);
            // sahne (tam ekranın solu): stüdyodaki giyinme setini gösterir
            var raw = UI.R("sahne", s); raw.anchorMin = Vector2.zero; raw.anchorMax = Vector2.one; raw.offsetMin = new Vector2(-40, -30); raw.offsetMax = new Vector2(80, 30); // ekran kenarlarına kadar taşar
            show = raw.gameObject.AddComponent<RawImage>(); show.raycastTarget = true; raw.gameObject.AddComponent<DragRotate>();
            raw.SetAsFirstSibling();
            // isim etiketi (sol üst)
            var tag = UI.Panel(raw, "isim", UI.A(UI.Ink, 0.75f), false, UI.Pill).At(new Vector2(0, 1), new Vector2(98, -60), new Vector2(190, 40), new Vector2(0, 1));
            nameTag = UI.Txt(tag, "", 16, null, UI.Display); nameTag.rectTransform.At(new Vector2(0, 1), new Vector2(14, -3), new Vector2(170, 20), new Vector2(0, 1));
            var tt = UI.Txt(tag, "", 9, UI.Gold, UI.Bold); tt.rectTransform.At(new Vector2(0, 0), new Vector2(14, 5), new Vector2(170, 14), new Vector2(0, 0)); tt.name = "unvan";
            tag.gameObject.SetActive(false); // karakterin üstünde isim/unvan kutusu gösterilmez
            var hint = UI.Txt(raw, "Karakteri döndürmek için sağa sola kaydır", 10, UI.Fg, null, TextAnchor.MiddleCenter).Shadowed(0.7f); hint.rectTransform.At(new Vector2(0.31f, 0), new Vector2(40, 12), new Vector2(300, 16), new Vector2(0.5f, 0));
            // sağ panel
            var pan = UI.Panel(s, "seçenekler", UI.A(UI.Ink, 0.9f)); pan.anchorMin = new Vector2(0.62f, 0); pan.anchorMax = Vector2.one; pan.offsetMin = new Vector2(4, 10); pan.offsetMax = new Vector2(-14, -60);
            // sekmeler ve düğmeler panelin genişliğine sığar (dar ekranda dışarı taşmaz)
            var tabs = UI.R("sekme", pan); tabs.anchorMin = new Vector2(0, 1); tabs.anchorMax = new Vector2(1, 1); tabs.pivot = new Vector2(0.5f, 1); tabs.offsetMin = new Vector2(10, -36); tabs.offsetMax = new Vector2(-10, -8); UI.HRow(tabs, 6, TextAnchor.MiddleCenter, null, true);
            UI.Chip(tabs, "Görünüm", true, () => { }, Hud.Emoji("tunic"));
            UI.Chip(tabs, "Mağaza", false, () => Show("magaza"), Hud.Emoji("pouch"));
            // alt sekmeler (Genel / Aksesuar / Efekt ...): bir anda tek bölüm gösterilir
            charTabs = UI.R("alt sekmeler", pan); charTabs.anchorMin = new Vector2(0, 1); charTabs.anchorMax = new Vector2(1, 1); charTabs.pivot = new Vector2(0.5f, 1); charTabs.offsetMin = new Vector2(10, -70); charTabs.offsetMax = new Vector2(-10, -42);
            UI.HRow(charTabs, 4, TextAnchor.MiddleCenter, null, true);
            var sc = UI.Scroll(pan, true, out charList, 6, UI.Pad(10, 10, 4, 8)); ((RectTransform)sc.transform).Fill(0, 76, 0, 74);
            selT = UI.Txt(pan, "", 10, UI.Muted, null, TextAnchor.MiddleCenter); selT.rectTransform.anchorMin = new Vector2(0, 0); selT.rectTransform.anchorMax = new Vector2(1, 0); selT.rectTransform.pivot = new Vector2(0.5f, 0); selT.rectTransform.offsetMin = new Vector2(8, 50); selT.rectTransform.offsetMax = new Vector2(-8, 72);
            var brow = UI.R("düğmeler", pan); brow.anchorMin = new Vector2(0, 0); brow.anchorMax = new Vector2(1, 0); brow.pivot = new Vector2(0.5f, 0); brow.offsetMin = new Vector2(8, 10); brow.offsetMax = new Vector2(-8, 44); UI.HRow(brow, 6, TextAnchor.MiddleCenter, null, true);
            UI.Button(brow, "Rastgele", UI.Style.Secondary, RandomLook, Hud.Emoji("dice"), 12).r.LE(-1, 34, 1);
            UI.Button(brow, "Sıfırla", UI.Style.Ghost, () => { draft = Store.P.look.Clone(); RefreshCharacter(); Toast("Kayıtlı görünüme dönüldü"); }, null, 12).r.LE(-1, 34, 0.8f);
            saveBtn = UI.Button(brow, "Onayla", UI.Style.Primary, SaveLook, Hud.Emoji("check"), 12, null); saveBtn.r.LE(-1, 34, 1.1f);
            footT = selT;
            return s;
        }
        void OpenCharacter()
        {
            draft = Store.P.look.Clone();
            // görüntü alanının en-boy oranında çiz: karakter basık/yayvan görünmesin
            Canvas.ForceUpdateCanvases(); var rr = show.rectTransform.rect; float asp = rr.height > 1 ? rr.width / rr.height : 1.1f;
            show.texture = Studio.I.Showcase(draft, Mathf.Clamp(Mathf.RoundToInt(720 * asp), 400, 1600), 720, true);
            Tween.Fade(dimG, dimG.alpha, 0, 0.3f);
            RefreshCharacter();
        }
        void CommitDraftPreviewOff() { }
        void RandomLook()
        {
            var r = new System.Random(); var l = Catalog.Random(r);
            l.effect = draft.effect; l.victory = draft.victory; l.frame = draft.frame; l.title = draft.title;
            if (!string.IsNullOrEmpty(draft.synty)) l.synty = Rig.SYNTY_PRESETS[r.Next(Rig.SYNTY_PRESETS.Length)]; else l.synty = "";
            draft = l; Sfx.Play("magic", 0.7f); RefreshCharacter();
        }
        // bölümler: başlık + ikon ızgarası (+ renk sırası)
        // kahraman modunda bölümler
        static readonly (string title, Cat[] cats)[] HERO_SECTIONS =
        {
            ("GENEL", new[] { Cat.Hero }),
            ("AKSESUAR", new[] { Cat.Frame, Cat.Title }),
            ("EFEKT", new[] { Cat.Effect, Cat.Victory }),
        };
        static readonly Dictionary<string, string> TAB_LABEL = new Dictionary<string, string> { { "GENEL", "Genel" }, { "BAŞ", "Baş" }, { "GÖVDE", "Gövde" }, { "EKSTRA", "Ekstra" }, { "AKSESUAR", "Aksesuar" }, { "EFEKT", "Efekt" } };
        static readonly Dictionary<string, string> TAB_ICON = new Dictionary<string, string> { { "GENEL", "tunic" }, { "BAŞ", "crown" }, { "GÖVDE", "tunic" }, { "EKSTRA", "wisp" }, { "AKSESUAR", "crown" }, { "EFEKT", "wisp" } };
        static readonly (string title, Cat[] cats)[] SECTIONS =
        {
            ("GENEL", new[] { Cat.Hero, Cat.Costume }),
            ("BAŞ", new[] { Cat.Hair, Cat.Hat, Cat.Glasses, Cat.Face, Cat.Mask, Cat.Head, Cat.Ears }),
            ("GÖVDE", new[] { Cat.Top, Cat.Bottom, Cat.Shoes, Cat.Hands }),
            ("EKSTRA", new[] { Cat.Effect, Cat.Victory, Cat.Frame, Cat.Title }),
        };
        void RefreshCharacter()
        {
            Studio.I.SetShow(draft);
            nameTag.text = Store.P.name; var ti = Catalog.Get(draft.title); nameTag.transform.parent.Find("unvan").GetComponent<Text>().text = ti != null ? ti.name : "";
            UI.Clear(charList); UI.Clear(charTabs);
            bool hero = !string.IsNullOrEmpty(draft.synty) || Rig.SyntyAvailable;
            var secs = (hero ? HERO_SECTIONS : SECTIONS).Select(sc => (sc.title, cats: sc.cats.Where(c => c != Cat.Hero || Rig.SyntyAvailable).Where(c => !hero || c == Cat.Hero || c >= Cat.Effect).ToArray())).Where(sc => sc.cats.Length > 0).ToList();
            if (charTab == null || !secs.Any(x => x.title == charTab)) charTab = secs.Count > 0 ? secs[0].title : null;
            foreach (var x in secs)
            {
                var t0 = x.title; var chip = UI.Chip(charTabs, TAB_LABEL.TryGetValue(t0, out var tl) ? tl : t0, t0 == charTab, () => { charTab = t0; RefreshCharacter(); }, Hud.Emoji(TAB_ICON.TryGetValue(t0, out var ti0) ? ti0 : "star"));
                chip.r.LE(-1, 28, 1);
            }
            foreach (var sec in secs)
            {
                if (sec.title != charTab) continue;
                var cats = sec.cats;
                if (sec.title == "GENEL" && !hero) Swatches(charList, "Ten rengi", Look.SKINS, () => draft.skin, v => draft.skin = v, true);
                foreach (var c in cats)
                {
                    Ribbon(charList, Catalog.CAT_NAME[c]);
                    IconRow(charList, c);
                    if (!hero && c == Cat.Top) Swatches(charList, "Üst rengi", Look.TINTS, () => draft.topColor, v => draft.topColor = v, false);
                    if (!hero && c == Cat.Bottom) Swatches(charList, "Alt rengi", Look.TINTS, () => draft.bottomColor, v => draft.bottomColor = v, false);
                    if (!hero && c == Cat.Hat && !string.IsNullOrEmpty(draft.hat)) Swatches(charList, "Şapka rengi", Look.TINTS, () => draft.hatColor, v => draft.hatColor = v, false);
                }
            }
            var missing = MissingItems(draft);
            int cost = missing.Sum(m => m.price);
            selT.text = missing.Count == 0 ? (draft.Json() == Store.P.look.Json() ? "Kayıtlı görünüm" : "Değişiklikler kaydedilmedi") : $"{missing.Count} ürün satın alınmamış · <color=#F7C74A>{cost} altın</color>";
            saveBtn.Label(missing.Count == 0 ? "Onayla" : "Al ve onayla").Restyle(missing.Count == 0 ? UI.Style.Primary : UI.Style.Gold);
        }
        void Ribbon(Transform p, string t)
        {
            var r = UI.R("bölüm", p); r.LE(-1, 24);
            if (UI.OrnLine != null) UI.Img(r, UI.OrnLine, Color.white, "süs").rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(280, 16));
            UI.Txt(r, t, 13, new Color(1f, 0.9f, 0.7f), UI.Serif, TextAnchor.MiddleCenter).Shadowed(0.7f).rectTransform.Fill(0, 1, 0, 0);
        }
        void SubTitle(Transform p, string t)
        {
            var r = UI.R("başlık", p); r.LE(-1, 16);
            UI.Txt(r, t, 10, UI.Lilac, UI.Bold, TextAnchor.MiddleCenter).rectTransform.Fill();
        }
        void IconRow(Transform p, Cat c)
        {
            var row = UI.R("ızgara", p); var g = row.gameObject.AddComponent<GridLayoutGroup>(); g.cellSize = new Vector2(46, 46); g.spacing = new Vector2(6, 6); g.childAlignment = TextAnchor.UpperCenter;
            g.constraint = GridLayoutGroup.Constraint.FixedColumnCount; g.constraintCount = 5;
            var items = new List<Item>(); if (Catalog.Optional(c) || c == Cat.Hero) items.Add(null); items.AddRange(Catalog.Of(c));
            int rows = (items.Count + 4) / 5; row.LE(-1, rows * 46 + (rows - 1) * 6);
            string cur = Catalog.Get(draft, c);
            foreach (var it in items)
            {
                var i2 = it; bool sel = it == null ? string.IsNullOrEmpty(cur) : cur == it.id;
                var b = UI.Panel(row, "seçenek", sel ? UI.A(UI.Purple, 0.32f) : new Color(1, 1, 1, 0.06f), false, UI.RoundSmall);
                if (it == null)
                {
                    var x = UI.Img(b, UI.Ring, new Color(1, 1, 1, 0.25f), "yok"); x.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(26, 26));
                    UI.Txt(b, c == Cat.Hero ? "Klasik" : "Yok", 8, UI.Muted, UI.Bold, TextAnchor.MiddleCenter).rectTransform.Fill();
                }
                else
                {
                    var rc = Catalog.RarityColor(it.rarity);
                    UI.Img(b, UI.Pill, UI.A(rc, 0.9f), "nadirlik").rectTransform.At(new Vector2(0.5f, 0), new Vector2(0, 2), new Vector2(18, 2), new Vector2(0.5f, 0));
                    var ic = UI.Icon(b, it.Icon(), 36); ic.rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(0, 1), new Vector2(36, 36));
                    if (it.cat == Cat.Frame) UI.Img(b, UI.Ring, it.tint, "çerçeve").rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(38, 38));
                    if (!Store.P.Owns(it.id))
                    {
                        var lk = UI.Img(b, UI.Pill, new Color(0.08f, 0.07f, 0.04f, 0.85f), "fiyat"); lk.rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, 1), new Vector2(40, 13), new Vector2(0.5f, 1));
                        UI.Txt(lk.rectTransform, it.price.ToString(), 8, UI.Gold, UI.Bold, TextAnchor.MiddleCenter).rectTransform.Fill();
                    }
                }
                if (sel) UI.Img(b, UI.Outline, UI.Lilac, "seçili").rectTransform.Fill(-1, -1, -1, -1);
                UI.Click(b, () =>
                {
                    Catalog.Set(draft, c, i2 == null ? "" : i2.id);
                    if (c == Cat.Costume && i2 != null) Toast("Kostüm giyildi: üst, alt, saç ve şapka gizlenir");
                    Studio.I.Cheer(c == Cat.Victory && i2 != null ? 1 : 0);
                    if (i2 != null) selT.text = i2.name + (Store.P.Owns(i2.id) ? "" : " · <color=#F7C74A>" + i2.price + " altın</color>");
                    Sfx.Play("select", 0.6f); RefreshCharacter();
                    if (i2 != null) selT.text = i2.name + " · " + i2.rarity + (Store.P.Owns(i2.id) ? "" : " · <color=#F7C74A>" + i2.price + " altın</color>");
                }, null);
            }
        }
        void Swatches(Transform p, string label, Color[] pal, Func<int> get, Action<int> set, bool skin)
        {
            var row = UI.R("renkler", p); row.LE(-1, 26); UI.HRow(row, 5, TextAnchor.MiddleCenter);
            UI.Txt(row, label, 9, UI.Muted, UI.Bold, TextAnchor.MiddleRight).rectTransform.LE(64, 20);
            for (int k = 0; k < pal.Length; k++)
            {
                int kk = k; var sw = UI.R("renk", row); sw.LE(22, 22);
                UI.Img(sw, UI.Circle, !skin && pal[k] == Color.white ? new Color(0.85f, 0.85f, 0.88f) : pal[k], "dolgu").rectTransform.Fill(3, 3, 3, 3);
                if (get() == k) UI.Img(sw, UI.Ring, UI.Fg, "seçili").rectTransform.Fill();
                UI.Click(sw, () => { set(kk); Sfx.Play("select", 0.5f, 1.2f); RefreshCharacter(); }, null);
            }
        }
        static List<Item> MissingItems(Look l)
        {
            var list = new List<Item>();
            foreach (Cat c in Enum.GetValues(typeof(Cat))) { var id = Catalog.Get(l, c); if (!string.IsNullOrEmpty(id) && !Store.P.Owns(id)) { var it = Catalog.Get(id); if (it != null) list.Add(it); } }
            return list;
        }
        void SaveLook()
        {
            var missing = MissingItems(draft); int cost = missing.Sum(m => m.price);
            if (missing.Count > 0)
            {
                if (Store.P.coins < cost) { Sfx.Play("lose", 0.5f); Toast($"Yetersiz altın: {cost - Store.P.coins} altın daha gerekiyor"); return; }
                foreach (var m in missing) Store.Buy(m);
                Sfx.Play("magic");
            }
            Store.Equip(draft); Sfx.Play("fanfare", 0.7f); Toast("Görünüm kaydedildi · lobide ve oyunda böyle görüneceksin");
            Tween.Scale((RectTransform)show.transform, 1.06f, 1, 0.4f);
            var me = slots.FirstOrDefault(x => x.me);
            if (me != null) { var i = slots.IndexOf(me); if (me.rig != null) Destroy(me.rig.gameObject); me.look = draft.Clone(); me.rig = Rig.Make(me.look, 0.96f); me.rig.transform.position = SeatPos(i, 9); me.rig.transform.LookAt(Vector3.zero); }
            RefreshCharacter();
        }

        // tek ürün kartı (özelleştirme ve mağaza)
        RectTransform ItemCard(Transform p, Item it, bool selected, Action onClick, bool shop = false)
        {
            var rc = it != null ? Catalog.RarityColor(it.rarity) : new Color(1, 1, 1, 0.3f);
            var r = UI.Panel(p, "ürün", new Color(1, 1, 1, 0.05f), false, UI.RoundSmall);
            var g = UI.Img(r, UI.Grad, UI.A(rc, 0.22f), "nadirlik"); g.rectTransform.Fill(1, 1, 1, 1); g.rectTransform.localScale = new Vector3(1, -1, 1);
            var strip = UI.Img(r, UI.Pill, UI.A(rc, 0.9f), "şerit"); strip.rectTransform.At(new Vector2(0.5f, 0), new Vector2(0, 3), new Vector2(30, 3), new Vector2(0.5f, 0));
            if (it == null)
            {
                UI.Txt(r, cat == Cat.Hero ? "Klasik" : "Yok", 12, UI.Muted, UI.Bold, TextAnchor.MiddleCenter).rectTransform.Fill(0, 18, 0, 0);
                var x = UI.Img(r, UI.Ring, new Color(1, 1, 1, 0.2f), "yok"); x.rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(0, 8), new Vector2(40, 40));
            }
            else
            {
                var ic = UI.Icon(r, it.Icon(), 50); ic.rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, -8), new Vector2(50, 50), new Vector2(0.5f, 1));
                if (it.cat == Cat.Frame) { var fr = UI.Img(r, UI.Ring, it.tint, "çerçeve"); fr.rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, -6), new Vector2(54, 54), new Vector2(0.5f, 1)); }
                var nm = UI.Txt(r, it.name, 10, null, UI.Bold, TextAnchor.MiddleCenter); nm.rectTransform.At(new Vector2(0.5f, 0), new Vector2(0, 22), new Vector2(78, 16), new Vector2(0.5f, 0)); nm.resizeTextForBestFit = true; nm.resizeTextMinSize = 8; nm.resizeTextMaxSize = 10;
                bool own = Store.P.Owns(it.id);
                var pr = UI.R("fiyat", r).At(new Vector2(0.5f, 0), new Vector2(0, 7), new Vector2(78, 14), new Vector2(0.5f, 0)); UI.HRow(pr, 3, TextAnchor.MiddleCenter);
                if (own) UI.Txt(pr, it.Free ? "Ücretsiz" : "Sende", 9, UI.Good, UI.Bold, TextAnchor.MiddleCenter).rectTransform.LE(-1, 14);
                else { UI.Icon(pr, Hud.Emoji("coin"), 11).rectTransform.LE(11, 11); UI.Txt(pr, it.price.ToString(), 10, UI.Gold, UI.Bold, TextAnchor.MiddleCenter).rectTransform.LE(-1, 14); }
            }
            if (selected) { var o = UI.Img(r, UI.Outline, UI.Lilac, "seçili"); o.rectTransform.Fill(-1, -1, -1, -1); var ck = UI.Img(r, UI.Circle, UI.Purple, "tik"); ck.rectTransform.At(new Vector2(1, 1), new Vector2(-4, -4), new Vector2(16, 16)); UI.Icon(ck.rectTransform, Hud.Emoji("check"), 10).rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(10, 10)); }
            UI.Click(r, onClick, null);
            return r;
        }

        // ================================================================ MAĞAZA
        bool invTab; Cat? shopCat; RectTransform shopGrid, shopFilters, featured; UI.Btn tabShop, tabInv;
        static readonly Cat[] SHOP_CATS = { Cat.Hero, Cat.Costume, Cat.Hat, Cat.Mask, Cat.Head, Cat.Glasses, Cat.Top, Cat.Bottom, Cat.Shoes, Cat.Face, Cat.Hands, Cat.Ears, Cat.Effect, Cat.Victory, Cat.Frame, Cat.Title };
        RectTransform BuildShop()
        {
            var s = UI.R("mağaza", content).Fill();
            // öne çıkan
            featured = UI.Panel(s, "öne çıkan").At(new Vector2(0, 0.5f), Vector2.zero, new Vector2(200, 324), new Vector2(0, 0.5f));
            featured.anchorMin = new Vector2(0, 0); featured.anchorMax = new Vector2(0, 1); featured.sizeDelta = new Vector2(200, 0);
            featured = UI.R("içerik", featured).Fill();
            var pan = UI.Panel(s, "vitrin"); pan.anchorMin = Vector2.zero; pan.anchorMax = Vector2.one; pan.offsetMin = new Vector2(210, 0); pan.offsetMax = Vector2.zero;
            var tabs = UI.R("sekme", pan).At(new Vector2(0, 1), new Vector2(10, -8), new Vector2(200, 30), new Vector2(0, 1)); UI.HRow(tabs, 6, TextAnchor.MiddleLeft, null, true);
            tabShop = UI.Chip(tabs, "Mağaza", true, () => { invTab = false; RefreshShop(); }, Hud.Emoji("pouch"));
            tabInv = UI.Chip(tabs, "Envanter", false, () => { invTab = true; RefreshShop(); }, Hud.Emoji("gift"));
            var fs = UI.Scroll(pan, false, out shopFilters, 6); var fr = (RectTransform)fs.transform; fr.anchorMin = new Vector2(0, 1); fr.anchorMax = new Vector2(1, 1); fr.pivot = new Vector2(0.5f, 1); fr.offsetMin = new Vector2(10, -72); fr.offsetMax = new Vector2(-10, -42);
            var gs = UI.Scroll(pan, true, out shopGrid, 8); ((RectTransform)gs.transform).Fill(10, 10, 10, 78);
            UI.Grid(shopGrid, new Vector2(92, 100), new Vector2(8, 8), 5);
            var note = UI.Txt(pan, "Tüm ürünler kozmetiktir · oyunda avantaj sağlamaz", 9, UI.Muted, null, TextAnchor.MiddleRight); note.rectTransform.At(new Vector2(1, 1), new Vector2(-12, -12), new Vector2(250, 20), new Vector2(1, 1));
            return s;
        }
        void RefreshShop()
        {
            tabShop.Restyle(!invTab ? UI.Style.Primary : UI.Style.Secondary); tabInv.Restyle(invTab ? UI.Style.Primary : UI.Style.Secondary);
            // öne çıkan ürün (her gün değişir)
            UI.Clear(featured);
            var pool = Catalog.ALL.Where(i => (i.rarity == "Efsanevi" || i.rarity == "Destansı") && Catalog.Usable(i)).ToList(); if (pool.Count == 0) pool = Catalog.ALL.Where(Catalog.Usable).ToList();
            var f = pool[DateTime.Now.DayOfYear % pool.Count];
            var fc = Catalog.RarityColor(f.rarity);
            var bg = UI.Img(featured, UI.Glow, UI.A(fc, 0.4f), "ışık"); bg.rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(220, 220), new Vector2(0.5f, 1));
            UI.Txt(featured, "GÜNÜN ÖNE ÇIKANI", 10, UI.Lilac, UI.Bold, TextAnchor.MiddleCenter).rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, -12), new Vector2(190, 16), new Vector2(0.5f, 1));
            var ic = UI.Icon(featured, f.Icon(), 110); ic.rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, -44), new Vector2(110, 110), new Vector2(0.5f, 1));
            Tween.Scale(ic.rectTransform, 0.8f, 1, 0.5f);
            UI.Txt(featured, f.name, 20, null, UI.Display, TextAnchor.MiddleCenter).rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, -160), new Vector2(190, 24), new Vector2(0.5f, 1));
            var rb = UI.Badge(featured, f.rarity + " · " + Catalog.CAT_NAME[f.cat], UI.A(fc, 0.2f), fc, 10); rb.At(new Vector2(0.5f, 1), new Vector2(0, -188), new Vector2(150, 20), new Vector2(0.5f, 1));
            var buy = UI.Button(featured, Store.P.Owns(f.id) ? "Sende · İncele" : f.price + " altın", Store.P.Owns(f.id) ? UI.Style.Secondary : UI.Style.Gold, () => OpenProduct(f), Store.P.Owns(f.id) ? null : Hud.Emoji("coin"), 13);
            buy.r.At(new Vector2(0.5f, 0), new Vector2(0, 14), new Vector2(170, 36), new Vector2(0.5f, 0));
            // filtreler
            UI.Clear(shopFilters);
            UI.Chip(shopFilters, "Tümü", shopCat == null, () => { shopCat = null; RefreshShop(); }).r.LE(-1, 26);
            foreach (var c in SHOP_CATS) { if (!Catalog.UsableCat(c)) continue; var cc = c; UI.Chip(shopFilters, Catalog.CAT_NAME[c], shopCat == c, () => { shopCat = cc; RefreshShop(); }).r.LE(-1, 26); }
            UI.Clear(shopGrid);
            var items = Catalog.ALL.Where(i => Catalog.Usable(i) && (shopCat == null || i.cat == shopCat) && (invTab ? Store.P.Owns(i.id) : !i.Free)).OrderBy(i => Store.P.Owns(i.id)).ThenBy(i => i.price).ToList();
            if (items.Count == 0) { var e = UI.Txt(shopGrid, invTab ? "Bu kategoride ürünün yok" : "Ürün yok", 12, UI.Muted, null, TextAnchor.MiddleCenter); e.rectTransform.LE(400, 60); }
            int n = 0;
            foreach (var it in items)
            {
                var i2 = it; bool equipped = Catalog.Get(Store.P.look, it.cat) == it.id;
                var card = ItemCard(shopGrid, it, invTab && equipped, () => OpenProduct(i2), true);
                Tween.Scale(card, 0.85f, 1, 0.3f, Mathf.Min(0.4f, n++ * 0.025f));
            }
        }
        // ürün penceresi: önizleme + satın al / kullan
        RectTransform modal;
        void OpenProduct(Item it)
        {
            if (modal != null) Destroy(modal.gameObject);
            modal = UI.R("pencere", root).Fill(-200, -200, -200, -200);
            var dim = UI.Img(modal, null, new Color(0, 0, 0, 0.6f), "karart"); dim.rectTransform.Fill(); dim.raycastTarget = true;
            UI.Click(dim.rectTransform, CloseModal, null);
            var cg = modal.gameObject.AddComponent<CanvasGroup>(); Tween.Fade(cg, 0, 1, 0.2f);
            var card = UI.Panel(modal, "kart", UI.A(UI.Anthracite, 0.97f)).At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520, 300)); Tween.Scale(card, 0.88f, 1, 0.32f);
            var rc = Catalog.RarityColor(it.rarity);
            var left = UI.R("önizleme", card).At(new Vector2(0, 0.5f), new Vector2(10, 0), new Vector2(220, 280), new Vector2(0, 0.5f));
            UI.Img(left, UI.Glow, UI.A(rc, 0.45f), "ışık").rectTransform.Fill(-20, 0, -20, 0);
            var preview = Store.P.look.Clone(); bool wearable = it.cat != Cat.Title && it.cat != Cat.Frame;
            Catalog.Set(preview, it.cat, it.id);
            if (wearable)
            {
                var raw = UI.R("karakter", left).Fill(); var ri = raw.gameObject.AddComponent<RawImage>(); ri.texture = Studio.I.Showcase(preview, 352, 448); raw.gameObject.AddComponent<DragRotate>();
                Studio.I.Cheer(it.cat == Cat.Victory ? 1 : 0);
            }
            else { var ic = UI.Icon(left, it.Icon(), 120); ic.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(120, 120)); if (it.cat == Cat.Frame) UI.Img(left, UI.Ring, it.tint, "çerçeve").rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(140, 140)); }
            var right = UI.R("bilgi", card).Fill(244, 16, 18, 16);
            UI.Badge(right, (it.rarity == "Efsanevi" ? "EFSANEVİ" : it.rarity == "Destansı" ? "DESTANSI" : it.rarity == "Nadir" ? "NADİR" : "SIRADAN"), UI.A(rc, 0.18f), rc, 10).At(new Vector2(0, 1), Vector2.zero, new Vector2(80, 20), new Vector2(0, 1));
            UI.Txt(right, it.name, 28, null, UI.Display).rectTransform.At(new Vector2(0, 1), new Vector2(0, -26), new Vector2(260, 34), new Vector2(0, 1));
            UI.Txt(right, Catalog.CAT_NAME[it.cat], 12, UI.Muted, UI.Bold).rectTransform.At(new Vector2(0, 1), new Vector2(0, -60), new Vector2(260, 18), new Vector2(0, 1));
            string desc = it.cat == Cat.Effect ? "Karakterinin çevresinde süzülen parıltılar. Lobide ve oyunda görünür." : it.cat == Cat.Victory ? "Maç kazandığında karakterin bu pozu yapar." : it.cat == Cat.Frame ? "Portrenin çevresindeki çerçeve; lobide, oyunda ve profilinde görünür." : it.cat == Cat.Title ? "Adının altında görünen unvan." : "Karakterinin görünümünü değiştirir. Herkes seni böyle görür.";
            UI.Txt(right, desc + "\n<color=#9A9CB0>Kozmetiktir, oyun dengesini etkilemez.</color>", 12, UI.Fg).rectTransform.At(new Vector2(0, 1), new Vector2(0, -86), new Vector2(256, 80), new Vector2(0, 1));
            var brow = UI.R("düğmeler", right).At(new Vector2(0, 0), Vector2.zero, new Vector2(256, 38), new Vector2(0, 0)); UI.HRow(brow, 8, TextAnchor.MiddleLeft, null, true);
            UI.Button(brow, "Kapat", UI.Style.Ghost, CloseModal).r.LE(-1, 38, 0.7f);
            bool own = Store.P.Owns(it.id); bool eq = Catalog.Get(Store.P.look, it.cat) == it.id;
            if (!own)
                UI.Button(brow, it.price + " · Satın al", UI.Style.Gold, () =>
                {
                    if (Store.P.coins < it.price) { Sfx.Play("lose", 0.5f); Toast($"Yetersiz altın: {it.price - Store.P.coins} altın daha gerekiyor"); return; }
                    Store.Buy(it); Sfx.Play("magic"); W.Burst(new Vector3(0, 1.5f, 0), UI.Gold, 30, 1.2f, 0.12f, 1.2f);
                    Toast(it.name + " envanterine eklendi"); OpenProduct(it); RefreshShop();
                }, Hud.Emoji("coin"), 14, null).r.LE(-1, 38, 1.3f);
            else
                UI.Button(brow, eq ? "Kullanılıyor" : "Kullan", eq ? UI.Style.Secondary : UI.Style.Primary, () =>
                {
                    if (eq) return; var l = Store.P.look.Clone(); Catalog.Set(l, it.cat, it.id); Store.Equip(l); Sfx.Play("select"); Toast(it.name + " kullanılıyor"); OpenProduct(it); RefreshShop();
                }, Hud.Emoji("check"), 14).r.LE(-1, 38, 1.3f);
            var coins = UI.Txt(right, "Altının: <color=#F7C74A>" + Store.P.coins + "</color>", 11, UI.Muted, UI.Bold, TextAnchor.MiddleRight); coins.rectTransform.At(new Vector2(1, 0), new Vector2(0, 44), new Vector2(200, 16), new Vector2(1, 0));
        }
        void CloseModal() { if (modal == null) return; var m = modal; modal = null; var cg = m.GetComponent<CanvasGroup>(); Studio.I.HideShowcase(); Tween.Fade(cg, 1, 0, 0.15f, 0, () => { if (m) Destroy(m.gameObject); }); if (current == "karakter") OpenCharacter(); }

        // ================================================================ GÖREVLER
        string qTab = "gunluk"; RectTransform qList, season; UI.Btn qd, qw, qa;
        static readonly (int level, string item)[] SEASON_CLASSIC = { (2, "fr_ice"), (4, "vic_clap"), (6, "head_cat"), (8, "fx_hearts"), (11, "mask_gold"), (15, "fx_stars"), (20, "t_legend") };
        static readonly (int level, string item)[] SEASON_HERO = { (2, "fr_ice"), (4, "vic_clap"), (6, "hero_m8"), (8, "fx_hearts"), (11, "hero_f13"), (15, "fx_stars"), (20, "t_legend") };
        public static (int level, string item)[] SEASON => Rig.SyntyAvailable ? SEASON_HERO : SEASON_CLASSIC;
        static int SeasonClaimable() => Store.P == null ? 0 : SEASON.Count(s => Store.P.level >= s.level && !Store.P.owned.Contains(s.item));
        RectTransform BuildQuests()
        {
            var s = UI.R("görevler", content).Fill();
            season = UI.Panel(s, "sezon"); season.anchorMin = new Vector2(0, 1); season.anchorMax = Vector2.one; season.pivot = new Vector2(0.5f, 1); season.offsetMin = new Vector2(0, -112); season.offsetMax = Vector2.zero;
            season = UI.R("içerik", season).Fill();
            var pan = UI.Panel(s, "liste"); pan.anchorMin = Vector2.zero; pan.anchorMax = Vector2.one; pan.offsetMin = Vector2.zero; pan.offsetMax = new Vector2(0, -120);
            var tabs = UI.R("sekme", pan).At(new Vector2(0, 1), new Vector2(10, -8), new Vector2(330, 28), new Vector2(0, 1)); UI.HRow(tabs, 6, TextAnchor.MiddleLeft, null, true);
            qd = UI.Chip(tabs, "Günlük", true, () => { qTab = "gunluk"; RefreshQuests(); }, Hud.Emoji("sun"));
            qw = UI.Chip(tabs, "Haftalık", false, () => { qTab = "haftalik"; RefreshQuests(); }, Hud.Emoji("hourglass"));
            qa = UI.Chip(tabs, "Başarımlar", false, () => { qTab = "basarim"; RefreshQuests(); }, Hud.Emoji("trophy"));
            var sc = UI.Scroll(pan, true, out qList, 6); ((RectTransform)sc.transform).Fill(10, 8, 10, 44);
            return s;
        }
        void RefreshQuests()
        {
            qd.Restyle(qTab == "gunluk" ? UI.Style.Primary : UI.Style.Secondary); qw.Restyle(qTab == "haftalik" ? UI.Style.Primary : UI.Style.Secondary); qa.Restyle(qTab == "basarim" ? UI.Style.Primary : UI.Style.Secondary);
            // sezon yolu
            UI.Clear(season); var p = Store.P;
            UI.Txt(season, "SEZON 1 · GECE YARISI", 11, UI.Lilac, UI.Bold).rectTransform.At(new Vector2(0, 1), new Vector2(14, -8), new Vector2(240, 16), new Vector2(0, 1));
            UI.Txt(season, $"Seviye {p.level} · {p.xp}/{Profile.XpFor(p.level)} XP", 10, UI.Muted, null, TextAnchor.MiddleRight).rectTransform.At(new Vector2(1, 1), new Vector2(-14, -8), new Vector2(240, 16), new Vector2(1, 1));
            var track = UI.R("yol", season).Fill(24, 10, 24, 30);
            int maxL = SEASON[SEASON.Length - 1].level;
            var line = UI.ProgressBar(track, UI.Purple, 4); line.r.anchorMin = new Vector2(0, 0.5f); line.r.anchorMax = new Vector2(1, 0.5f); line.r.sizeDelta = new Vector2(0, 4); line.r.anchoredPosition = new Vector2(0, 6);
            line.Set(Mathf.Clamp01((p.level - 1 + p.xp / (float)Profile.XpFor(p.level)) / (maxL - 1)));
            foreach (var st in SEASON)
            {
                var it = Catalog.Get(st.item); float x = (st.level - 1) / (float)(maxL - 1);
                bool reached = p.level >= st.level, got = p.owned.Contains(st.item);
                var node = UI.R("ödül", track); node.anchorMin = node.anchorMax = new Vector2(x, 0.5f); node.sizeDelta = new Vector2(46, 46); node.anchoredPosition = new Vector2(0, 6);
                var bgc = UI.Img(node, UI.Circle, got ? UI.A(UI.Good, 0.35f) : reached ? UI.A(UI.Gold, 0.4f) : new Color(0.14f, 0.14f, 0.2f), "zemin"); bgc.rectTransform.Fill();
                UI.Img(node, UI.Ring, got ? UI.Good : reached ? UI.Gold : new Color(1, 1, 1, 0.15f), "halka").rectTransform.Fill();
                var ic = UI.Icon(node, it.Icon(), 28); ic.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(28, 28)); if (!reached) ic.color = new Color(1, 1, 1, 0.45f);
                UI.Txt(node, "Sv." + st.level, 9, reached ? UI.Fg : UI.Muted, UI.Bold, TextAnchor.MiddleCenter).rectTransform.At(new Vector2(0.5f, 0), new Vector2(0, -14), new Vector2(50, 12), new Vector2(0.5f, 0.5f));
                if (reached && !got)
                {
                    Tween.Run(node, 1.2f, k => { if (node) node.localScale = Vector3.one * (1 + Mathf.Sin(k * Mathf.PI * 2) * 0.06f); }, k => k);
                    var s2 = st; UI.Click(node, () => { Store.P.owned.Add(s2.item); Store.Save(); Sfx.Play("fanfare"); Toast("Sezon ödülü: " + Catalog.Get(s2.item).name); RefreshQuests(); });
                }
                else if (!reached) UI.Icon(node, Hud.Emoji("lock"), 12).rectTransform.At(new Vector2(1, 0), new Vector2(2, -2), new Vector2(14, 14), new Vector2(1, 0));
            }
            // liste
            UI.Clear(qList);
            if (qTab == "basarim")
            {
                foreach (var a in Store.ACH)
                {
                    bool done = p.achieved.Contains(a.id); int prog = Mathf.Min(p.Stat(a.stat), a.goal);
                    QuestRow(qList, a.icon, a.name, a.desc, prog, a.goal, a.reward != null ? Catalog.Get(a.reward)?.name : null, 0, 0, done ? "done" : "", null);
                }
            }
            else
            {
                foreach (var q in qTab == "gunluk" ? Store.DAILY : Store.WEEKLY)
                {
                    var st = Store.QState(q.id); int prog = Store.Progress(q); bool claimed = st != null && st.claimed;
                    var q2 = q;
                    QuestRow(qList, q.icon, q.name, q.weekly ? "Haftalık görev" : "Günlük görev", prog, q.goal, null, q.xp, q.coins, claimed ? "claimed" : prog >= q.goal ? "ready" : "", () =>
                    {
                        if (Store.Claim(q2)) { Sfx.Play("win", 0.7f); Toast($"+{q2.coins} altın · +{q2.xp} XP"); RefreshQuests(); RefreshTop(); }
                    });
                }
            }
        }
        void QuestRow(Transform p, string icon, string name, string sub, int prog, int goal, string rewardItem, int xp, int coins, string state, Action claim)
        {
            var r = UI.Panel(p, "görev", state == "ready" ? UI.A(UI.Gold, 0.1f) : new Color(1, 1, 1, 0.04f), false, UI.RoundSmall); r.LE(-1, 50);
            var ib = UI.Img(r, UI.Circle, new Color(1, 1, 1, 0.07f), "ikon zemin"); ib.rectTransform.At(new Vector2(0, 0.5f), new Vector2(8, 0), new Vector2(36, 36), new Vector2(0, 0.5f));
            var ic = UI.Icon(ib.rectTransform, Hud.Emoji(icon) ?? Resources.Load<Sprite>("Items/" + icon), 22); ic.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(22, 22));
            if (state == "done" || state == "claimed") ic.color = Color.white; else if (prog == 0) ic.color = new Color(1, 1, 1, 0.6f);
            UI.Txt(r, name, 13, null, UI.Bold).rectTransform.At(new Vector2(0, 1), new Vector2(54, -5), new Vector2(260, 18), new Vector2(0, 1));
            UI.Txt(r, sub, 10, UI.Muted).rectTransform.At(new Vector2(0, 1), new Vector2(54, -22), new Vector2(200, 14), new Vector2(0, 1));
            var bar = UI.ProgressBar(r, state == "done" || state == "claimed" ? UI.Good : UI.Purple, 5); bar.r.At(new Vector2(0, 0), new Vector2(54, 8), new Vector2(170, 5), new Vector2(0, 0)); bar.Set(prog / (float)goal);
            UI.Txt(r, prog + "/" + goal, 10, UI.Muted, UI.Bold).rectTransform.At(new Vector2(0, 0), new Vector2(230, 3), new Vector2(50, 14), new Vector2(0, 0));
            var rw = UI.R("ödül", r).At(new Vector2(1, 0.5f), new Vector2(-110, 0), new Vector2(170, 24), new Vector2(1, 0.5f)); UI.HRow(rw, 5, TextAnchor.MiddleRight);
            if (xp > 0) UI.Badge(rw, "+" + xp + " XP", UI.A(UI.Purple, 0.2f), UI.Lilac, 10);
            if (coins > 0) UI.Badge(rw, "+" + coins + " altın", UI.A(UI.Gold, 0.16f), UI.Gold, 10);
            if (!string.IsNullOrEmpty(rewardItem)) UI.Badge(rw, "Ödül: " + rewardItem, UI.A(UI.Gold, 0.16f), UI.Gold, 10);
            var act = UI.R("eylem", r).At(new Vector2(1, 0.5f), new Vector2(-8, 0), new Vector2(92, 30), new Vector2(1, 0.5f));
            if (state == "ready") { var b = UI.Button(act, "Al", UI.Style.Gold, claim, Hud.Emoji("gift"), 13, null); b.r.Fill(); Tween.Run(b.r, 1.4f, k => { if (b.r) b.r.localScale = Vector3.one * (1 + Mathf.Sin(k * Mathf.PI * 2) * 0.04f); }, k => k); }
            else if (state == "claimed" || state == "done") { var t = UI.Txt(act, "Tamamlandı", 11, UI.Good, UI.Bold, TextAnchor.MiddleCenter); t.rectTransform.Fill(); }
            else { var t = UI.Txt(act, "Devam ediyor", 10, UI.Muted, null, TextAnchor.MiddleCenter); t.rectTransform.Fill(); }
        }

        // ================================================================ PROFİL
        RectTransform profLeft, profRight; RawImage profShow;
        RectTransform BuildProfile()
        {
            var s = UI.R("profil", content).Fill();
            profLeft = UI.Panel(s, "kart"); profLeft.anchorMin = Vector2.zero; profLeft.anchorMax = new Vector2(0, 1); profLeft.pivot = new Vector2(0, 0.5f); profLeft.sizeDelta = new Vector2(236, 0); profLeft.anchoredPosition = Vector2.zero;
            profLeft = UI.R("içerik", profLeft).Fill();
            profRight = UI.R("bilgiler", s); profRight.anchorMin = Vector2.zero; profRight.anchorMax = Vector2.one; profRight.offsetMin = new Vector2(246, 0); profRight.offsetMax = Vector2.zero;
            return s;
        }
        void RefreshProfile()
        {
            var p = Store.P; UI.Clear(profLeft); UI.Clear(profRight);
            // sol: vitrin + ad + seviye
            var fc = UI.FrameColor(p.look.frame);
            UI.Img(profLeft, UI.Glow, UI.A(Catalog.Get(p.look.frame) != null ? fc : UI.Purple, 0.4f), "ışık").rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, -6), new Vector2(240, 220), new Vector2(0.5f, 1));
            var raw = UI.R("karakter", profLeft).At(new Vector2(0.5f, 1), new Vector2(0, -4), new Vector2(170, 206), new Vector2(0.5f, 1));
            profShow = raw.gameObject.AddComponent<RawImage>(); profShow.texture = Studio.I.Showcase(p.look, 340, 412); raw.gameObject.AddComponent<DragRotate>();
            UI.Txt(profLeft, p.name, 26, null, UI.Display, TextAnchor.MiddleCenter).Shadowed().rectTransform.At(new Vector2(0.5f, 0), new Vector2(0, 92), new Vector2(220, 30), new Vector2(0.5f, 0));
            var ti = Catalog.Get(p.look.title);
            UI.Txt(profLeft, ti != null ? "« " + ti.name + " »" : "", 11, UI.Gold, UI.Bold, TextAnchor.MiddleCenter).rectTransform.At(new Vector2(0.5f, 0), new Vector2(0, 74), new Vector2(220, 16), new Vector2(0.5f, 0));
            var lv = UI.Img(profLeft, UI.Circle, UI.Purple, "seviye"); lv.rectTransform.At(new Vector2(0, 0), new Vector2(18, 26), new Vector2(38, 38), new Vector2(0, 0));
            UI.Img(lv.rectTransform, UI.Ring, UI.Lilac, "halka").rectTransform.Fill();
            UI.Txt(lv.rectTransform, p.level.ToString(), 16, null, UI.Title, TextAnchor.MiddleCenter).rectTransform.Fill(0, 2, 0, 0);
            UI.Txt(profLeft, "Seviye " + p.level, 11, null, UI.Bold).rectTransform.At(new Vector2(0, 0), new Vector2(64, 46), new Vector2(150, 16), new Vector2(0, 0));
            var xb = UI.ProgressBar(profLeft, UI.Purple, 7); xb.r.At(new Vector2(0, 0), new Vector2(64, 36), new Vector2(150, 7), new Vector2(0, 0)); xb.Set(0);
            float target = p.xp / (float)Profile.XpFor(p.level); Tween.Run(xb.r, 0.9f, k => xb.Set(target * k), Tween.OutCubic, 0.2f);
            UI.Txt(profLeft, $"{p.xp} / {Profile.XpFor(p.level)} XP", 9, UI.Muted).rectTransform.At(new Vector2(0, 0), new Vector2(64, 18), new Vector2(150, 14), new Vector2(0, 0));
            var edit = UI.Button(profLeft, "", UI.Style.Secondary, () => Show("karakter"), Hud.Emoji("tunic"), 13); edit.r.At(new Vector2(1, 1), new Vector2(-8, -8), new Vector2(36, 32), new Vector2(1, 1));
            // sağ: istatistik kutuları
            int games = p.Stat("games"), wins = p.Stat("wins"), losses = p.Stat("losses");
            var grid = UI.R("istatistik", profRight); grid.anchorMin = new Vector2(0, 1); grid.anchorMax = Vector2.one; grid.pivot = new Vector2(0.5f, 1); grid.offsetMin = new Vector2(0, -132); grid.offsetMax = Vector2.zero;
            var gl = grid.gameObject.AddComponent<GridLayoutGroup>(); gl.cellSize = new Vector2(160, 62); gl.spacing = new Vector2(8, 8); gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount; gl.constraintCount = 3;
            int n = 0;
            void Tile(string icon, string label, string value, Color c)
            {
                var t = UI.Panel(grid, "kutu", null, false, UI.RoundSmall);
                UI.Icon(t, Hud.Emoji(icon), 22).rectTransform.At(new Vector2(0, 0.5f), new Vector2(12, 0), new Vector2(22, 22), new Vector2(0, 0.5f));
                var v = UI.Txt(t, value, 20, c, UI.Title); v.rectTransform.At(new Vector2(0, 1), new Vector2(44, -6), new Vector2(110, 28), new Vector2(0, 1));
                UI.Txt(t, label, 10, UI.Muted, UI.Bold).rectTransform.At(new Vector2(0, 0), new Vector2(44, 7), new Vector2(110, 14), new Vector2(0, 0));
                Tween.Scale(t, 0.85f, 1, 0.3f, n++ * 0.04f);
            }
            Tile("swords", "Toplam maç", games.ToString(), UI.Fg);
            Tile("trophy", "Galibiyet", wins.ToString(), UI.Good);
            Tile("skull", "Mağlubiyet", losses.ToString(), UI.Bad);
            Tile("star", "Kazanma oranı", games > 0 ? "%" + Mathf.RoundToInt(100f * wins / games) : "—", UI.Lilac);
            Tile("medal", "MVP", p.Stat("mvp").ToString(), UI.Gold);
            Tile("shield", "Hayatta kalma", games > 0 ? "%" + Mathf.RoundToInt(100f * p.Stat("survived") / games) : "—", UI.Fg);
            // roller
            var roles = UI.Panel(profRight, "roller"); roles.anchorMin = new Vector2(0, 0); roles.anchorMax = new Vector2(0.5f, 1); roles.offsetMin = Vector2.zero; roles.offsetMax = new Vector2(-4, -140);
            UI.Txt(roles, "En çok oynanan roller", 12, null, UI.Bold).rectTransform.At(new Vector2(0, 1), new Vector2(12, -8), new Vector2(220, 18), new Vector2(0, 1));
            var top = Store.TopRoles(); int max = top.Count > 0 ? top[0].Value : 1;
            var names = new Dictionary<string, string> { { "vampir", "Vampir" }, { "koylu", "Köylü" }, { "kahin", "Kâhin" }, { "koruma", "Koruma" }, { "cadi", "Cadı" }, { "avci", "Avcı" }, { "melek", "Aşk Meleği" } };
            if (top.Count == 0) UI.Txt(roles, "Henüz maç oynamadın. İlk maçından sonra burada görünecek.", 11, UI.Muted).rectTransform.At(new Vector2(0, 1), new Vector2(12, -32), new Vector2(220, 40), new Vector2(0, 1));
            for (int i = 0; i < Mathf.Min(4, top.Count); i++)
            {
                float y = -34 - i * 30;
                UI.Icon(roles, Hud.Emoji(top[i].Key), 20).rectTransform.At(new Vector2(0, 1), new Vector2(12, y), new Vector2(20, 20), new Vector2(0, 1));
                UI.Txt(roles, names.TryGetValue(top[i].Key, out var nn) ? nn : top[i].Key, 11, null, UI.Bold).rectTransform.At(new Vector2(0, 1), new Vector2(38, y), new Vector2(90, 18), new Vector2(0, 1));
                var b = UI.ProgressBar(roles, UI.Purple, 6); b.r.At(new Vector2(0, 1), new Vector2(120, y - 7), new Vector2(70, 6), new Vector2(0, 1)); b.Set(top[i].Value / (float)max);
                UI.Txt(roles, top[i].Value.ToString(), 11, UI.Muted, UI.Bold, TextAnchor.MiddleRight).rectTransform.At(new Vector2(1, 1), new Vector2(-12, y), new Vector2(30, 18), new Vector2(1, 1));
            }
            // başarımlar ve son maçlar
            var ach = UI.Panel(profRight, "başarımlar"); ach.anchorMin = new Vector2(0.5f, 0); ach.anchorMax = new Vector2(1, 1); ach.offsetMin = new Vector2(4, 0); ach.offsetMax = new Vector2(0, -140);
            UI.Txt(ach, $"Başarımlar  <color=#9A9CB0>{p.achieved.Count}/{Store.ACH.Count}</color>", 12, null, UI.Bold).rectTransform.At(new Vector2(0, 1), new Vector2(12, -8), new Vector2(220, 18), new Vector2(0, 1));
            var arow = UI.R("rozetler", ach).At(new Vector2(0, 1), new Vector2(10, -32), new Vector2(230, 70), new Vector2(0, 1));
            var ag = arow.gameObject.AddComponent<GridLayoutGroup>(); ag.cellSize = new Vector2(30, 30); ag.spacing = new Vector2(6, 6); ag.constraint = GridLayoutGroup.Constraint.FixedColumnCount; ag.constraintCount = 6;
            foreach (var a in Store.ACH)
            {
                bool got = p.achieved.Contains(a.id);
                var c = UI.Img(arow, UI.Circle, got ? UI.A(UI.Gold, 0.25f) : new Color(1, 1, 1, 0.05f), "rozet");
                var ic = UI.Icon(c.rectTransform, Hud.Emoji(a.icon), 18); ic.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(18, 18)); if (!got) ic.color = new Color(1, 1, 1, 0.25f);
            }
            UI.Txt(ach, "Son maçlar", 11, UI.Muted, UI.Bold).rectTransform.At(new Vector2(0, 0), new Vector2(12, 34), new Vector2(150, 16), new Vector2(0, 0));
            var hrow = UI.R("geçmiş", ach).At(new Vector2(0, 0), new Vector2(10, 8), new Vector2(240, 22), new Vector2(0, 0)); UI.HRow(hrow, 4, TextAnchor.MiddleLeft);
            if (p.history.Count == 0) UI.Txt(hrow, "—", 11, UI.Muted).rectTransform.LE(20, 20);
            foreach (var h in p.history.Take(9))
            {
                bool w = h.StartsWith("W"); var c = UI.Img(hrow, UI.RoundSmall, w ? UI.A(UI.Good, 0.3f) : UI.A(UI.Bad, 0.25f), "maç"); c.rectTransform.LE(22, 22);
                UI.Txt(c.rectTransform, w ? "G" : "M", 10, w ? UI.Good : UI.Bad, UI.Bold, TextAnchor.MiddleCenter).rectTransform.Fill();
            }
        }
    }

    // vitrindeki karakteri parmakla döndürme
    public class DragRotate : MonoBehaviour, IDragHandler, IBeginDragHandler
    {
        public void OnBeginDrag(PointerEventData e) { }
        public void OnDrag(PointerEventData e) { if (Studio.I != null) Studio.I.yaw -= e.delta.x * 0.55f; }
    }
}
