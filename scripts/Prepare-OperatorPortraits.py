"""Build UI portraits from the uncropped official artwork; keep source attribution."""
from pathlib import Path
from concurrent.futures import ThreadPoolExecutor, as_completed
from PIL import Image
import urllib.request, re, json, io, hashlib, html, time, sys

root = Path(__file__).resolve().parent.parent
siege = root / 'assets/siege'
catalog = json.loads((siege / 'catalog.json').read_text(encoding='utf-8-sig'))
cache = root / 'build-config/operator-downloads'
cache.mkdir(parents=True, exist_ok=True)

def fetch(url):
    for attempt in range(3):
        try:
            with urllib.request.urlopen(urllib.request.Request(url, headers={'User-Agent': 'CasterDesk asset preparation'}), timeout=35) as response:
                return response.read(15_000_001)
        except Exception:
            if attempt == 2: raise
            time.sleep(attempt + 1)

def prepare(op):
    slug = op['id']
    page = f'https://www.ubisoft.com/pt-br/game/rainbow-six/siege/game-info/operators/{slug}'
    meta = cache / (slug + '.json')
    raw = cache / (slug + '.png')
    if raw.exists() and meta.exists():
        origin = json.loads(meta.read_text())
        data = raw.read_bytes()
    else:
        markup = fetch(page).decode('utf-8')
        urls = set(re.findall(r'https://staticctf\.ubisoft\.com/[^\s<>"\\)]+\.png', html.unescape(markup)))
        candidates = [u for u in urls if re.search(r'r6[s]?-operator[s]?-[^/]+\.png$', u, re.I)
                      and not re.search(r'badge|list-|ability|elite|gadget|loadout|faction|biography|bio-', u, re.I)]
        if not candidates: raise RuntimeError(f'{slug}: no full-width official artwork found')
        # The hero belongs to this operator's own page; prefer its slug over incidental artwork.
        candidates.sort(key=lambda u: (slug.replace('-', '') not in u.rsplit('/', 1)[-1].replace('-', ''), len(u)))
        url = candidates[0]
        data = fetch(url)
        origin = {'id': slug, 'source_page': page, 'url': url}
        raw.write_bytes(data)
        meta.write_text(json.dumps(origin), encoding='utf-8')
    if len(data) > 15_000_000: raise RuntimeError(f'{slug}: source too large')
    image = Image.open(io.BytesIO(data)).convert('RGBA')
    if image.width < 350 or image.height < 350: raise RuntimeError(f'{slug}: source is a thumbnail')
    original = siege / 'operators/originals' / (slug + '.png')
    original.parent.mkdir(parents=True, exist_ok=True)
    image.save(original, optimize=True)
    bbox = image.getchannel('A').getbbox()
    if not bbox: raise RuntimeError(f'{slug}: empty artwork')
    left, top, right, bottom = bbox
    # Shield exceptions focus the operator rather than empty shield area.
    # Blitz keeps the normal framing: his shield is already below the bust.
    focus = {'montagne': (0, 0, .78, .86), 'blackbeard': (0, 0, .72, .86), 'clash': (0, .13, 1, 1),
             'glaz': (0, 0, .94, 1)}.get(slug, (0, 0, 1, .86))
    crop_box = [left + round((right-left)*focus[0]), top + round((bottom-top)*focus[1]), left + round((right-left)*focus[2]), top + round((bottom-top)*focus[3])]
    # A wide rifle/pose must not push the head down. Include enough torso to fill
    # the same frame and anchor every portrait at its top, preserving proportions.
    needed_height = round((crop_box[2] - crop_box[0]) * 280 / 312)
    if slug != 'blitz': crop_box[3] = min(bottom, max(crop_box[3], crop_box[1] + needed_height))
    crop = image.crop(crop_box)
    for size, label in [(320, 'large'), (160, 'medium'), (80, 'small')]:
        canvas = Image.new('RGBA', (size, round(size * .9)))
        fitted = crop.copy()
        fitted.thumbnail((size - max(4, round(size * .025)), canvas.height - max(4, round(size * .025))), Image.Resampling.LANCZOS)
        canvas.alpha_composite(fitted, ((size - fitted.width) // 2, max(2, round(size * .025))))
        destination = siege / 'operators/portraits' / label / (slug + '.png')
        destination.parent.mkdir(parents=True, exist_ok=True)
        canvas.save(destination, optimize=True)
    return {**origin, 'source_size': list(image.size), 'source_sha256': hashlib.sha256(data).hexdigest(),
            'crop': crop_box, 'anchor': 'top', 'variants': {'large': [320,288], 'medium': [160,144], 'small': [80,72]}}

wanted = set(sys.argv[1:])
old_manifest = siege / 'operators/portraits-manifest.json'
results = [r for r in json.loads(old_manifest.read_text(encoding='utf-8')) if r['id'] not in wanted] if wanted and old_manifest.exists() else []
errors = []
with ThreadPoolExecutor(max_workers=4) as pool:
    jobs = {pool.submit(prepare, op): op['id'] for op in catalog['operators'] if not wanted or op['id'] in wanted}
    for job in as_completed(jobs):
        try:
            results.append(job.result())
            print('Prepared', jobs[job], flush=True)
        except Exception as error:
            errors.append(str(error))
            print('FAILED', error, flush=True)
(siege / 'operators').mkdir(exist_ok=True)
(siege / 'operators/portraits-manifest.json').write_text(json.dumps(sorted(results,key=lambda r:r['id']), ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps({'prepared':len(results),'errors':errors}, ensure_ascii=False), flush=True)
if errors: raise SystemExit(1)
