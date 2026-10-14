(() => {
    const minimumWidth = 64;
    const initialized = new Map();
    const savedWidths = new Map();

    function initialize(table) {
        if (initialized.has(table)) return;
        const headers = Array.from(table.tHead?.rows[0]?.cells ?? []);
        if (headers.length < 2 || headers.some(header => header.colSpan !== 1)) return;
        if (!table.getBoundingClientRect().width) return;

        const tables = Array.from(document.querySelectorAll('.app-shell__content table.table'));
        const key = `${location.pathname}:${tables.indexOf(table)}:${headers.map(header => header.textContent.trim()).join('|')}`;
        const widths = savedWidths.get(key)?.slice() ?? headers.map(header => Math.max(minimumWidth, header.getBoundingClientRect().width));
        table.classList.add('table--resizable');

        function applyWidths() {
            headers.forEach((header, index) => {
                header.style.width = `${widths[index]}px`;
                const handle = header.querySelector('.table-column-resize');
                if (handle) {
                    handle.setAttribute('aria-valuenow', Math.round(widths[index]));
                    handle.setAttribute('aria-valuemax', Math.round(widths[index] + widths[index + 1] - minimumWidth));
                }
            });
            table.style.width = `${widths.reduce((sum, width) => sum + width, 0)}px`;
            savedWidths.set(key, widths.slice());
        }

        headers.forEach((header, index) => {
            // The outer edge stays fixed; each internal boundary divides two columns.
            if (index === headers.length - 1) return;
            const handle = document.createElement('span');
            handle.className = 'table-column-resize';
            handle.tabIndex = 0;
            handle.setAttribute('role', 'separator');
            handle.setAttribute('aria-orientation', 'vertical');
            handle.setAttribute('aria-label', `Ширина колонки «${header.textContent.trim()}»`);
            handle.setAttribute('aria-valuemin', minimumWidth);
            handle.title = 'Перетащите для изменения ширины. Стрелки ← и → — изменить ширину.';

            handle.addEventListener('pointerdown', event => {
                if (event.button !== 0) return;
                event.preventDefault();
                event.stopPropagation();
                const startX = event.clientX;
                const startWidth = widths[index];
                const nextWidth = widths[index + 1];
                handle.setPointerCapture(event.pointerId);
                document.body.classList.add('table-column-resizing');

                function move(moveEvent) {
                    const delta = Math.max(minimumWidth - startWidth,
                        Math.min(nextWidth - minimumWidth, moveEvent.clientX - startX));
                    widths[index] = startWidth + delta;
                    widths[index + 1] = nextWidth - delta;
                    applyWidths();
                }

                function finish() {
                    document.body.classList.remove('table-column-resizing');
                    handle.removeEventListener('pointermove', move);
                    handle.removeEventListener('pointerup', finish);
                    handle.removeEventListener('pointercancel', finish);
                    handle.removeEventListener('lostpointercapture', finish);
                    if (handle.hasPointerCapture(event.pointerId)) handle.releasePointerCapture(event.pointerId);
                }

                handle.addEventListener('pointermove', move);
                handle.addEventListener('pointerup', finish);
                handle.addEventListener('pointercancel', finish);
                handle.addEventListener('lostpointercapture', finish);
            });
            handle.addEventListener('keydown', event => {
                if (event.key !== 'ArrowLeft' && event.key !== 'ArrowRight') return;
                event.preventDefault();
                event.stopPropagation();
                const step = event.shiftKey ? 32 : 8;
                const delta = Math.max(minimumWidth - widths[index],
                    Math.min(widths[index + 1] - minimumWidth, event.key === 'ArrowRight' ? step : -step));
                widths[index] += delta;
                widths[index + 1] -= delta;
                applyWidths();
            });
            header.append(handle);
        });
        applyWidths();
        function updateHandleHeight() {
            const tableBottom = table.getBoundingClientRect().bottom;
            headers.forEach(header => {
                const handle = header.querySelector('.table-column-resize');
                if (handle) handle.style.height = `${tableBottom - header.getBoundingClientRect().top}px`;
            });
        }
        const observer = new ResizeObserver(updateHandleHeight);
        observer.observe(table);
        initialized.set(table, observer);
        updateHandleHeight();
    }

    function scan() {
        initialized.forEach((observer, table) => {
            if (!table.isConnected) {
                observer.disconnect();
                initialized.delete(table);
            }
        });
        document.querySelectorAll('.app-shell__content table.table').forEach(initialize);
    }

    // Blazor inserts and replaces tables after navigation and asynchronous data loading.
    let scheduled = false;
    new MutationObserver(() => {
        if (scheduled) return;
        scheduled = true;
        requestAnimationFrame(() => {
            scheduled = false;
            scan();
        });
    }).observe(document.body, { childList: true, subtree: true });
    window.addEventListener('resize', scan);
    scan();
})();
