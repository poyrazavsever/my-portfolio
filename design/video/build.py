"""
Demo videosu üretim hattı: ekran görüntüleri + HTML sahne kartları + İngilizce seslendirme -> MP4 ve README animasyonu.

Kullanım (uygulama http://localhost:5000 adresinde çalışırken):
  python design/video/build.py shots     # headless Chrome ile güncel ekran görüntüleri
  python design/video/build.py tts       # Higgsfield (Inworld TTS) ile sahne başına seslendirme (~2 kredi/sahne)
  python design/video/build.py render    # HTML sahne kartlarını PNG'ye çiz
  python design/video/build.py compose   # ffmpeg: sahneler -> docs/demo.mp4 (sesli) + docs/demo.webp (sessiz, README)

Ara dosyalar ASCII bir geçici klasörde tutulur (Chrome, Türkçe karakterli yollara ekran görüntüsü yazamıyor).
"""
import json
import os
import re
import shutil
import subprocess
import sys
import tempfile
import urllib.request
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
WWW = ROOT / "Portfolio" / "wwwroot"
DOCS = ROOT / "docs"
WORK = Path(tempfile.gettempdir()) / "portfolio-video"
CHROME = r"C:\Program Files\Google\Chrome\Application\chrome.exe"
HF = shutil.which("higgsfield") or "higgsfield"
BASE = "http://localhost:5000"
VOICE = "Ethan (en)"

W, H, FPS = 1280, 720, 30
# Tarayıcı penceresi çerçevesi (sahne kartında) ve içindeki ekran görüntüsü alanı
WIN_X, WIN_Y, WIN_W, TITLEBAR = 80, 34, 1120, 34
CONTENT_Y = WIN_Y + TITLEBAR
CONTENT_H = 540

# Her sahne: tür, kaynak görüntü, altyazı, seslendirme metni
SCENES = [
    {"id": "title", "kind": "card",
     "vo": "My portfolio, rebuilt as a CMS with ASP.NET Core MVC."},
    {"id": "home", "kind": "scroll", "shot": "home-full.png", "pan_to": 3340,
     "title": "Public site", "sub": "Every section is rendered live from SQL Server",
     "vo": "Every section is rendered live from SQL Server with EF Core."},
    {"id": "mobile", "kind": "phone", "shot": "mobile-home.png",
     "title": "Responsive", "sub": "Category filters · instant search · mobile menu",
     "vo": "It is fully responsive, with category filters and instant search."},
    {"id": "dashboard", "kind": "pan", "shot": "admin-dashboard.png",
     "title": "Admin dashboard", "sub": "Count · GroupBy · Take — all live EF Core queries",
     "vo": "The admin dashboard summarizes everything with live queries."},
    {"id": "inbox", "kind": "pan", "shot": "admin-inbox.png",
     "title": "Inbox", "sub": "Search · read tracking · Bootstrap modal details",
     "vo": "An inbox with search, read tracking and modal details."},
    {"id": "projects", "kind": "pan", "shot": "admin-projects.png",
     "title": "Content management", "sub": "CRUD · validation · safe image and PDF uploads",
     "vo": "Projects, skills and the résumé are all managed from here."},
    {"id": "arch", "kind": "card",
     "vo": "MVC areas, view components, a code-first model and focused services."},
    {"id": "outro", "kind": "card",
     "vo": "Glass icons made with Higgsfield. Source on GitHub."},
]


def run(cmd, **kw):
    return subprocess.run(cmd, capture_output=True, text=True, encoding="utf-8", **kw)


def ensure_dirs():
    for sub in ("shots", "audio", "cards", "segments"):
        (WORK / sub).mkdir(parents=True, exist_ok=True)
    (WORK / "assets").mkdir(exist_ok=True)
    for name in ("brand/avatar-192.webp", "icons/projects.png", "icons/dashboard.png", "icons/skills.png",
                 "icons/workflow.png", "icons/messages.png", "icons/cv.png", "icons/about.png"):
        shutil.copy(WWW / "assets" / name, WORK / "assets" / Path(name).name)


def chrome_shot(out, url, size, transparent=False):
    args = [CHROME, "--headless=new", "--disable-gpu", "--hide-scrollbars", "--force-device-scale-factor=1",
            "--virtual-time-budget=4000", f"--window-size={size}", f"--screenshot={out}"]
    if transparent:
        args.append("--default-background-color=00000000")
    run(args + [url])
    if not Path(out).exists():
        raise RuntimeError(f"Ekran görüntüsü alınamadı: {url}")


