export function read(key) {
    try { return { value: localStorage.getItem(key), available: true }; }
    catch { return { value: null, available: false }; }
}

export function write(key, value) {
    try { localStorage.setItem(key, value); return true; }
    catch { return false; }
}

export function remove(key) {
    try { localStorage.removeItem(key); return true; }
    catch { return false; }
}

export function measureWidths(host) {
    return Array.from(host.querySelectorAll('thead [data-grid-field]'))
        .map(cell => ({ field: cell.dataset.gridField, width: cell.getBoundingClientRect().width }))
        .filter(column => column.width > 0);
}

const attached = new Map();

export function attachResize(host, reference) {
    detachResize(host);
    let finishDrag;
    function boundary(event) {
        const cell = event.target.closest('[data-grid-field]');
        if (!cell || !host.contains(cell) || !cell.closest('tbody') || cell.closest('input,button,a')) return null;
        if (Math.abs(event.clientX - cell.getBoundingClientRect().right) > 5) return null;
        const headers = Array.from(host.querySelectorAll('thead [data-grid-field]'));
        const index = headers.findIndex(header => header.dataset.gridField === cell.dataset.gridField);
        return index >= 0 && index < headers.length - 1 ? { cell, headers, index } : null;
    }
    function hover(event) {
        host.classList.toggle('print-grid--resize-hover', !!boundary(event));
    }
    function start(event) {
        if (event.button !== 0 || finishDrag) return;
        const target = boundary(event);
        if (!target) return;
        event.preventDefault();
        event.stopPropagation();
        const { headers, index } = target;
        const fields = headers.map(header => header.dataset.gridField);
        const widths = headers.map(header => header.getBoundingClientRect().width);
        const startX = event.clientX;
        const edge = headers[index].getBoundingClientRect().right;
        const guide = document.createElement('div');
        guide.className = 'print-grid__resize-guide';
        const rect = host.querySelector('thead').getBoundingClientRect();
        const bottom = host.querySelector('tbody').getBoundingClientRect().bottom;
        Object.assign(guide.style, { top: `${rect.top}px`, height: `${bottom - rect.top}px`, left: `${edge}px` });
        document.body.append(guide);
        document.body.classList.add('print-grid--resizing');
        let delta = 0;
        function move(moveEvent) {
            delta = Math.max(64 - widths[index], Math.min(widths[index + 1] - 64, moveEvent.clientX - startX));
            guide.style.left = `${edge + delta}px`;
        }
        function finish(apply) {
            document.removeEventListener('pointermove', move);
            document.removeEventListener('pointerup', up);
            document.removeEventListener('pointercancel', cancel);
            window.removeEventListener('blur', cancel);
            guide.remove();
            document.body.classList.remove('print-grid--resizing');
            finishDrag = null;
            if (apply && delta) reference.invokeMethodAsync('ResizeColumns', fields[index], fields[index + 1], fields, widths, delta)
                .catch(error => console.error('Grid resize failed', error));
        }
        const up = () => finish(true);
        const cancel = () => finish(false);
        finishDrag = cancel;
        document.addEventListener('pointermove', move);
        document.addEventListener('pointerup', up);
        document.addEventListener('pointercancel', cancel);
        window.addEventListener('blur', cancel);
    }
    host.addEventListener('pointermove', hover);
    host.addEventListener('pointerdown', start, true);
    attached.set(host, () => {
        finishDrag?.();
        host.removeEventListener('pointermove', hover);
        host.removeEventListener('pointerdown', start, true);
        host.classList.remove('print-grid--resize-hover');
    });
}

export function detachResize(host) {
    attached.get(host)?.();
    attached.delete(host);
}

new MutationObserver(() => {
    for (const host of attached.keys()) if (!host.isConnected) detachResize(host);
}).observe(document.body, { childList: true, subtree: true });
