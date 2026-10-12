import {chromium} from '../../clients/WebClient/node_modules/playwright/index.mjs';
import fs from 'node:fs';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
import assert from 'node:assert/strict';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'../..'),run=process.argv[2]||'mvpuser';
if(!/^[a-z0-9_-]{1,24}$/.test(run))throw new Error('Invalid run');
const folder=path.join(root,'.dm-acceptance/runs',run),read=n=>JSON.parse(fs.readFileSync(path.join(folder,n),'utf8').replace(/^\uFEFF/,''));
const users=read('manifest.json').accounts,p=read('mvp-fixtures.json').pairs.find(p=>p.case==='C01');
const browser=await chromium.launch({channel:'msedge',headless:true}),results=[];
try{for(const[alias,peer]of[['A','B'],['B','A']]){
 const context=await browser.newContext({baseURL:'http://localhost:15300'}),page=await context.newPage();let auth;
 try{
  const u=users.find(u=>u.alias===alias),other=users.find(u=>u.alias===peer);await page.goto('/');await page.locator('input[name="login"]').fill(u.username);await page.locator('input[name="password"]').fill('DmDemo2026!Local');
  const login=page.waitForResponse(r=>r.url().endsWith('/api/v1/auth/login')&&r.request().method()==='POST');await page.getByRole('button',{name:'Đăng nhập',exact:true}).last().click();auth=await(await login).json();
  await page.getByTitle('Tạo cuộc trò chuyện trực tiếp (DM)',{exact:true}).click();await page.getByLabel('Tên tài khoản hoặc tên hiển thị').fill(other.username);await page.locator('.dm-search__result').filter({hasText:new RegExp('@'+other.username+'\\s*$')}).click();await page.getByRole('button',{name:'Mở hội thoại',exact:true}).click();
  await page.waitForFunction(()=>document.querySelector('.dm-connection-state')?.dataset.state==='online');assert.equal(await page.locator('.dm-text-message').count(),0);assert.equal(await page.getByLabel('Nội dung tin nhắn',{exact:true}).isVisible(),true);
  results.push({alias,username:u.username,peer:other.username,conversationId:p.conversationId,rows:0,composerVisible:true,connection:'online'});
 }finally{if(auth)await page.request.post('/api/v1/auth/logout',{data:{refreshToken:auth.refreshToken}});await context.close();}
}
 fs.writeFileSync(path.join(folder,'handover-browser-smoke.json'),JSON.stringify({run,results,noMessageWrites:true,result:'PASS actual handover FE search/open/empty composer'},null,2));console.log(JSON.stringify({run,result:'PASS actual handover FE',rows:0,actors:results.map(r=>r.username)}));
}finally{await browser.close();}
