import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {fileURLToPath} from 'node:url';
import {execFile} from 'node:child_process';
import {promisify} from 'node:util';
const exec=promisify(execFile),root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'../..'),run=process.argv[2]||'mvpagentbe';
if(!/^[a-z0-9_-]{1,24}$/.test(run))throw new Error('Invalid run');
const folder=path.join(root,'.dm-acceptance/runs',run),read=n=>JSON.parse(fs.readFileSync(path.join(folder,n),'utf8').replace(/^\uFEFF/,''));
const p=read('mvp-fixtures.json').pairs.find(p=>p.case==='C02'),actor=read('manifest.json').accounts.find(a=>a.alias===p.peerAlias);
async function api(auth,method,url,body){const r=await fetch('http://localhost:15026/api/v1'+url,{method,headers:{'Content-Type':'application/json',...(auth?{Authorization:'Bearer '+auth.accessToken}:{})},body:body?JSON.stringify(body):undefined});assert(r.ok,'Expected real API success');return r.status===204?null:r.json();}
function keys(){const map={};for(const dir of ['cursor','hmac'])for(const name of fs.readdirSync(path.join(root,'.dm-acceptance/keyrings',dir))){const file=path.join(root,'.dm-acceptance/keyrings',dir,name);if(fs.statSync(file).isFile())map[dir+'/'+name]=crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');}return map;}
const auth=await api(null,'POST','/auth/login',{login:actor.username,password:'DmDemo2026!Local',deviceName:'MVP-restart-readonly'});
try{
 const keyBefore=keys(),url=`/direct-conversations/${p.conversationId}/messages`,before=await api(auth,'GET',url);
 await exec('docker',['compose','--project-name','scdc-dm-acceptance','--env-file','.env.dm-test','-f','compose.dm-test.yaml','restart','chat-service','web-client'],{cwd:root,timeout:120000});
 let ready=false;const end=Date.now()+30000;while(Date.now()<end){try{if((await fetch('http://localhost:15300/api/v1/health')).status===200){ready=true;break;}}catch{}await new Promise(r=>setTimeout(r,100));}assert(ready);
 const after=await api(auth,'GET',url);assert.deepEqual(after.items.map(m=>[m.id,m.version,m.sequence]),before.items.map(m=>[m.id,m.version,m.sequence]));assert.deepEqual(keys(),keyBefore);
 const oldCursor=await api(auth,'GET',url+'?after='+encodeURIComponent(before.resumeCursor));assert.equal(oldCursor.items.length,0);assert.equal(oldCursor.throughSequence,before.throughSequence);
 const proof={run,conversationId:p.conversationId,latestCount:after.items.length,through:after.throughSequence,idsVersionsUnchanged:true,keysUnchanged:true,oldCursorValid:true,result:'PASS real compose restart/API/proxy/keyring/cursor'};
 fs.writeFileSync(path.join(folder,'restart-smoke.json'),JSON.stringify(proof,null,2));console.log(JSON.stringify(proof));
}finally{await api(null,'POST','/auth/logout',{refreshToken:auth.refreshToken});}
