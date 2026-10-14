'use strict';
const $=id=>document.getElementById(id);
const key=new URLSearchParams(location.hash.slice(1)).get('key')||'';
const icons=new Map();
let state=null,posting=false,connected=false,rendered='';
const headers={'X-CasterDesk-Key':key,'Content-Type':'application/json'};
function showTab(tab){for(const name of ['timer','bans']){$(name).hidden=name!==tab;$('tab-'+name).setAttribute('aria-selected',String(name===tab));}}
$('tab-timer').addEventListener('click',()=>showTab('timer'));$('tab-bans').addEventListener('click',()=>showTab('bans'));
function enable(){const ready=connected&&state?.controllable&&!posting;for(const id of ['start','pause','add','subtract','set'])$(id).disabled=!ready;if(ready){$('start').disabled=state.timer.running||state.timer.seconds<=0;$('pause').disabled=!state.timer.running;$('subtract').disabled=state.timer.seconds<=0;}for(const button of $('operators').children)button.disabled=!ready||button.dataset.available!=='true';}
function format(seconds){const s=Math.ceil(seconds),h=Math.floor(s/3600),m=Math.floor(s/60)%60;return(h>0?String(h).padStart(2,'0')+':':'')+String(h>0?m:Math.floor(s/60)).padStart(2,'0')+':'+String(s%60).padStart(2,'0');}
async function image(id,img){if(icons.has(id)){img.src=icons.get(id);return;}try{const r=await fetch('/icons/'+encodeURIComponent(id),{headers});if(!r.ok)return;const url=URL.createObjectURL(await r.blob());icons.set(id,url);img.src=url;}catch{}}
function render(data){state=data;connected=true;document.documentElement.lang=data.language;const l=data.labels;
 $('connection').textContent=l.connected;$('context').textContent=data.match.teamA+' × '+data.match.teamB+(data.match.map?' · '+data.match.map:'');$('time').textContent=format(data.timer.seconds);
 $('tab-timer').textContent=l.timer;$('tab-bans').textContent=l.bans;$('start').textContent='▶ '+l.start;$('pause').textContent='Ⅱ '+l.pause;$('set').textContent=l.set;$('duration').setAttribute('aria-label',l.duration);$('search').placeholder=l.select;$('search').setAttribute('aria-label',l.search);
 if(!data.controllable)$('error').textContent=l.busy;else if($('error').textContent===l.busy)$('error').textContent='';
 const nextKey=data.revision+':'+data.pending+':'+data.role;
 if(nextKey!==rendered){rendered=nextKey;$('ban-slots').replaceChildren();
  for(const ban of data.bans){const slot=document.createElement('div');slot.className='slot'+(ban.slot===data.pending?' next':'');slot.style.setProperty('--team',ban.team===0?'#408fb3':'#ff7737');const title=document.createElement('strong');title.textContent=ban.name||(ban.unlocked?l.select:l.blocked);const detail=document.createElement('span');detail.textContent=(ban.team===0?data.match.teamA:data.match.teamB)+' · '+(ban.role==='DEF'?l.def:l.atq);slot.append(title,detail);$('ban-slots').append(slot);}
  const pending=data.bans.find(b=>b.slot===data.pending);$('phase').textContent=pending?((pending.team===0?data.match.teamA:data.match.teamB)+' · '+(data.role==='DEF'?l.def:l.atq)):'—';$('bans-wait').textContent=data.pending===null?l.wait:'';renderOperators();
 }
 enable();
}
function renderOperators(){if(!state)return;$('operators').replaceChildren();const query=$('search').value.trim().toLocaleLowerCase();for(const op of state.operators.filter(o=>o.name.toLocaleLowerCase().includes(query))){const button=document.createElement('button');button.className='operator';button.dataset.available=String(op.enabled);button.setAttribute('aria-label',op.name);const img=document.createElement('img');img.alt='';image(op.id,img);const label=document.createElement('span');label.textContent=op.name;button.append(img,label);button.addEventListener('click',()=>command({action:'ban',matchId:state.match.id,mapId:state.match.mapId,revision:state.revision,slot:state.pending,operatorId:op.id}));$('operators').append(button);}enable();}
$('search').addEventListener('input',renderOperators);
async function command(body){if(posting||!connected||!state?.controllable)return;posting=true;enable();$('error').textContent='';try{const r=await fetch('/api/command',{method:'POST',headers,body:JSON.stringify(body)});const data=await r.json().catch(()=>null);if(!r.ok)throw new Error(data?.error||'Erro '+r.status);render(data);}catch(error){$('error').textContent=error.message;}finally{posting=false;enable();}}
$('start').addEventListener('click',()=>command({action:'start'}));$('pause').addEventListener('click',()=>command({action:'pause'}));$('add').addEventListener('click',()=>command({action:'adjust',seconds:60}));$('subtract').addEventListener('click',()=>command({action:'adjust',seconds:-60}));
$('set').addEventListener('click',()=>{const value=$('duration').value.trim();if(!/^\d{1,2}:\d{2}(?::\d{2})?$/.test(value)){ $('error').textContent=state?.labels.invalidTime||'Use mm:ss ou hh:mm:ss';return;}const parts=value.split(':').map(Number);if(parts.at(-1)>59||(parts.length===3&&parts[1]>59)){ $('error').textContent=state.labels.invalidTime;return;}const seconds=parts.length===3?parts[0]*3600+parts[1]*60+parts[2]:parts[0]*60+parts[1];command({action:'set',seconds});});
async function poll(){if(!key){$('connection').textContent='Use o endereço copiado no CasterDesk.';enable();return;}if(!posting){try{const r=await fetch('/api/state',{headers});if(!r.ok)throw new Error('HTTP '+r.status);render(await r.json());}catch{connected=false;$('connection').textContent=state?.labels.disconnected||'Desconectado';enable();}}setTimeout(poll,connected?1000:2000);}
window.addEventListener('unload',()=>{for(const url of icons.values())URL.revokeObjectURL(url);});poll();
