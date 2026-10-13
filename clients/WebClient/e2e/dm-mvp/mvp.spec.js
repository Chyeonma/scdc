import { test, expect } from '@playwright/test';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { execFile } from 'node:child_process';
import { promisify } from 'node:util';
const exec=promisify(execFile), root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'../../../..');
const run=process.env.DM_MVP_RUN||'mvpagenta', folder=path.join(root,'.dm-acceptance/runs',run);
const read=name=>JSON.parse(fs.readFileSync(path.join(folder,name),'utf8').replace(/^\uFEFF/,''));
const users=read('manifest.json').accounts, fixture=read('mvp-fixtures.json');
const user=alias=>users.find(u=>u.alias===alias), pair=id=>fixture.pairs.find(p=>p.case===id);
const contexts=[], sessions=[];
const button=(page,name)=>page.getByRole('button',{name,exact:true});
const draft=page=>page.getByRole('textbox',{name:'Nội dung tin nhắn',exact:true});
const rows=page=>page.locator('.dm-text-message');
const source=name=>fs.readFileSync(path.join(root,'scripts/dm-acceptance',name),'utf8');
const script=async(action,id,extras=[])=> (await exec('powershell.exe',['-NoProfile','-ExecutionPolicy','Bypass','-File',path.join(root,'scripts/dm-acceptance/mvp.ps1'),'-Action',action,'-Run',run,'-Case',id,...extras],{cwd:root,encoding:'utf8',timeout:150000})).stdout;
const snap=async id=>JSON.parse(await script('Snapshot',id));
function proof(id,data){fs.writeFileSync(path.join(folder,'mvp-browser-'+id+'.json'),JSON.stringify({run,case:id,...data,result:'PASS actual browser/API/WebSocket/PostgreSQL',userResult:'Chua xac nhan'},null,2));}
async function login(page,alias){
 await page.addInitScript({content:source('realtime-faults.js')});await page.goto('/#home');
 await page.locator('input[name="login"]').fill(user(alias).username);await page.locator('input[name="password"]').fill('DmDemo2026!Local');
 const pending=page.waitForResponse(r=>r.url().endsWith('/api/v1/auth/login')&&r.request().method()==='POST');
 await button(page,'Đăng nhập').last().click();const auth=await(await pending).json();sessions.push(auth);
 await expect(page.locator('.user-dock__tag')).toHaveText('@'+user(alias).username);return auth;
}
async function open(page,peer,id){
 await page.evaluate(id=>window.dmHubFixture.on({conversationId:id}),id);
 await page.getByTitle('Tạo cuộc trò chuyện trực tiếp (DM)',{exact:true}).click();
 await page.getByLabel('Tên tài khoản hoặc tên hiển thị',{exact:true}).fill(user(peer).username);
 await page.locator('.dm-search__result').filter({hasText:new RegExp('@'+user(peer).username+'\\s*$')}).click();
 const opened=page.waitForResponse(r=>r.request().method()==='POST'&&new URL(r.url()).pathname==='/api/v1/direct-conversations');
 await button(page,'Mở hội thoại').click();expect((await(await opened).json()).id).toBe(id);
 await expect(draft(page)).toHaveAttribute('placeholder','Nhắn tin cho '+user(peer).displayName);
 await expect(page.locator('.dm-connection-state')).toHaveAttribute('data-state','online',{timeout:20000});
}
async function clients(page,browser,id){
 const p=pair(id);const context=await browser.newContext({baseURL:'http://localhost:15300'});contexts.push(context);const b=await context.newPage();
 const aa=await login(page,'A'),bb=await login(b,p.peerAlias);await open(b,'A',p.conversationId);await open(page,p.peerAlias,p.conversationId);
 const before=await snap(id);expect(before.messages).toBe(0);return{p,b,aa,bb,before,context};
}
async function send(page,p,text){
 const pending=page.waitForResponse(r=>r.request().method()==='POST'&&new URL(r.url()).pathname.endsWith(`/${p.conversationId}/messages`),{timeout:15000});
 await draft(page).fill(text);await button(page,'Gửi').click();const r=await pending;expect(r.status()).toBe(200);return await r.json();
}
async function edit(page,mid,text,status=200){
 const row=page.locator(`[data-message-id="${mid}"]`);await button(row,'Sửa').click();await page.getByLabel('Sửa nội dung tin nhắn',{exact:true}).fill(text);
 const pending=page.waitForResponse(r=>r.request().method()==='PATCH'&&new URL(r.url()).pathname.endsWith('/'+mid));
 await button(row,'Lưu thay đổi').click();const r=await pending;expect(r.status()).toBe(status);return await r.json();
}
async function requestApi(request,auth,method,url,body){
 const r=await request.fetch('/api/v1'+url,{method,headers:{Authorization:'Bearer '+auth.accessToken},data:body});
 return{status:r.status(),body:r.status()===204?null:await r.json()};
}
async function offline(page){await page.context().setOffline(true);await page.evaluate(()=>window.dmHubFixture.disconnect());await expect(page.locator('.dm-connection-state')).toHaveAttribute('data-state','offline');}
async function online(page){await page.context().setOffline(false);await expect(page.locator('.dm-connection-state')).toHaveAttribute('data-state','online',{timeout:30000});}
test.afterEach(async({request,page})=>{
 await page.evaluate(()=>window.dmHubFixture?.off()).catch(()=>{});
 for(const auth of sessions.splice(0))expect((await request.post('/api/v1/auth/logout',{data:{refreshToken:auth.refreshToken}})).status()).toBe(204);
 for(const c of contexts.splice(0))await c.close();
});

