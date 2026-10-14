// Start DiplomaTry2.GridChecks, then run: node scripts/test-devexpress-grid.cjs [playwright-module-path] [base-url]
const assert = require('node:assert/strict');
const { chromium } = require(process.argv[2] || 'playwright');

(async () => {
    const browser = await chromium.launch({ channel: process.env.GRID_TEST_BROWSER || 'chrome', headless: true });
    try {
        const page = await browser.newPage({ viewport: { width: 1280, height: 1000 } });
        const errors = [];
        page.on('pageerror', error => errors.push(error.message));
        await page.goto(process.argv[3] || 'http://127.0.0.1:5098');
        const data = field => page.locator(`tbody [data-grid-field="${field}"]`);
        const header = field => page.locator(`thead [data-grid-field="${field}"]`).first();
        await data('Name').first().waitFor({ timeout: 20000 });
        assert.equal(await data('Name').first().innerText(), 'Printer 01');
        assert.equal(await page.locator('tbody img').count(), 0, 'Data must render as plain text');
        assert.equal(await page.locator('.print-grid__data thead input').count(), 0, 'Display grids must not contain filter editors');
        assert.equal(await page.getByPlaceholder('Поиск по таблице…').count(), 0, 'Search panel must be hidden');
        assert.equal(await header('Queue').count(), 0, 'Queue must be hidden by default');
        await page.getByRole('button', { name: 'Поиск', exact: true }).click();
        await page.getByPlaceholder('Поиск по таблице…').fill('Printer 02');
        await page.waitForFunction(() => document.querySelectorAll('tbody [data-grid-field="Name"]').length === 1);
        await page.reload();
        await data('Name').first().waitFor();
        assert.equal(await data('Name').first().innerText(), 'Printer 02', 'Common search must survive reload');
        assert.equal(await page.getByPlaceholder('Поиск по таблице…').inputValue(), 'Printer 02');
        await page.getByRole('button', { name: 'Скрыть поиск', exact: true }).click();
        await page.waitForFunction(() => document.querySelectorAll('tbody [data-grid-field="Name"]').length === 25);
        assert.equal(await page.getByPlaceholder('Поиск по таблице…').count(), 0);
        await page.getByRole('button', { name: 'Поиск', exact: true }).click();
        await page.getByPlaceholder('Поиск по таблице…').fill('Long document');
        await page.waitForFunction(() => document.querySelector('tbody [data-grid-field="Name"]').textContent.trim() === 'Printer 02');
        await page.getByRole('button', { name: 'Скрыть поиск', exact: true }).click();
        await page.waitForFunction(() => document.querySelector('tbody [data-grid-field="Name"]').textContent.trim() === 'Printer 01');
        const widths = () => page.locator('thead [data-grid-field]').evaluateAll(cells =>
            cells.map(cell => cell.getBoundingClientRect().width));
        const before = await widths();
        if (process.argv[4]) await page.screenshot({ path: process.argv[4], fullPage: true });
        async function bodyResize(delta) {
            const initial = await widths();
            const box = await data('Name').first().boundingBox();
            await page.mouse.move(box.x + box.width - 2, box.y + box.height / 2);
            await page.mouse.down();
            await page.mouse.move(box.x + box.width - 2 + delta, box.y + box.height / 2, { steps: 5 });
            await page.mouse.up();
            await page.waitForFunction(({ initial, delta }) => {
                const cell = document.querySelector('thead [data-grid-field="Name"]');
                return Math.abs(cell.getBoundingClientRect().width - initial[0] - delta) < 3;
            }, { initial, delta });
            const resized = await widths();
            assert.ok(Math.abs(resized[1] - initial[1] + delta) < 3, 'Neighbour width must compensate');
            assert.ok(Math.abs(resized.reduce((a, b) => a + b) - initial.reduce((a, b) => a + b)) < 3);
            return resized;
        }
        await bodyResize(40);
        await header('Pages').click();
        await page.waitForFunction(() => document.querySelector('tbody [data-grid-field="Pages"]').textContent.trim() === '1');
        const sortedWidths = await bodyResize(-20);
        await page.click('#reload-data');
        await page.waitForFunction(() => document.querySelector('tbody [data-grid-field="Pages"]').textContent.trim() === '1');
        assert.deepEqual(await widths(), sortedWidths, 'Data reload must keep widths');
        const savedKey = await page.evaluate(() => Object.keys(localStorage).find(key => key.startsWith('diploma:grid:v2:check-grid:')));
        assert.ok(savedKey, 'Layout must be persisted');
        const legacyLayout = await page.evaluate(key => JSON.parse(localStorage.getItem(key)), savedKey);
        await page.reload();
        await data('Name').first().waitFor();
        assert.equal(await data('Pages').first().innerText(), '1', 'Sorting must survive page reload');
        const restored = await widths();
        restored.forEach((width, index) => assert.ok(Math.abs(width - sortedWidths[index]) < 3, 'Widths must survive page reload'));
        await bodyResize(10);
        await page.getByRole('button', { name: 'Сбросить настройки таблицы' }).click();
        await page.waitForFunction(() => document.querySelector('tbody [data-grid-field="Name"]').textContent.trim() === 'Printer 01');
        const reset = await widths();
        reset.forEach((width, index) => assert.ok(Math.abs(width - before[index]) < 3, 'Reset must restore initial widths'));
        const assertFilled = () => page.waitForFunction(() => {
            const cells = Array.from(document.querySelectorAll('thead [data-grid-field]'));
            const row = document.querySelector('thead tr');
            return cells.length > 0 && Math.abs(cells.reduce((sum, cell) => sum + cell.getBoundingClientRect().width, 0) - row.getBoundingClientRect().width) < 3;
        });
        await bodyResize(20);
        await page.getByRole('button', { name: 'Колонки', exact: true }).click();
        await page.getByRole('checkbox', { name: 'Документ', exact: true }).uncheck();
        await page.keyboard.press('Escape');
        await page.waitForFunction(() => !document.querySelector('thead [data-grid-field="Document"]'));
        await assertFilled();
        await page.reload();
        await data('Name').first().waitFor();
        assert.equal(await header('Document').count(), 0, 'Hidden column must stay hidden after reload');
        await assertFilled();
        await page.getByRole('button', { name: 'Колонки', exact: true }).click();
        await page.getByRole('checkbox', { name: 'Документ', exact: true }).check();
        await page.getByRole('checkbox', { name: 'Очередь', exact: true }).check();
        await page.keyboard.press('Escape');
        await header('Queue').waitFor();
        await assertFilled();
        await page.getByRole('button', { name: 'Сбросить настройки таблицы' }).click();
        await page.waitForFunction(() => !document.querySelector('thead [data-grid-field="Queue"]'));
        await page.getByText('2', { exact: true }).click();
        await page.waitForFunction(() => document.querySelector('tbody [data-grid-field="Name"]').textContent.trim() === 'Printer 26');
        await page.reload();
        await data('Name').first().waitFor();
        assert.equal(await data('Name').first().innerText(), 'Printer 26', 'Page index must survive reload');
        await bodyResize(10);
        await page.evaluate(key => localStorage.setItem(key, '{invalid-json'), savedKey);
        await page.reload();
        await data('Name').first().waitFor();
        assert.ok((await page.locator('.print-grid__notice').innerText()).includes('повреждены'));
        assert.equal(await data('Name').first().innerText(), 'Printer 01');
        await page.evaluate(({ key, layout }) => {
            layout.SearchText = 'Printer 02';
            layout.FilterCriteria = '[Pages] = 59';
            for (const column of layout.Columns) {
                if (column.FieldName === 'Queue') column.Visible = true;
                if (column.FieldName === 'Name') column.GroupIndex = 0;
            }
            localStorage.removeItem(key);
            localStorage.setItem(key.replace(':v2:', ':v1:'), JSON.stringify(layout));
        }, { key: savedKey, layout: legacyLayout });
        await page.reload();
        await data('Name').first().waitFor();
        assert.equal(await data('Pages').first().innerText(), '1', 'Migration must retain sorting');
        assert.equal(await data('Name').count(), 25, 'Invisible legacy filters and groups must be cleared');
        assert.equal(await header('Queue').count(), 0, 'Migration must apply default hidden queue');
        await assertFilled();
        assert.deepEqual(errors, [], 'No browser exceptions');
        console.log('Passed: display-only grids, default hidden queue, column chooser fill after hide/show/reload, body resize, sorting, persistent state, reset and corrupt-state recovery.');
    } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
