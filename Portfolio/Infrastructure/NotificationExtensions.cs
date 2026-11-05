using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Portfolio.Infrastructure
{
    /// <summary>
    /// TempData üzerinden tek seferlik bildirim (toast) mesajları.
    /// Anahtarları tek yerde tutar: TempData["Sucess"] gibi yazım hataları derleme hatasına dönüşür.
    ///
    /// Kullanım (controller):  TempData.Success("Proje eklendi.");
    /// Gösterim: Areas/Admin/Views/Shared/_Alerts.cshtml -> wwwroot/js/admin-ui.js (SweetAlert2 toast)
    /// </summary>
    public static class NotificationExtensions
    {
        public const string SuccessKey = "Notify.Success";
        public const string ErrorKey = "Notify.Error";

        public static void Success(this ITempDataDictionary tempData, string message)
            => tempData[SuccessKey] = message;

        public static void Error(this ITempDataDictionary tempData, string message)
            => tempData[ErrorKey] = message;

        public static string? TakeSuccess(this ITempDataDictionary tempData)
            => tempData[SuccessKey] as string;   // indexer ile okumak mesajı "tüketir" (sonraki istekte silinir); TempData.Peek() ise silmeden okurdu

        public static string? TakeError(this ITempDataDictionary tempData)
            => tempData[ErrorKey] as string;
    }
}