test('C01 full search/open/send/reload happy path and actual outsider REST denials',async({page,browser,request})=>{
 const{p,b,aa,before}=await clients(page,browser,'C01');let reads=0;b.on('request',r=>{if(r.method()==='GET'&&new URL(r.url()).pathname.endsWith(`/${p.conversationId}/messages`))reads++;});
 const dto=await send(page,p,'MVP-M01');await expect(b.locator(`[data-message-id="${dto.id}"]`)).toContainText('MVP-M01');expect(reads).toBe(0);
 const outsider=await login(await (async()=>{const c=await browser.newContext({baseURL:'http://localhost:15300'});contexts.push(c);return c.newPage();})(),'S23');
 const denied=await requestApi(request,outsider,'GET',`/direct-conversations/${p.conversationId}/messages`);expect(denied.status).toBe(404);expect(denied.body.errorCode).toBe('RESOURCE_NOT_FOUND');
 await b.reload();await expect(rows(b)).toHaveCount(1);await page.reload();await expect(rows(page)).toHaveCount(1);
 const after=await snap('C01');expect(after.messages).toBe(1);expect(after.operations).toBe(1);expect(after.createOutbox).toBe(1);proof('C01',{p,before,after,messageId:dto.id,receiverHistoryReadsBeforeReload:0,outsiderStatus:404});
});

for(const id of ['C02','C02P1','C02P2','C02LIVE'])test(`${id} actual automatic recovery101 with fixed through and interruption/live frame`,async({page,browser,request})=>{
 const{p,b,aa,before}=await clients(page,browser,id);await offline(b);await script('Seed101',id);
 const seeded=read('seed-'+id+'.json');let count=0,waiting,block;const reached=new Promise(r=>waiting=r);let interrupted=false;const pages=[];let live;
 await b.route(`**/api/v1/direct-conversations/${p.conversationId}/messages?*`,async route=>{
  const url=new URL(route.request().url());if(!url.searchParams.has('after')){await route.continue();return;}
  count++;
  const at=id==='C02P1'?2:id==='C02P2'?3:0;
  if(at&&count===at&&!interrupted){interrupted=true;block=route;waiting();return;}
  const response=await route.fetch();const dto=await response.json();pages.push({items:dto.items.length,through:dto.throughSequence,resume:!!dto.resumeCursor});
  if(id==='C02LIVE'&&count===1){
   live=await requestApi(request,aa,'POST',`/direct-conversations/${p.conversationId}/messages`,{clientMessageId:p.X,content:'RC-102'});expect(live.status).toBe(200);
   await expect.poll(()=>b.evaluate(mid=>window.dmHubFixture.stats().actual.some(e=>e.id===mid),live.body.id),{timeout:15000}).toBe(true);
  }
  await route.fulfill({response});
 });
 await b.context().setOffline(false);
 if(id==='C02P1'||id==='C02P2'){
  await reached;await expect(rows(b)).toHaveCount(id==='C02P1'?50:100);
  await b.context().setOffline(true);await b.evaluate(()=>window.dmHubFixture.disconnect());await block.abort('internetdisconnected');await b.context().setOffline(false);
 }
 await expect(b.locator('.dm-connection-state')).toHaveAttribute('data-state','online',{timeout:40000});
 const expectedIds=[...seeded.receipts.map(r=>r.id),...(live?[live.body.id]:[])];await expect(rows(b)).toHaveCount(expectedIds.length);
 expect(await rows(b).evaluateAll(nodes=>nodes.map(n=>n.dataset.messageId))).toEqual(expectedIds);
 expect(pages.map(p=>p.items)).toEqual([50,50,1]);expect(pages.map(p=>p.through)).toEqual(['101','101','101']);expect(pages.map(p=>p.resume)).toEqual([false,false,true]);
 await expect(button(b,'Tải tin cũ hơn')).toHaveCount(0);const after=await snap(id);expect(after.messages).toBe(live?102:101);proof(id,{p,before,after,pages,idsMatchWriter:true,interrupted,liveBeforeFirstPageRelease:!!live});
});

