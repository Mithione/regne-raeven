// Regneræven – "Mit værelse": move, flip, tilt and layer things; saves automatically.
(function () {
  'use strict';

  function init(root) {
    if (!root) return;
    const svg = root.querySelector('svg.room-svg');
    const layer = svg.querySelector('.room-items');
    const token = document.querySelector('meta[name="rr-token"]')?.content || '';
    const nameEl = root.querySelector('[data-selected-name]');
    const hintEl = root.querySelector('[data-hint]');
    const savedEl = root.querySelector('[data-saved]');
    const tools = [...root.querySelectorAll('[data-act]')];
    let selected = null;

    const num = (g, k) => parseFloat(g.dataset[k]) || 0;
    const items = () => [...layer.querySelectorAll('.ri')];

    function apply(g) {
      const x = num(g, 'x'), y = num(g, 'y'), r = num(g, 'r'), w = num(g, 'w'), h = num(g, 'h');
      const f = g.dataset.f === '1' ? -1 : 1;
      g.setAttribute('transform', `translate(${x} ${y}) rotate(${r}) scale(${f} 1) translate(${-w / 2} ${-h})`);
    }

    function point(evt) {
      const pt = svg.createSVGPoint();
      pt.x = evt.clientX; pt.y = evt.clientY;
      return pt.matrixTransform(svg.getScreenCTM().inverse());
    }

    function select(g) {
      items().forEach(i => i.classList.toggle('selected', i === g));
      selected = g;
      tools.forEach(t => t.disabled = !g || (t.dataset.act === 'box' && g.dataset.id === 'fox'));
      hintEl.classList.toggle('hidden', !!g);
      nameEl.classList.toggle('hidden', !g);
      nameEl.textContent = g ? g.dataset.name : '';
    }

    // Keep the drawing order (DOM order) in line with the layers.
    function sortLayers() {
      items().sort((a, b) => num(a, 'z') - num(b, 'z')).forEach(g => layer.appendChild(g));
    }

    let saveTimer = null;
    function save() {
      clearTimeout(saveTimer);
      savedEl.textContent = 'Gemmer …';
      saveTimer = setTimeout(async () => {
        const body = {
          v: 2, wall: '', bedding: '',
          items: items().map(g => ({ id: g.dataset.id, x: num(g, 'x'), y: num(g, 'y'), f: g.dataset.f === '1', r: num(g, 'r'), z: Math.round(num(g, 'z')) })),
        };
        try {
          const res = await fetch('/vaerelse/indret', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json', 'Accept': 'application/json', 'RequestVerificationToken': token },
            body: JSON.stringify(body),
          });
          savedEl.textContent = res.ok ? 'Gemt ✓' : 'Kunne ikke gemme';
        } catch { savedEl.textContent = 'Kunne ikke gemme'; }
      }, 350);
    }

    // ---- dragging (mouse, pen and touch)
    let drag = null;
    svg.addEventListener('pointerdown', e => {
      const g = e.target.closest('.ri');
      if (!g) {
        if (e.target.closest('[data-window]')) { svg.classList.toggle('night'); return; }
        select(null);
        return;
      }
      e.preventDefault();
      select(g);
      const p = point(e);
      drag = { g, dx: p.x - num(g, 'x'), dy: p.y - num(g, 'y'), moved: false, id: e.pointerId };
      svg.setPointerCapture(e.pointerId);
      g.classList.add('dragging');
    });
    svg.addEventListener('pointermove', e => {
      if (!drag || e.pointerId !== drag.id) return;
      const p = point(e);
      const x = Math.max(0, Math.min(800, p.x - drag.dx));
      const y = Math.max(20, Math.min(500, p.y - drag.dy));
      if (Math.abs(x - num(drag.g, 'x')) + Math.abs(y - num(drag.g, 'y')) > 0.5) drag.moved = true;
      drag.g.dataset.x = x.toFixed(1);
      drag.g.dataset.y = y.toFixed(1);
      apply(drag.g);
    });
    const endDrag = e => {
      if (!drag || e.pointerId !== drag.id) return;
      drag.g.classList.remove('dragging');
      if (drag.moved) save();
      drag = null;
    };
    svg.addEventListener('pointerup', endDrag);
    svg.addEventListener('pointercancel', endDrag);

    // ---- toolbar
    function act(what) {
      const g = selected;
      if (!g) return;
      const zs = items().map(i => num(i, 'z'));
      switch (what) {
        case 'left': g.dataset.r = Math.max(-180, num(g, 'r') - 15); break;
        case 'right': g.dataset.r = Math.min(180, num(g, 'r') + 15); break;
        case 'straight': g.dataset.r = 0; break;
        case 'flip': g.dataset.f = g.dataset.f === '1' ? '0' : '1'; break;
        case 'front': g.dataset.z = Math.max(...zs) + 1; sortLayers(); break;
        case 'back': g.dataset.z = Math.min(...zs) - 1; sortLayers(); break;
        case 'box':
          if (g.dataset.id === 'fox') return;
          g.remove();
          select(null);
          save();
          // Show it in the box without reloading; "Sæt ind" puts it back.
          addToBox(g.dataset.id, g.dataset.name);
          return;
      }
      apply(g);
      save();
    }
    tools.forEach(t => t.addEventListener('click', () => act(t.dataset.act)));

    function addToBox(id, name) {
      const box = document.querySelector('[data-box]');
      box.querySelector('[data-box-empty]')?.remove();
      const f = document.createElement('form');
      f.method = 'post'; f.action = '/vaerelse/placer'; f.className = 'box-item';
      f.innerHTML = `<input type="hidden" name="id" value="${id}"><input type="hidden" name="__RequestVerificationToken" value="${token}">` +
        `<button type="submit"><span class="thumb">📦</span><span></span><span class="small muted">Sæt ind</span></button>`;
      f.querySelector('button span:nth-child(2)').textContent = name;
      box.appendChild(f);
    }

    // ---- keyboard: arrows move, R/E rotate, F flips, Delete puts in the box
    document.addEventListener('keydown', e => {
      if (!selected || e.target.closest('input, textarea')) return;
      const step = e.shiftKey ? 20 : 5;
      const moves = { ArrowLeft: [-step, 0], ArrowRight: [step, 0], ArrowUp: [0, -step], ArrowDown: [0, step] };
      if (moves[e.key]) {
        e.preventDefault();
        selected.dataset.x = Math.max(0, Math.min(800, num(selected, 'x') + moves[e.key][0]));
        selected.dataset.y = Math.max(20, Math.min(500, num(selected, 'y') + moves[e.key][1]));
        apply(selected);
        save();
      } else if (e.key === 'r') act('right');
      else if (e.key === 'e') act('left');
      else if (e.key === 'f') act('flip');
      else if (e.key === 'Delete') act('box');
      else if (e.key === 'Escape') select(null);
    });

    select(null);
  }

  window.rrRoom = { init };
})();