# ---------------------------------------------------------------- 1) shots
def shots():
    ensure_dirs()
    d = WORK / "shots"
    chrome_shot(d / "home-full.png", f"{BASE}/", "1440,4400")
    chrome_shot(d / "mobile-home.png", f"{BASE}/", "390,844")
    chrome_shot(d / "admin-dashboard.png", f"{BASE}/Admin", "1440,1000")
    chrome_shot(d / "admin-inbox.png", f"{BASE}/Admin/Message/MessageList?open=1", "1440,1000")
    chrome_shot(d / "admin-projects.png", f"{BASE}/Admin/Project/ProjectList", "1440,1000")
    print("ekran görüntüleri tamam")


# ---------------------------------------------------------------- 2) tts
def tts():
    ensure_dirs()
    for scene in SCENES:
        out = WORK / "audio" / f"{scene['id']}.mp3"
        if out.exists():
            print(f"{scene['id']}: zaten var, atlandı")
            continue
        res = run([HF, "generate", "create", "inworld_text_to_speech",
                   "--prompt", scene["vo"], "--voice", VOICE, "--wait", "--json"])
        urls = re.findall(r'https://[^"\s]+\.(?:mp3|wav|ogg|m4a)', res.stdout)
        if not urls:
            print(f"{scene['id']}: HATA {res.stderr.strip()[-300:]}")
            continue
        with urllib.request.urlopen(urls[0]) as r:
            raw = r.read()
        tmp = out.with_suffix(Path(urls[0]).suffix + ".src")
        tmp.write_bytes(raw)
        run(["ffmpeg", "-v", "error", "-y", "-i", str(tmp), "-ar", "44100", "-ac", "2", str(out)])
        tmp.unlink()
        print(f"{scene['id']}: {duration(out):.2f} sn")


def fitted(scene):
    """Seslendirme klibinin kırpılmış ve %10 hızlandırılmış hali (perde korunur)."""
    src = WORK / "audio" / f"{scene['id']}.mp3"
    out = WORK / "audio" / f"{scene['id']}.fit.mp3"
    if src.exists() and (not out.exists() or out.stat().st_mtime < src.stat().st_mtime):
        trim = "silenceremove=start_periods=1:start_threshold=-45dB:start_silence=0.05"
        run(["ffmpeg", "-v", "error", "-y", "-i", str(src), "-af",
             f"{trim},areverse,{trim},areverse,atempo=1.1", str(out)])
    return out


def duration(path):
    res = run(["ffprobe", "-v", "error", "-show_entries", "format=duration", "-of", "csv=p=0", str(path)])
    return float(res.stdout.strip() or 0)


# ---------------------------------------------------------------- 3) render
CSS = """
@import url('https://fonts.googleapis.com/css2?family=IBM+Plex+Mono:wght@400;500&family=IBM+Plex+Sans:wght@400;500;600;700&display=swap');
*{box-sizing:border-box;margin:0}
html,body{width:1280px;height:720px;overflow:hidden}
body{font-family:"IBM Plex Sans",sans-serif;color:#EDEDEF}
.bg{background-color:#0A0B0D;background-image:
  linear-gradient(rgba(255,255,255,.055) 1px,transparent 1px),linear-gradient(90deg,rgba(255,255,255,.055) 1px,transparent 1px),
  linear-gradient(rgba(255,255,255,.03) 1px,transparent 1px),linear-gradient(90deg,rgba(255,255,255,.03) 1px,transparent 1px);
  background-size:120px 120px,120px 120px,24px 24px,24px 24px}
.mono{font-family:"IBM Plex Mono",monospace}
.red{color:#FF5A54}
.glass{background:rgba(255,255,255,.04);border:1px solid rgba(255,255,255,.1);border-top-color:rgba(255,255,255,.18);border-radius:14px}
.ticks{position:relative}
.ticks::before,.ticks::after{content:"";position:absolute;width:12px;height:12px;border-color:#5A5C63;border-style:solid}
.ticks::before{top:-7px;left:-7px;border-width:1px 0 0 1px}.ticks::after{bottom:-7px;right:-7px;border-width:0 1px 1px 0}
"""


