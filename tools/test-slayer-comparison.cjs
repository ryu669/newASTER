const assert=require('node:assert/strict'),path=require('node:path'),fs=require('node:fs');
const {pathToFileURL}=require('node:url');
const {chromium}=require(process.env.SLAYER_PLAYWRIGHT_MODULE);
(async()=>{const browser=await chromium.launch({channel:'msedge',headless:true});try{
 const page=await browser.newPage({viewport:{width:1440,height:1200}}),errors=[];
 page.on('pageerror',e=>errors.push(e.message));
 await page.goto(pathToFileURL(path.resolve(__dirname,'../game/art-source/slayer/slayer-2d-3d-comparison.html')).href);
 await page.waitForFunction(()=>document.getElementById('status').textContent.includes('比較できます'));
 const out=path.resolve(__dirname,'../game/Builds/2d-3d-comparison');fs.mkdirSync(out,{recursive:true});
 const hp=side=>page.locator('#party-'+side+' .member span').allTextContents();
 assert.equal(await page.locator('canvas').count(),2);assert.equal(await page.locator('.member').count(),10);
 await page.screenshot({path:path.join(out,'face.png'),fullPage:true});
 for(const expression of ['closed','talk']){await page.locator('#expression').selectOption(expression);await page.screenshot({path:path.join(out,expression+'.png'),fullPage:true})}
 await page.locator('#view').selectOption('full');await page.screenshot({path:path.join(out,'full.png'),fullPage:true});
 await page.locator('#count').selectOption('3');await page.locator('[data-action=heal]').click();
 await page.waitForTimeout(100);await page.locator('#pause').click();const frozen=await hp('two');await page.waitForTimeout(400);assert.deepEqual(await hp('two'),frozen);await page.locator('#pause').click();
 await page.waitForFunction(()=>!document.querySelector('[data-action=heal]').disabled);
 assert.deepEqual(await hp('two'),['HP 820','HP 610','HP 790','HP 440','HP 880']);assert.deepEqual(await hp('two'),await hp('three'));
 await page.locator('[data-action=chain]').click();await page.waitForFunction(()=>document.querySelector('.actor').textContent==='03');assert.deepEqual(await page.locator('.actor').allTextContents(),['03','03']);
 await page.locator('#reset').click();assert.deepEqual(await hp('two'),['HP 640','HP 430','HP 790','HP 260','HP 880']);
 await page.locator('[data-action=cast]').click();await page.waitForFunction(()=>document.getElementById('status').textContent.startsWith('発動'));await page.waitForFunction(()=>!document.querySelector('[data-action=cast]').disabled);
 await page.locator('#view').selectOption('face');await page.locator('#expression').selectOption('neutral');
 await page.setViewportSize({width:390,height:844});assert.equal(await page.evaluate(()=>document.documentElement.scrollWidth),390);await page.screenshot({path:path.join(out,'mobile.png'),fullPage:true});
 assert.deepEqual(errors,[]);console.log('COMPARISON_PASS: assets, face/full/expressions, matched HP, pause, chain actors, cast phases, reset, 390px layout');
 }finally{await browser.close()}})().catch(e=>{console.error(e);process.exitCode=1});
