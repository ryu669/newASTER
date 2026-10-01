const assert=require('node:assert/strict');
const fs=require('node:fs');
const path=require('node:path');
const vm=require('node:vm');
const {pathToFileURL}=require('node:url');
const root=path.resolve(__dirname,'..');
const source=path.join(root,'game/art-source/slayer');
const context={};vm.runInNewContext(fs.readFileSync(path.join(source,'slayer-2d-motion.js'),'utf8'),context);
const {frameAt}=context.SlayerMotion;
assert.equal(frameAt(0,false).blink,false);
assert.equal(frameAt(3.85,false).blink,true);
assert.equal(frameAt(4,false).blink,false);
assert.equal(frameAt(0,false).talk,false);
assert.equal(frameAt(0,true).talk,true);
assert.equal(frameAt(.2,true).talk,false);
assert.equal(frameAt(NaN,false).blink,false);
console.log('7 timing assertions passed');
if(!process.env.SLAYER_PLAYWRIGHT_MODULE){console.log('Browser check skipped: set SLAYER_PLAYWRIGHT_MODULE to installed playwright module');process.exit(0)}
(async()=>{
 const {chromium}=require(process.env.SLAYER_PLAYWRIGHT_MODULE);
 const browser=await chromium.launch({channel:'msedge',headless:true});
 try{
 const page=await browser.newPage({viewport:{width:1400,height:1000}});
 const errors=[];page.on('pageerror',e=>errors.push(e.message));
 await page.goto(pathToFileURL(path.join(source,'slayer-2d-motion.html')).href);
 await page.getByRole('button',{name:'再生',exact:true}).waitFor();
 await page.waitForFunction(()=>!document.getElementById('play').disabled);
 const result=path.join(root,'game/Builds/portrait-motion');fs.mkdirSync(result,{recursive:true});
 await page.screenshot({path:path.join(result,'neutral.png')});
 await page.getByRole('button',{name:'閉眼を確認',exact:true}).click();
 await page.waitForFunction(()=>document.getElementById('status').textContent==='静止状態の確認');
 await page.screenshot({path:path.join(result,'blink.png')});
 await page.getByRole('button',{name:'口開きを確認',exact:true}).click();
 await page.waitForTimeout(80);
 await page.screenshot({path:path.join(result,'talk.png')});
 await page.getByRole('button',{name:'再生',exact:true}).click();
 await page.getByRole('button',{name:'会話 OFF',exact:true}).click();
 await page.waitForFunction(()=>document.getElementById('status').textContent==='再生中（無音）');
 await page.getByRole('button',{name:'一時停止',exact:true}).click();
 await page.getByRole('button',{name:'通常へ戻す',exact:true}).click();
 assert.equal(await page.getByRole('button',{name:'会話 OFF',exact:true}).getAttribute('aria-pressed'),'false');
 assert.deepEqual(errors,[]);
 console.log('BROWSER_PASS: load, blink, speaking, play/pause, reset; no page errors');
 }finally{await browser.close()}
})().catch(e=>{console.error(e);process.exitCode=1});
