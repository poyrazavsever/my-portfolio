/* Admin paneli etkileşimleri: silme onayı ve işlem sonucu bildirimi (SweetAlert2).
   Kademeli iyileştirme:
   - SweetAlert2 yüklenemezse silme onayı tarayıcının kendi confirm() penceresine düşer.
   - Bildirimler sunucuda düz bir uyarı kutusu olarak basılır; JS bunu toast'a çevirir. */
(function () {
    "use strict";

    const hasSwal = typeof window.Swal !== "undefined";

    // Ortak görünüm: SweetAlert2'nin kendi buton stillerini kapatıp tasarım sistemimizin sınıflarını veriyoruz.
    const dialog = hasSwal ? Swal.mixin({
        buttonsStyling: false,
        reverseButtons: true,
        focusCancel: true,          // yanlışlıkla Enter'a basılırsa silme değil vazgeç seçilsin
        showClass: { popup: "pa-swal-in" },
        hideClass: { popup: "pa-swal-out" },
        customClass: {
            popup: "pa-swal",
            title: "pa-swal-title",
            htmlContainer: "pa-swal-text",
            actions: "pa-swal-actions",
            confirmButton: "btn btn-primary",
            cancelButton: "btn btn-ghost"
        }
    }) : null;

    const toast = hasSwal ? Swal.mixin({
        toast: true,
        position: "bottom-end",
        showConfirmButton: false,
        timer: 3600,
        timerProgressBar: true,
        showClass: { popup: "pa-swal-in" },
        hideClass: { popup: "pa-swal-out" },
        customClass: { popup: "pa-toast", timerProgressBar: "pa-toast-bar" },
        didOpen: function (el) {
            el.addEventListener("mouseenter", Swal.stopTimer);
            el.addEventListener("mouseleave", Swal.resumeTimer);
        }
    }) : null;

    /* ---------- 1. Silme onayı ----------
       <form data-confirm="Açıklama" data-confirm-title="Başlık" data-confirm-button="Sil"> */
    document.addEventListener("submit", function (event) {
        const form = event.target;
        if (!(form instanceof HTMLFormElement) || !form.hasAttribute("data-confirm")) return;
        if (form.dataset.confirmed === "true") return; // onaylandıktan sonraki gerçek gönderim

        event.preventDefault();
        const text = form.dataset.confirm;

        if (!dialog) {
            if (window.confirm(text)) submitConfirmed(form);
            return;
        }

        dialog.fire({
            title: form.dataset.confirmTitle || "Emin misiniz?",
            text: text,
            showCancelButton: true,
            confirmButtonText: form.dataset.confirmButton || "Sil",
            cancelButtonText: "Vazgeç"
        }).then(function (result) {
            if (result.isConfirmed) submitConfirmed(form);
        });
    });

    function submitConfirmed(form) {
        form.dataset.confirmed = "true";
        const button = form.querySelector("button[type=submit]");
        if (button) button.disabled = true; // çift tıklamayla iki kez gönderilmesin
        form.submit();
    }

    /* ---------- 2. İşlem sonucu bildirimi ----------
       _Alerts.cshtml: <div class="pa-alert" data-flash="success|error">Mesaj</div> */
    function showFlash() {
        if (!toast) return; // JS/SweetAlert yoksa sunucunun bastığı uyarı kutusu görünür kalır

        document.querySelectorAll("[data-flash]").forEach(function (alert, index) {
            const isError = alert.dataset.flash === "error";
            const message = alert.textContent.trim();
            alert.remove();

            setTimeout(function () {
                toast.fire({
                    html: '<i class="bi ' + (isError ? "bi-exclamation-octagon" : "bi-check2-circle") + '"></i><span></span>',
                    customClass: { popup: "pa-toast " + (isError ? "is-error" : "is-ok"), timerProgressBar: "pa-toast-bar" },
                    timer: isError ? 6000 : 3600,  // hata mesajı okunacak kadar kalsın
                    didRender: function (popup) {
                        // Mesajı textContent ile yaz: kullanıcı verisi (ör. kategori adı) HTML olarak yorumlanmasın.
                        popup.querySelector(".swal2-html-container span").textContent = message;
                    }
                });
            }, index * 250);
        });
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", showFlash);
    } else {
        showFlash();
    }
})();