def frame_html(title, sub):
    """Tarayıcı çerçevesi + altyazı. Pencere içi şeffaf: ekran görüntüsü ffmpeg ile altına yerleşir."""
    return f"""<!doctype html><html><head><meta charset="utf-8"><style>{CSS}
body{{background:transparent}}
.win{{position:absolute;left:{WIN_X}px;top:{WIN_Y}px;width:{WIN_W}px;height:{TITLEBAR + CONTENT_H}px;
  border:1px solid rgba(255,255,255,.14);border-radius:12px 12px 4px 4px;box-shadow:0 30px 60px -30px #000}}
.bar{{height:{TITLEBAR}px;display:flex;align-items:center;gap:8px;padding:0 14px;background:#121317;
  border-bottom:1px solid rgba(255,255,255,.08);border-radius:12px 12px 0 0}}
.dot{{width:10px;height:10px;border-radius:50%;background:#2A2B31}}.dot:first-child{{background:#E5322D}}
.url{{margin-left:14px;font:12px "IBM Plex Mono";color:#8B8D94;background:#0C0D10;border:1px solid rgba(255,255,255,.07);
  border-radius:7px;padding:4px 12px;width:420px}}
.cap{{position:absolute;left:{WIN_X}px;top:{WIN_Y + TITLEBAR + CONTENT_H + 22}px;display:flex;align-items:baseline;gap:18px}}
.cap h2{{font-size:26px;font-weight:600;letter-spacing:-.02em}}
.cap p{{font:14px "IBM Plex Mono";color:#8B8D94}}
.mark{{width:9px;height:9px;background:#E5322D;display:inline-block;margin-right:12px;transform:translateY(-3px)}}
</style></head><body>
<div class="win"><div class="bar"><span class="dot"></span><span class="dot"></span><span class="dot"></span>
<span class="url">localhost:5000</span></div></div>
<div class="cap"><h2><span class="mark"></span>{title}</h2><p>{sub}</p></div>
</body></html>"""


def phone_html(title, sub):
    return f"""<!doctype html><html><head><meta charset="utf-8"><style>{CSS}
body{{background:transparent}}
.phone{{position:absolute;left:760px;top:24px;width:324px;height:674px;border:12px solid #17181C;border-radius:40px;
  box-shadow:0 0 0 1px rgba(255,255,255,.14),0 30px 60px -30px #000}}
.cap{{position:absolute;left:120px;top:300px;width:520px}}
.cap h2{{font-size:44px;font-weight:600;letter-spacing:-.03em}}
.cap p{{margin-top:14px;font:16px "IBM Plex Mono";color:#8B8D94;line-height:1.6}}
.mark{{width:12px;height:12px;background:#E5322D;display:block;margin-bottom:22px}}
</style></head><body><div class="phone"></div>
<div class="cap"><span class="mark"></span><h2>{title}</h2><p>{sub}</p></div></body></html>"""


def title_html():
    return f"""<!doctype html><html><head><meta charset="utf-8"><style>{CSS}
.wrap{{position:absolute;left:120px;top:190px}}
.brand{{display:flex;align-items:center;gap:14px;font-size:18px;font-weight:600}}
.brand img{{width:44px;height:44px;border-radius:11px;border:1px solid rgba(255,255,255,.18);box-shadow:0 3px 0 -1px #E5322D}}
h1{{margin-top:36px;font-size:76px;line-height:1;letter-spacing:-.045em;font-weight:600}}
p{{margin-top:22px;font:18px "IBM Plex Mono";color:#8B8D94}}
.icon{{position:absolute;right:120px;top:170px;width:360px}}
</style></head><body class="bg">
<img class="icon" src="assets/projects.png">
<div class="wrap"><div class="brand"><img src="assets/avatar-192.webp">Poyraz Avsever</div>
<h1>Portfolio CMS</h1><p>ASP.NET Core MVC · EF Core · SQL Server</p></div></body></html>"""