test('C03 actual UI edit twice/no-op/cancel/bound validation, recipient gets current version',async({page,browser})=>{
 const{p,b,before}=await clients(page,browser,'C03');const dto=await send(page,p,'EDIT-ORIGINAL');await expect(rows(b)).toHaveCount(1);
 await edit(page,dto.id,'EDIT-V2');await expect(rows(b)).toHaveAttribute('data-version','2');await edit(page,dto.id,'EDIT-V3');await expect(rows(b)).toContainText('EDIT-V3');await expect(rows(b)).toContainText('Đã sửa');
 const beforeNoop=await snap('C03');await edit(page,dto.id,'EDIT-V3');expect(await snap('C03')).toEqual(beforeNoop);
 await button(rows(page),'Sửa').click();await page.getByLabel('Sửa nội dung tin nhắn').fill('CANCELLED');await button(page,'Hủy sửa').click();await expect(rows(page)).toContainText('EDIT-V3');
 await button(rows(page),'Sửa').click();await page.getByLabel('Sửa nội dung tin nhắn').fill('a'.repeat(2001));await button(page,'Lưu thay đổi').click();await expect(page.getByRole('alert').filter({hasText:'2.000'})).toBeVisible();await button(page,'Hủy sửa').click();
 expect(await snap('C03')).toEqual(beforeNoop);await expect(button(rows(b),'Sửa')).toHaveCount(0);proof('C03',{p,before,after:await snap('C03'),noopAndValidationUnchanged:true});
});

test('C04 actual delete confirmation/cancel/repeated delete/original-send replay never resurrect',async({page,browser,request})=>{
 const{p,b,aa,before}=await clients(page,browser,'C04');const dto=await send(page,p,'DELETE-ORIGINAL');await expect(rows(b)).toHaveCount(1);
 await button(rows(page),'Xóa').click();await button(page,'Hủy xóa').click();await expect(rows(page)).toContainText('DELETE-ORIGINAL');
 await button(rows(page),'Xóa').click();await button(page,'Xác nhận xóa').click();await expect(rows(b)).toContainText('Tin nhắn đã bị xóa');await expect(rows(page)).toHaveAttribute('data-version','2');
 const first=await snap('C04');const repeat=await requestApi(request,aa,'DELETE',`/direct-conversations/${p.conversationId}/messages/${dto.id}?expectedVersion=1`);expect(repeat.status).toBe(200);
 const replay=await requestApi(request,aa,'POST',`/direct-conversations/${p.conversationId}/messages`,{clientMessageId:dto.clientMessageId,content:'DELETE-ORIGINAL'});expect(replay.status).toBe(200);expect(replay.body.id).toBe(dto.id);expect(replay.body.content).toBeNull();
 expect(await snap('C04')).toEqual(first);await b.reload();await expect(rows(b)).toHaveCount(1);await expect(rows(b)).not.toContainText('DELETE-ORIGINAL');proof('C04',{p,before,after:first,replayStatus:200,deleted:true});
});

