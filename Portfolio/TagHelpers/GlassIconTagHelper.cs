using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Portfolio.TagHelpers
{
    /// <summary>
    /// Hareketli cam ikonu basar:  &lt;glass-icon name="projects" size="64" /&gt;
    ///
    /// Sunucu her zaman şeffaf PNG üretir (her tarayıcıda çalışır).
    /// wwwroot/js/glass-icon.js, tarayıcı destekliyorsa ve kullanıcı azaltılmış
    /// hareket istemiyorsa bunu alfa kanallı WebM videoya yükseltir.
    /// Buna "progressive enhancement" denir: temel deneyim garanti, ekstrası opsiyonel.
    /// </summary>
    [HtmlTargetElement("glass-icon", TagStructure = TagStructure.WithoutEndTag)]
    public class GlassIconTagHelper : TagHelper
    {
        private const string IconPath = "/assets/icons/";

        /// <summary>wwwroot/assets/icons altındaki dosya adı (uzantısız), örn. "projects".</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Piksel cinsinden kenar uzunluğu.</summary>
        public int Size { get; set; } = 48;

        /// <summary>Hareket kapalı: hep statik PNG kalır (ör. çok sayıda ikonun yan yana olduğu listeler).</summary>
        public bool Static { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "img";
            output.TagMode = TagMode.SelfClosing;

            output.Attributes.SetAttribute("src", $"{IconPath}{Name}.png");
            output.Attributes.SetAttribute("width", Size);
            output.Attributes.SetAttribute("height", Size);
            output.Attributes.SetAttribute("alt", string.Empty);       // dekoratif: ekran okuyucu atlasın
            output.Attributes.SetAttribute("aria-hidden", "true");
            output.Attributes.SetAttribute("decoding", "async");
            output.Attributes.SetAttribute("draggable", "false");

            var existingClass = output.Attributes["class"]?.Value?.ToString();
            output.Attributes.SetAttribute("class", string.IsNullOrWhiteSpace(existingClass)
                ? "glass-icon"
                : $"glass-icon {existingClass}");

            if (!Static)
            {
                output.Attributes.SetAttribute("data-glass-video", $"{IconPath}{Name}.webm");
            }
        }
    }
}