def arch_html():
    def box(icon, title, lines, red=False):
        items = "".join(f"<li>{line}</li>" for line in lines)
        return f"""<div class="box glass ticks {'hl' if red else ''}"><img src="assets/{icon}.png"><h3>{title}</h3><ul>{items}</ul></div>"""
    arrow = '<div class="arrow"><span></span></div>'
    return f"""<!doctype html><html><head><meta charset="utf-8"><style>{CSS}
h2{{position:absolute;left:80px;top:56px;font-size:40px;letter-spacing:-.03em;font-weight:600}}
.lead{{position:absolute;left:80px;top:112px;font:15px "IBM Plex Mono";color:#8B8D94}}
.row{{position:absolute;left:80px;right:80px;top:200px;display:flex;align-items:stretch}}
.box{{flex:1;padding:22px 20px 18px}}
.box.hl{{border-color:rgba(229,50,45,.45)}}
.box img{{width:64px;height:64px;margin:-6px 0 6px -6px}}
.box h3{{font-size:19px;font-weight:600;letter-spacing:-.01em}}
.box ul{{list-style:none;padding:0;margin-top:12px}}
.box li{{font:13.5px "IBM Plex Mono";color:#A9ABB2;padding:5px 0;border-top:1px solid rgba(255,255,255,.07)}}
.arrow{{width:44px;display:flex;align-items:center;justify-content:center}}
.arrow span{{width:30px;height:1px;background:#E5322D;position:relative}}
.arrow span::after{{content:"";position:absolute;right:-1px;top:-4px;border:4px solid transparent;border-left:7px solid #E5322D}}
.foot{{position:absolute;left:80px;top:540px;font:14px "IBM Plex Mono";color:#8B8D94}}
.foot b{{color:#EDEDEF;font-weight:500}}
</style></head><body class="bg">
<h2>Architecture</h2><p class="lead">ASP.NET Core MVC (.NET 10) · Code First · no repository layer — kept simple on purpose</p>
<div class="row">
{box("about", "Public site", ["DefaultController", "9 View Components", "Tag Helper &lt;glass-icon&gt;", "Contact form + honeypot"])}
{arrow}
{box("dashboard", "Admin area", ["Areas/Admin", "CRUD controllers", "DashboardViewModel", "SweetAlert2 + TempData"], True)}
{arrow}
{box("workflow", "Services", ["ImageStorage → wwwroot", "CvStorage → App_Data", "magic-byte checks", "DI: AddScoped"])}
{arrow}
{box("skills", "Data", ["EF Core 10", "6 migrations", "Category 1—N Project", "SQL Server LocalDB"])}
</div>
<p class="foot"><b>Design system</b> — CSS tokens · Bootstrap 5 themed via --bs-* · glass icons: GPT Image 2.5 + MiniMax H3 → ffmpeg alpha WebM</p>
</body></html>"""


def outro_html():
    return f"""<!doctype html><html><head><meta charset="utf-8"><style>{CSS}
.wrap{{position:absolute;left:0;right:0;top:220px;text-align:center}}
.wrap img{{width:72px;height:72px;border-radius:16px;border:1px solid rgba(255,255,255,.18);box-shadow:0 4px 0 -1px #E5322D}}
h2{{margin-top:28px;font-size:46px;letter-spacing:-.035em;font-weight:600}}
p{{margin-top:16px;font:18px "IBM Plex Mono";color:#8B8D94}}
p.red{{color:#FF5A54;margin-top:8px}}
</style></head><body class="bg"><div class="wrap"><img src="assets/avatar-192.webp">
<h2>Poyraz Avsever</h2><p>github.com/poyrazavsever/my-portfolio</p><p class="red">poyrazavsever.com</p></div></body></html>"""


def render():
    ensure_dirs()
    pages = {"bg": f'<!doctype html><html><head><style>{CSS}</style></head><body class="bg"></body></html>',
             "title": title_html(), "arch": arch_html(), "outro": outro_html()}
    for scene in SCENES:
        if scene["kind"] in ("scroll", "pan"):
            pages[f"frame-{scene['id']}"] = frame_html(scene["title"], scene["sub"])
        elif scene["kind"] == "phone":
            pages[f"frame-{scene['id']}"] = phone_html(scene["title"], scene["sub"])

    for name, html in pages.items():
        page = WORK / f"{name}.html"
        page.write_text(html, encoding="utf-8")
        transparent = name.startswith("frame-")
        chrome_shot(WORK / "cards" / f"{name}.png", page.as_uri(), f"{W},{H}", transparent)
    print(f"{len(pages)} kart çizildi")


# ---------------------------------------------------------------- 4) compose
def scene_length(scene):
    audio = fitted(scene)
    spoken = duration(audio) if audio.exists() else 3.0
    return round(max(3.0, spoken + 0.6), 2)