for(const id of ['EDITOLD','DELETEOLD'])test(`${id} offline old message changes through production API then101 recovery refreshes old window`,async({page,browser})=>{
 const{p,b,before}=await clients(page,browser,id);const dto=await send(page,p,'OLD-M01');await expect(rows(b)).toHaveCount(1);await offline(b);
 if(id==='EDITOLD')await edit(page,dto.id,'CURRENT-OLD-M01');else{await button(rows(page),'Xóa').click();await button(page,'Xác nhận xóa').click();}
 await script('Seed101',id);await online(b);await expect(rows(b)).toHaveCount(102);const old=b.locator(`[data-message-id="${dto.id}"]`);
 await expect(old).toHaveAttribute('data-version','2');await expect(old).toContainText(id==='EDITOLD'?'CURRENT-OLD-M01':'Tin nhắn đã bị xóa');
 const all=read('seed-'+id+'.json');expect(await rows(b).evaluateAll(n=>n.map(x=>x.dataset.messageId))).toEqual([dto.id,...all.receipts.map(x=>x.id)]);
 await b.evaluate(mid=>window.dmHubFixture.duplicate({messageId:mid,version:'1',content:'STALE-LOCAL'}),dto.id);await expect(old).not.toContainText('STALE-LOCAL');proof(id,{p,before,after:await snap(id),productionMutation:true,oldVersion:'2',total:102});
});

test('C05 two actual A sessions submit stale edit and stale delete, conflict reloads current UI',async({page,browser})=>{
 const{p,before}=await clients(page,browser,'C05');const dto=await send(page,p,'RACE-ORIGINAL');
 const c=await browser.newContext({baseURL:'http://localhost:15300'});contexts.push(c);const a2=await c.newPage();await login(a2,'A');await open(a2,p.peerAlias,p.conversationId);
 await a2.evaluate(id=>window.dmHubFixture.on({conversationId:id,dropEvents:true}),p.conversationId);
 await button(rows(a2),'Sửa').click();await a2.getByLabel('Sửa nội dung tin nhắn').fill('RACE-LOSER');await edit(page,dto.id,'RACE-WINNER');
 const pending=a2.waitForResponse(r=>r.request().method()==='PATCH');await button(a2,'Lưu thay đổi').click();expect((await pending).status()).toBe(409);await expect(rows(a2)).toContainText('RACE-WINNER');
 await button(rows(a2),'Xóa').click();await edit(page,dto.id,'RACE-WINNER-3');const staleDelete=a2.waitForResponse(r=>r.request().method()==='DELETE');await button(a2,'Xác nhận xóa').click();expect((await staleDelete).status()).toBe(409);await expect(rows(a2)).toContainText('RACE-WINNER-3');
 const after=await snap('C05');expect(after.messages).toBe(1);expect(after.outbox).toBe(3);proof('C05',{p,before,after,staleEditStatus:409,staleDeleteStatus:409});
});

test('C08 exact old session revoked via settings UI and conservative request-to-clear cutoff <=5s',async({page,browser,request})=>{
 const{p,aa,before}=await clients(page,browser,'C08');const c=await browser.newContext({baseURL:'http://localhost:15300'});contexts.push(c);const a2=await c.newPage();const auth2=await login(a2,'A');await open(a2,p.peerAlias,p.conversationId);
 const sid=JSON.parse(Buffer.from(auth2.accessToken.split('.')[1],'base64url').toString()).sid;
 await button(page,'Cài đặt').click();await page.getByRole('button',{name:/Phiên đăng nhập/}).click();const card=page.locator(`[data-session-id="${sid}"]`);
 const started=Date.now();const revoked=page.waitForResponse(r=>r.request().method()==='DELETE'&&new URL(r.url()).pathname.endsWith('/sessions/'+sid));
 await button(card,'Thu hồi').click();expect((await revoked).status()).toBe(204);const commitConfirmedAt=Date.now();await expect(a2.locator('input[name="login"]')).toBeVisible({timeout:5000});const clearedAt=Date.now();expect(clearedAt-started).toBeLessThanOrEqual(5000);
 const sent=await requestApi(request,aa,'POST',`/direct-conversations/${p.conversationId}/messages`,{clientMessageId:p.X,content:'AFTER-REVOKE'});expect(sent.status).toBe(200);
 const old=await request.post('/hubs/chat/negotiate?negotiateVersion=1',{headers:{Authorization:'Bearer '+auth2.accessToken}});expect(old.status()).toBe(401);
 expect((await a2.evaluate(()=>window.dmHubFixture.stats())).actual).toHaveLength(0);
 proof('C08',{p,before,after:await snap('C08'),sessionId:sid,requestStartedAt:started,commitConfirmedAt,clearedAt,conservativeCutoffMs:clearedAt-started,oldNegotiation:401,oldSessionReceived:0});
});

