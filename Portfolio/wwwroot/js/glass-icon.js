/* Cam ikonları: statik PNG'yi, uygun tarayıcılarda alfa kanallı WebM'e yükseltir.
   - Safari VP9 alfa kanalını desteklemiyor -> PNG kalır.
   - Kullanıcı "azaltılmış hareket" istiyorsa -> PNG kalır.
   - Ekran dışındaki videolar duraklatılır (CPU/pil tasarrufu). */
(function () {
    "use strict";

    const reduceMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    const ua = navigator.userAgent;
    const isSafari = /^((?!chrome|chromium|crios|fxios|android|edg).)*safari/i.test(ua);
    const probe = document.createElement("video");
    const canWebm = probe.canPlayType('video/webm; codecs="vp9"') !== "";

    if (reduceMotion || isSafari || !canWebm) return;

    const observer = "IntersectionObserver" in window
        ? new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                const video = entry.target;
                if (entry.isIntersecting) {
                    video.play().catch(function () { /* otomatik oynatma engellendi: poster kalır */ });
                } else {
                    video.pause();
                }
            });
        }, { rootMargin: "80px" })
        : null;

    function upgrade(img) {
        const video = document.createElement("video");
        video.className = img.className;
        video.width = img.width;
        video.height = img.height;
        video.poster = img.src;
        video.muted = true;          // tarayıcılar sesi kapalı videoya otomatik oynatma izni verir
        video.loop = true;
        video.playsInline = true;
        video.preload = "metadata";
        video.setAttribute("aria-hidden", "true");
        video.setAttribute("disablepictureinpicture", "");
        video.src = img.dataset.glassVideo;

        // PNG'yi ancak video ilk kareyi çizebildiğinde değiştir: titreme olmasın.
        video.addEventListener("loadeddata", function () {
            img.replaceWith(video);
            if (observer) observer.observe(video); else video.play().catch(function () { });
        }, { once: true });

        video.load();
    }

    function init(root) {
        (root || document).querySelectorAll("img[data-glass-video]").forEach(upgrade);
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", function () { init(); });
    } else {
        init();
    }

    window.GlassIcons = { init: init };
})();
