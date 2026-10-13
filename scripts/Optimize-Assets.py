from pathlib import Path
from PIL import Image
import numpy as np,json,shutil,hashlib
project=Path(__file__).resolve().parent.parent
root=project/'assets';backup=project.parent.parent/'work/image-optimization-originals';backup.mkdir(exist_ok=True)
report=[];total=sum(f.stat().st_size for f in root.rglob('*') if f.is_file())
for f in list(root.rglob('*.png')):
 im=Image.open(f);rgba=np.asarray(im.convert('RGBA'));before=f.stat().st_size
 candidate=im.copy()
 if (rgba[:,:,3]==255).all():
  if (rgba[:,:,0]==rgba[:,:,1]).all() and (rgba[:,:,1]==rgba[:,:,2]).all():candidate=im.convert('L')
  elif im.mode not in ('P','L'):candidate=im.convert('RGB')
 tmp=f.with_suffix('.optimized.png')
 candidate.save(tmp,optimize=True,compress_level=9)
 if tmp.stat().st_size<before:
  decoded=np.asarray(Image.open(tmp).convert('RGBA'))
  if not np.array_equal(decoded,rgba):raise RuntimeError('Pixel mismatch '+str(f))
  dest=backup/f.relative_to(root);dest.parent.mkdir(parents=True,exist_ok=True)
  if not dest.exists():shutil.copy2(f,dest)
  after=tmp.stat().st_size;tmp.replace(f)
  report.append({'file':str(f),'before':before,'after':after,'pixels_identical':True,'dimensions':im.size})
 else:tmp.unlink()
studio=root/'studio';alias_file=studio/'aliases.json'
aliases=json.loads(alias_file.read_text(encoding='utf8')) if alias_file.exists() else {}
unique={};duplicates=[]
for f in sorted(studio.iterdir()):
 if f.suffix.lower() not in ('.png','.jpg'):continue
 digest=hashlib.sha256(f.read_bytes()).hexdigest()
 if digest in unique:
  canonical=unique[digest];aliases[f.name]=canonical.name
  # Flatten aliases that previously referred to this now-duplicated file.
  aliases={k:canonical.name if v==f.name else v for k,v in aliases.items()}
  dest=backup/'studio'/f.name;dest.parent.mkdir(parents=True,exist_ok=True)
  if not dest.exists():shutil.copy2(f,dest)
  duplicates.append({'file':f.name,'target':canonical.name,'bytes':f.stat().st_size});f.unlink()
 else:unique[digest]=f
alias_file.write_text(json.dumps(aliases,indent=2),encoding='utf8')
after=sum(f.stat().st_size for f in root.rglob('*') if f.is_file())
result={'assets_before':total,'assets_after':after,'saved':total-after,'files':report,'duplicates':duplicates}
(project.parent.parent/'work/image-optimization-report.json').write_text(json.dumps(result,indent=2),encoding='utf8')
print(json.dumps({'before_mb':round(total/1048576,2),'after_mb':round(after/1048576,2),'saved_mb':round((total-after)/1048576,2),'optimized_files':len(report)},indent=2))
