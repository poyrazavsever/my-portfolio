/* Public portfolyo sayfası etkileşimleri.
   Hepsi kademeli iyileştirme: JS olmadan da bütün içerik görünür ve linkler çalışır. */
(function () {
    "use strict";

    /* ---------- 1. Navbar: kaydırınca belirginleşsin, aktif bölümü işaretlesin ---------- */
    const nav = document.querySelector("[data-site-nav]");
    if (nav) {
        const onScroll = function () { nav.classList.toggle("is-scrolled", window.scrollY > 24); };
        onScroll();
        window.addEventListener("scroll", onScroll, { passive: true });

        // Mobil menüde bir linke tıklanınca menüyü kapat.
        const collapse = document.getElementById("siteNavLinks");
        nav.querySelectorAll("[data-nav-link]").forEach(function (link) {
            link.addEventListener("click", function () {
                if (collapse && collapse.classList.contains("show") && window.bootstrap) {
                    bootstrap.Collapse.getOrCreateInstance(collapse).hide();
                }
            });
        });

        // Scrollspy: ekranın üst-orta bandına giren bölümün linkini aktif yap.
        const links = new Map();
        nav.querySelectorAll("[data-nav-link]").forEach(function (link) {
            const id = link.getAttribute("href").split("#")[1];
            if (id) links.set(id, link);
        });

        if ("IntersectionObserver" in window && links.size > 0) {
            const spy = new IntersectionObserver(function (entries) {
                entries.forEach(function (entry) {
                    if (!entry.isIntersecting) return;
                    links.forEach(function (link) { link.classList.remove("is-active"); link.removeAttribute("aria-current"); });
                    const active = links.get(entry.target.id);
                    if (active) { active.classList.add("is-active"); active.setAttribute("aria-current", "true"); }
                });
            }, { rootMargin: "-40% 0px -55% 0px" });

            links.forEach(function (_, id) {
                const section = document.getElementById(id);
                if (section) spy.observe(section);
            });
        }
    }

    /* ---------- 2. Projeler: kategori filtresi + arama ---------- */
    const toolbar = document.querySelector("[data-project-toolbar]");
    if (toolbar) {
        const cards = Array.from(document.querySelectorAll("[data-project-grid] > [data-category]"));
        const chips = Array.from(toolbar.querySelectorAll("[data-filter]"));
        const search = toolbar.querySelector("[data-project-search]");
        const empty = document.querySelector("[data-project-empty]");
        const reset = document.querySelector("[data-project-reset]");
        let activeFilter = "all";

        // Türkçe "İ/ı" harfleri için yerel küçük harfe çevirme: "İLETİŞİM" -> "iletişim".
        const normalize = function (text) { return (text || "").toLocaleLowerCase("tr-TR").trim(); };
        cards.forEach(function (card) { card.dataset.searchNormalized = normalize(card.dataset.search); });

        const apply = function () {
            const query = normalize(search ? search.value : "");
            let visible = 0;

            cards.forEach(function (card) {
                const matchesFilter = activeFilter === "all" || card.dataset.category === activeFilter;
                const matchesSearch = query === "" || card.dataset.searchNormalized.includes(query);
                const show = matchesFilter && matchesSearch;
                card.hidden = !show;
                if (show) visible++;
            });

            if (empty) empty.hidden = visible > 0;
        };

        chips.forEach(function (chip) {
            chip.addEventListener("click", function () {
                activeFilter = chip.dataset.filter;
                chips.forEach(function (other) {
                    const on = other === chip;
                    other.classList.toggle("is-active", on);
                    other.setAttribute("aria-pressed", on ? "true" : "false");
                });
                apply();
            });
        });

        if (search) search.addEventListener("input", apply);

        if (reset) {
            reset.addEventListener("click", function () {
                if (search) search.value = "";
                chips[0].click();
            });
        }
    }

    /* ---------- 3. İletişim formu: sunucu hata döndürdüyse ilk hatalı alana odaklan ----------
       Bölüme kaydırmayı formun "#contact" fragment'ı yapıyor; burada sadece klavye odağını taşıyoruz. */
    // "load" sonrası: tarayıcının fragment'e atlaması odağı sıfırlayabildiği için ondan sonra odaklanıyoruz.
    window.addEventListener("load", function () {
        const firstInvalid = document.querySelector("[data-contact-form] .input-validation-error");
        if (firstInvalid) firstInvalid.focus({ preventScroll: true });

        document.documentElement.classList.add("is-ready");
    });
})();
