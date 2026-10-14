// Run only against a disposable service SQLite database with one current Windows admin.
// node scripts/test-settings.cjs [playwright-module-path] [base-url] [screenshot-directory]
const assert = require('node:assert/strict');
const path = require('node:path');
const fs = require('node:fs');
const { chromium } = require(process.argv[2] || 'playwright');
(async () => {
    const browser = await chromium.launch({channel:'chrome', headless:true, args:['--auth-server-allowlist=localhost']});
    const base = process.argv[3] || 'http://localhost:5088';
    try {
        const context = await browser.newContext({viewport:{width:1280,height:1000}});
        const page = await context.newPage();
        const errors=[]; page.on('pageerror', e => errors.push(e.message));
        await page.goto(`${base}/settings?section=printing`);
        const add=page.getByRole('button',{name:'Добавить в список',exact:true});
        const activeHost=(target)=>target.locator('.saved-server:has(.role-badge--admin)').getAttribute('data-server');
        const ready=()=>page.waitForFunction(() => [...document.querySelectorAll('button')].some(b => b.textContent.includes('Добавить в список') && !b.disabled));
        await add.waitFor(); await ready();
        const previous=await activeHost(page);
        await page.locator('#global-print-server').fill('server/queue'); await add.click();
        await page.getByRole('alert').waitFor();
        await page.reload(); await add.waitFor(); await ready();
        assert.equal(await activeHost(page),previous);
        const testHost='print-list-browser-test';
        await page.locator('#global-print-server').fill(testHost); await add.click();
        await page.getByText('Сервер сохранён в списке. Его можно выбрать активным.',{exact:true}).waitFor();
        await page.locator('#global-print-server').fill(testHost.toUpperCase()); await add.click();
        await page.waitForFunction(() => ![...document.querySelectorAll('button')].find(b=>b.textContent==='Добавить в список')?.disabled);
        assert.equal(await page.locator(`.saved-server[data-server="${testHost}"]`).count(),1);
        if (await activeHost(page) === testHost) {
            await page.locator('#global-print-server').fill('print-list-browser-alt'); await add.click(); await ready();
            await page.locator('.saved-server[data-server="print-list-browser-alt"]').getByRole('button',{name:'Сделать активным',exact:true}).click();
            await page.getByText('Активный сервер печати изменён для всех пользователей.',{exact:true}).waitFor();
        }
        await page.locator(`.saved-server[data-server="${testHost}"]`).getByRole('button',{name:'Сделать активным',exact:true}).click();
        await page.getByText('Активный сервер печати изменён для всех пользователей.',{exact:true}).waitFor();
        await page.locator(`.saved-server[data-server="${testHost}"]`).getByText('Активный',{exact:true}).waitFor();
        assert.equal(await page.locator(`.saved-server[data-server="${testHost}"]`).getByRole('button',{name:'Сделать активным',exact:true}).count(),0);
        assert.equal(await page.locator('#active-print-server').count(),0);
        assert.equal(await page.locator(`.saved-server[data-server="${testHost}"]`).getByRole('button',{name:'Удалить из списка',exact:true}).isEnabled(),false);
        const second=await context.newPage(); await second.goto(`${base}/settings?section=printing`);
        await second.locator('.saved-server:has(.role-badge--admin)').waitFor();
        assert.equal(await activeHost(second),testHost);
        await page.locator('#global-print-server').fill('127.0.0.1');
        await page.getByRole('button',{name:'Проверить адрес',exact:true}).click();
        await page.getByRole('heading',{name:'Проверка: 127.0.0.1',exact:true}).waitFor({timeout:10000});
        assert.ok((await page.locator('.probe-panel').innerText()).includes('Ответ за'));
        assert.equal(await activeHost(page),testHost);
        assert.equal(await page.locator('.saved-server[data-server="127.0.0.1"]').count(),0);
        if (process.argv[4]) {fs.mkdirSync(process.argv[4],{recursive:true});await page.screenshot({path:path.join(process.argv[4],'print-servers-desktop.png'),fullPage:true});}
        await page.setViewportSize({width:390,height:844});
        assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth),'Print servers mobile overflow');
        if(process.argv[4]) await page.screenshot({path:path.join(process.argv[4],'print-servers-mobile.png'),fullPage:true});
        await page.setViewportSize({width:1280,height:1000});
        await page.evaluate(() => sessionStorage.setItem('diploma:printSource:v1', JSON.stringify({Mode:'Windows',ServerName:'old-browser-server',Scenario:'Default'})));
        await page.goto(`${base}/test-tools`);
        await page.locator('#print-source-mode').waitFor();
        assert.match(await page.locator('#print-source-mode').innerText(),/print-list-browser-test/);
        assert.doesNotMatch(await page.locator('#print-source-mode').innerText(),/old-browser-server/);
        await page.locator('#print-source-mode').selectOption('Fixture');
        await page.locator('#print-source-scenario').selectOption('Empty');
        await page.reload(); await page.locator('#print-source-scenario').waitFor();
        assert.equal(await page.locator('#print-source-scenario').inputValue(),'Empty');
        await page.goto(`${base}/settings?section=printing`); await add.waitFor(); await ready();
        await page.goto(`${base}/settings?section=users`);
        await page.waitForFunction(() => [...document.querySelectorAll("button")].some(b => b.textContent.includes("Назначить User") && !b.disabled));
        await page.getByRole('button',{name:'Назначить User',exact:true}).click();
        await page.getByText('Нельзя удалить или понизить права последнего администратора.',{exact:true}).waitFor();
        await page.getByRole('button',{name:'Удалить из приложения',exact:true}).click();
        await page.getByText('Нельзя удалить или понизить права последнего администратора.',{exact:true}).waitFor();
        await page.goto(`${base}/settings?section=database`);
        await page.waitForFunction(() => !document.querySelector("#sql-server")?.disabled);
        await page.locator('#sql-server').fill('127.0.0.1,1'); await page.locator('#sql-database').fill('missing');
        await page.getByRole('button',{name:'Сохранить подключение',exact:true}).click();
        await page.getByText('Подключение или схема БД не прошли проверку. Рабочие настройки сохранены.',{exact:true}).waitFor({timeout:20000});
        await page.reload(); await page.locator('#sql-server').waitFor();
        assert.notEqual(await page.locator('#sql-server').inputValue(),'127.0.0.1,1');
        await page.goto(`${base}/settings`);
        await page.getByRole("heading", {name:"Обзор",exact:true}).waitFor();
        assert.equal(await page.locator("#sql-server").count(),0);
        assert.equal(await page.locator("#global-print-server").count(),0);
        const nav=page.getByRole("navigation",{name:"Разделы настроек"});
        await nav.getByRole("link",{name:"База данных Подключение к SQL Server"}).click();
        await page.locator("#sql-server").waitFor();
        await nav.getByRole("link",{name:"Обзор Текущая конфигурация"}).click();
        await page.getByRole("heading",{name:"Обзор",exact:true}).waitFor();
        if (process.argv[4]) { fs.mkdirSync(process.argv[4],{recursive:true}); await page.screenshot({path:path.join(process.argv[4],'settings-desktop.png'),fullPage:true}); }
        await page.setViewportSize({width:390,height:844});
        assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth),'Mobile overflow');
        if(process.argv[4]) await page.screenshot({path:path.join(process.argv[4],'settings-mobile.png'),fullPage:true});
        assert.deepEqual(errors,[]);
        console.log('Passed: saved server list, deduplication, activation, lightweight loopback probe; global persistence across reload/tabs; old browser server ignored; fixture scenarios; last-admin guards; invalid SQL rejected; mobile layout; no page errors.');
    } finally { await browser.close(); }
})().catch(e=>{ console.error(e); process.exitCode=1; });