test('C09 desktop composition/draft switch/reload have no unexpected writer',async({page,browser})=>{
 const{p,before}=await clients(page,browser,'C09');await draft(page).fill('Tiếng Việt 👩‍💻');let posts=0;page.on('request',r=>{if(r.method()==='POST'&&new URL(r.url()).pathname.endsWith('/messages'))posts++;});
 await draft(page).evaluate(el=>{el.dispatchEvent(new CompositionEvent('compositionstart',{bubbles:true}));el.dispatchEvent(new KeyboardEvent('keydown',{key:'Enter',bubbles:true,isComposing:true}));el.dispatchEvent(new CompositionEvent('compositionend',{bubbles:true}));});expect(posts).toBe(0);
 await draft(page).press('Shift+Enter');await expect(draft(page)).toHaveValue('Tiếng Việt 👩‍💻\n');
 await open(page,pair('C01').peerAlias,pair('C01').conversationId);await draft(page).fill('DRAFT-OTHER');await open(page,p.peerAlias,p.conversationId);await expect(draft(page)).toHaveValue('Tiếng Việt 👩‍💻\n');
 await draft(page).press('Enter');await expect(rows(page)).toHaveCount(1);await expect(draft(page)).toHaveValue('');expect(posts).toBe(1);
 await draft(page).fill('RAM-ONLY');await page.reload();await expect(draft(page)).toHaveValue('');const after=await snap('C09');expect(after.messages).toBe(1);proof('C09',{p,before,after,compositionPosts:0,desktopEnterPosts:1,draftReloadEmpty:true});
});

test('MOBILE actual touch/coarse browser Enter adds newline and explicit Send persists',async({browser})=>{
 const p=pair('MOBILE'),c=await browser.newContext({baseURL:'http://localhost:15300',viewport:{width:390,height:844},isMobile:true,hasTouch:true});contexts.push(c);const page=await c.newPage();await login(page,'A');await open(page,p.peerAlias,p.conversationId);
 expect(await page.evaluate(()=>matchMedia('(pointer: coarse)').matches)).toBe(true);await draft(page).fill('MOBILE-LINE');await draft(page).press('Enter');await expect(draft(page)).toHaveValue('MOBILE-LINE\n');expect((await snap('MOBILE')).messages).toBe(0);
 await button(page,'Gửi').click();await expect(rows(page)).toHaveCount(1);proof('MOBILE',{p,after:await snap('MOBILE'),type:'Actual Edge touch/coarse emulation, not a physical keyboard/device'});
});

test('C10 disabled K cannot receive/send; A still reads/edits/deletes and original retry returns current',async({page,browser,request})=>{
 const{p,b,aa,bb,before}=await clients(page,browser,'C10');const dto=await send(page,p,'K-ORIGINAL');await expect(rows(b)).toHaveCount(1);
 try{
  await script('State','C10',['-Alias','K','-State','Disabled']);await expect(b.locator('input[name="login"]')).toBeVisible({timeout:5000});await button(page,'Làm mới hội thoại').click();await expect(button(page,'Gửi')).toBeDisabled();
  await edit(page,dto.id,'K-CURRENT');await button(rows(page),'Xóa').click();await button(page,'Xác nhận xóa').click();await expect(rows(page)).toContainText('Tin nhắn đã bị xóa');
  const replay=await requestApi(request,aa,'POST',`/direct-conversations/${p.conversationId}/messages`,{clientMessageId:dto.clientMessageId,content:'K-ORIGINAL'});expect(replay.status).toBe(200);expect(replay.body.content).toBeNull();
  const denied=await requestApi(request,aa,'POST',`/direct-conversations/${p.conversationId}/messages`,{clientMessageId:p.Y,content:'NEW-BLOCKED'});expect(denied.status).toBe(404);
  await script('State','C10',['-Alias','K','-State','Unlock']);const old=await request.get('/api/v1/users/me',{headers:{Authorization:'Bearer '+bb.accessToken}});expect(old.status()).toBe(401);
  const after=await snap('C10');expect(after.messages).toBe(1);expect(after.outbox).toBe(3);proof('C10',{p,before,after,retryCurrent:200,newBlocked:404,oldSessionAfterUnlock:401});
 }finally{await script('State','C10',['-Alias','K','-State','Unlock']);}
});