def build_segment(scene, length):
    out = WORK / "segments" / f"{scene['id']}.mp4"
    cards, shots_dir = WORK / "cards", WORK / "shots"
    fade = f"fade=t=in:st=0:d=0.3,fade=t=out:st={length - 0.3}:d=0.3"
    ease = f"(1-cos(PI*min(t/{length - 0.6},1)))/2"  # yumuşak başla, yumuşak dur

    if scene["kind"] == "card":
        inputs = ["-loop", "1", "-t", str(length), "-i", str(cards / f"{scene['id']}.png")]
        graph = f"[0:v]scale={W}:{H},format=yuv420p,{fade}[v]"
    else:
        shot = shots_dir / scene["shot"]
        inputs = ["-loop", "1", "-t", str(length), "-i", str(cards / "bg.png"),
                  "-loop", "1", "-t", str(length), "-i", str(shot),
                  "-loop", "1", "-t", str(length), "-i", str(cards / f"frame-{scene['id']}.png")]
        if scene["kind"] == "phone":
            # Telefon ekranı: 300 px genişlik, hafif aşağı kayma
            graph = (f"[1:v]scale=300:-2,crop=300:650:0:'min(ih-650,40*{ease})'[c];"
                     f"[0:v][c]overlay=772:36[b];[b][2:v]overlay=0:0,format=yuv420p,{fade}[v]")
        else:
            scale = WIN_W / 1440
            pan = scene.get("pan_to", 999999) * scale
            graph = (f"[1:v]scale={WIN_W}:-1,crop={WIN_W}:{CONTENT_H}:0:'min(ih-{CONTENT_H},{pan:.0f})*{ease}'[c];"
                     f"[0:v][c]overlay={WIN_X}:{CONTENT_Y}[b];[b][2:v]overlay=0:0,format=yuv420p,{fade}[v]")
    res = run(["ffmpeg", "-v", "error", "-y", *inputs, "-filter_complex", graph, "-map", "[v]",
               "-r", str(FPS), "-c:v", "libx264", "-preset", "medium", "-crf", "20", str(out)])
    if res.returncode:
        raise RuntimeError(f"{scene['id']}: {res.stderr[-400:]}")
    return out


def compose():
    ensure_dirs()
    segments, lengths = [], []
    for scene in SCENES:
        length = scene_length(scene)
        segments.append(build_segment(scene, length))
        lengths.append(length)
        print(f"{scene['id']}: {length:.2f} sn")

    # Video: segmentleri uç uca ekle
    listfile = WORK / "segments.txt"
    listfile.write_text("".join(f"file '{p.as_posix()}'\n" for p in segments), encoding="utf-8")
    silent = WORK / "demo-silent.mp4"
    run(["ffmpeg", "-v", "error", "-y", "-f", "concat", "-safe", "0", "-i", str(listfile), "-c", "copy", str(silent)])

    # Ses: her cümle kendi sahnesinin 0.25 sn sonrasında başlar
    audio_inputs, filters, offset = [], [], 0.0
    for index, (scene, length) in enumerate(zip(SCENES, lengths)):
        clip = fitted(scene)
        if clip.exists():
            audio_inputs += ["-i", str(clip)]
            n = len(audio_inputs) // 2
            delay = int((offset + 0.2) * 1000)
            filters.append(f"[{n}:a]adelay={delay}|{delay}[a{index}]")
        offset += length
    DOCS.mkdir(exist_ok=True)
    mp4 = DOCS / "demo.mp4"

    if filters:
        labels = "".join(f"[a{i}]" for i in range(len(SCENES)) if f"[a{i}]" in "".join(filters))
        mix = ";".join(filters) + f";{labels}amix=inputs={labels.count('[')}:normalize=0,apad[aout]"
        res = run(["ffmpeg", "-v", "error", "-y", "-i", str(silent), *audio_inputs, "-filter_complex", mix,
                   "-map", "0:v", "-map", "[aout]", "-c:v", "copy", "-c:a", "aac", "-b:a", "128k", "-shortest",
                   "-movflags", "+faststart", str(mp4)])
        if res.returncode:
            raise RuntimeError(res.stderr[-400:])
    else:
        # Henüz seslendirme üretilmediyse sessiz video
        shutil.copy(silent, mp4)

    # README: sessiz, döngüsel animasyonlu WebP (GitHub README'de otomatik oynar).
    # Kareleri ffmpeg ile çıkarıp Pillow ile birleştiriyoruz: her karenin süresi açıkça 100 ms olsun
    # (ffmpeg'in libwebp_anim encoder'ı birleştirdiği karelerin süresini her okuyucunun anlayamadığı biçimde yazıyor).
    from PIL import Image

    frames_dir = WORK / "frames"
    shutil.rmtree(frames_dir, ignore_errors=True)
    frames_dir.mkdir()
    run(["ffmpeg", "-v", "error", "-y", "-i", str(silent), "-vf", "fps=10,scale=960:-2:flags=lanczos",
         str(frames_dir / "f%04d.png")])
    frames = [Image.open(path).convert("RGB") for path in sorted(frames_dir.glob("f*.png"))]
    webp = DOCS / "demo.webp"
    frames[0].save(webp, format="WEBP", save_all=True, append_images=frames[1:],
                   duration=100, loop=0, quality=62, method=6)

    total = sum(lengths)
    print(f"toplam {total:.1f} sn · demo.mp4 {mp4.stat().st_size / 1e6:.1f} MB · demo.webp {webp.stat().st_size / 1e6:.1f} MB")


if __name__ == "__main__":
    {"shots": shots, "tts": tts, "render": render, "compose": compose}[sys.argv[1]]()
