"""One-time, repeatable move to the installed asset layout; never touches user data."""
from pathlib import Path
import json, shutil
root = Path(__file__).resolve().parent.parent
assets = root / 'assets'
def move(source, target):
    if not source.exists(): return
    if not source.resolve().is_relative_to(root) or not target.resolve().is_relative_to(root): raise ValueError('Outside project')
    target.parent.mkdir(parents=True, exist_ok=True)
    if target.exists():
        if source.read_bytes() != target.read_bytes(): raise ValueError('Conflicting asset '+str(target))
        source.unlink()
    else: shutil.move(str(source), str(target))
siege = assets / 'siege'
catalog = json.loads((siege / 'catalog.json').read_text(encoding='utf-8-sig'))
for op in catalog['operators']:
    move(siege / 'icons' / (op['id']+'.png'), siege / 'operators/icons' / (op['id']+'.png'))
    op['icon'] = 'operators/icons/'+op['id']+'.png'
    move(siege / 'portraits' / (op['id']+'.png'), root / 'build-config/legacy-cropped-portraits' / (op['id']+'.png'))
(siege / 'catalog.json').write_text(json.dumps(catalog,ensure_ascii=False,indent=2),encoding='utf-8')
manifest=json.loads((siege/'manifest.json').read_text(encoding='utf-8-sig'))
for item in manifest:
    if item['kind']=='icon': item['file']='operators/icons/'+item['id']+'.png'
(siege/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
studio=assets/'studio'
aliases=json.loads((studio/'aliases.json').read_text(encoding='utf-8-sig'))
relocated={}
for file in list(studio.iterdir()):
    if not file.is_file() or file.name=='aliases.json': continue
    name=file.name
    category=('layouts' if name.startswith('layout-') else 'templates/'+name.split('-')[1] if name.startswith('artwork-') else 'backgrounds' if name.startswith('background-') else 'icons' if name.startswith('Icon_') or name in ('kills.png','deaths.png','assists.png') else 'map-veto')
    destination=studio/category/name
    move(file,destination); relocated[name]=str(destination.relative_to(studio)).replace('\\','/')
for name,target in list(aliases.items()): aliases[name]=relocated.get(target,target)
aliases.update(relocated)
(studio/'aliases.json').write_text(json.dumps(aliases,indent=2),encoding='utf-8')
print('Organized operator icons, source portraits, templates, layouts, backgrounds and map-veto assets.')
