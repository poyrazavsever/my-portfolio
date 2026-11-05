"""
Cam ikon üretim hattı (Higgsfield CLI + ffmpeg).

Kullanım:
  python design/icons/generate.py image   [key ...]   # GPT Image 2.5 ile statik görsel
  python design/icons/generate.py video   [key ...]   # MiniMax H3 ile döngüsel animasyon
  python design/icons/generate.py convert [key ...]   # şeffaf WebM + PNG poster -> wwwroot
  python design/icons/generate.py sheet               # kontrol için tüm ikonların kontakt sayfası

key verilmezse icons.json'daki tüm ikonlar işlenir. Ham çıktılar design/icons/raw/ altına
(git'e girmez), web'e hazır dosyalar Portfolio/wwwroot/assets/icons/ altına yazılır.
"""
import json
import re
import shutil
import subprocess
import sys
import urllib.request
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path

ROOT = Path(__file__).resolve().parent
RAW = ROOT / "raw"
OUT = ROOT.parent.parent / "Portfolio" / "wwwroot" / "assets" / "icons"
CONFIG = json.loads((ROOT / "icons.json").read_text(encoding="utf-8"))

BG_HEX = "0x0A0B0D"          # model çıktılarının ortalama arka plan rengi
KEY = f"colorkey={BG_HEX}:0.10:0.08"
WEB_SIZE = 360               # 2x ekranlarda ~180px gösterim için yeterli
HF = shutil.which("higgsfield") or "higgsfield"


def run(cmd):
    return subprocess.run(cmd, capture_output=True, text=True, encoding="utf-8")


def first_url(text, exts):
    found = re.findall(r'https://[^"\s]+\.(?:%s)' % "|".join(exts), text)
    full = [u for u in found if "_min." not in u]
    return (full or found or [None])[0]


def download(url, path):
    with urllib.request.urlopen(url) as r, open(path, "wb") as f:
        f.write(r.read())


def gen_image(icon):
    prompt = CONFIG["style"].format(subject=icon["subject"], accent=icon["accent"])
    res = run([HF, "generate", "create", "gpt_image_2_5",
               "--quality", "high", "--resolution", "1k", "--aspect_ratio", "1:1",
               "--background", "opaque", "--prompt", prompt, "--wait", "--json"])
    url = first_url(res.stdout, ["png", "jpg", "jpeg", "webp"])
    if not url:
        return f"{icon['key']}: HATA {res.stderr.strip()[-200:]}"
    download(url, RAW / f"{icon['key']}.png")
    return f"{icon['key']}: görsel tamam"


def gen_video(icon):
    src = RAW / f"{icon['key']}.png"
    if not src.exists():
        return f"{icon['key']}: önce görsel üretilmeli"
    prompt = CONFIG["motion"].format(motion=icon["motion"])
    res = run([HF, "generate", "create", "minimax_h3",
               "--start-image", str(src), "--end-image", str(src),
               "--duration", "5", "--prompt", prompt,
               "--wait", "--wait-timeout", "20m", "--json"])
    url = first_url(res.stdout, ["mp4"])
    if not url:
        return f"{icon['key']}: HATA {res.stderr.strip()[-200:]}"
    download(url, RAW / f"{icon['key']}.mp4")
    return f"{icon['key']}: video tamam"


def convert(icon):
    k = icon["key"]
    OUT.mkdir(parents=True, exist_ok=True)
    png, mp4 = RAW / f"{k}.png", RAW / f"{k}.mp4"
    msgs = []
    if png.exists():
        r = run(["ffmpeg", "-v", "error", "-y", "-i", str(png),
                 "-vf", f"scale={WEB_SIZE}:-2,{KEY},format=rgba", str(OUT / f"{k}.png")])
        msgs.append("png" if r.returncode == 0 else f"png HATA {r.stderr[-200:]}")
    if mp4.exists():
        r = run(["ffmpeg", "-v", "error", "-y", "-i", str(mp4), "-an",
                 "-vf", f"scale={WEB_SIZE}:-2,fps=30,{KEY},format=yuva420p",
                 "-c:v", "libvpx-vp9", "-b:v", "0", "-crf", "34",
                 "-auto-alt-ref", "0", "-row-mt", "1", str(OUT / f"{k}.webm")])
        msgs.append("webm" if r.returncode == 0 else f"webm HATA {r.stderr[-200:]}")
    return f"{k}: " + (", ".join(msgs) or "kaynak yok")


def sheet():
    files = [RAW / f"{i['key']}.png" for i in CONFIG["icons"] if (RAW / f"{i['key']}.png").exists()]
    inputs = sum([["-i", str(f)] for f in files], [])
    n = len(files)
    filt = "".join(f"[{i}:v]scale=256:256[s{i}];" for i in range(n))
    filt += "".join(f"[s{i}]" for i in range(n)) + f"xstack=inputs={n}:layout=" + "|".join(
        f"{(i % 6) * 256}_{(i // 6) * 256}" for i in range(n)) + ":fill=black"
    out = RAW / "_sheet.png"
    r = run(["ffmpeg", "-v", "error", "-y", *inputs, "-filter_complex", filt, "-frames:v", "1", str(out)])
    return str(out) if r.returncode == 0 else r.stderr


def main():
    RAW.mkdir(exist_ok=True)
    stage, keys = sys.argv[1], set(sys.argv[2:])
    if stage == "sheet":
        print(sheet())
        return
    icons = [i for i in CONFIG["icons"] if not keys or i["key"] in keys]
    fn = {"image": gen_image, "video": gen_video, "convert": convert}[stage]
    with ThreadPoolExecutor(max_workers=6) as pool:
        for msg in pool.map(fn, icons):
            print(msg, flush=True)


if __name__ == "__main__":
    main()
