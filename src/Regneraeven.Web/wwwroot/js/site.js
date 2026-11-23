// Regneræven – small, dependency-free page scripts.
(function () {
  'use strict';
  const token = document.querySelector('meta[name="rr-token"]')?.content || '';

  // Every POST form gets the anti-forgery token.
  function addTokens(root) {
    root.querySelectorAll('form[method="post" i]').forEach(f => {
      if (!f.querySelector('input[name="__RequestVerificationToken"]')) {
        const i = document.createElement('input');
        i.type = 'hidden'; i.name = '__RequestVerificationToken'; i.value = token;
        f.appendChild(i);
      }
    });
  }
  addTokens(document);

  async function post(url, body) {
    const res = await fetch(url, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', 'Accept': 'application/json', 'RequestVerificationToken': token },
      body: JSON.stringify(body || {}),
    });
    const data = await res.json().catch(() => ({}));
    if (!res.ok) throw new Error(data.error || 'Noget gik galt. Prøv igen.');
    return data;
  }

  const esc = s => String(s ?? '').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
  const debounce = (fn, ms) => { let t; return (...a) => { clearTimeout(t); t = setTimeout(() => fn(...a), ms); }; };

  // ------------------------------------------------------------------ calculator
  // A tiny safe evaluator (no eval): numbers with comma, + − · : and parentheses.
  function evaluate(text) {
    const src = text.replace(/×|\*|x/g, '·').replace(/÷|\//g, ':').replace(/-/g, '−');
    const tokens = [];
    for (let i = 0; i < src.length;) {
      const c = src[i];
      if (c === ' ') { i++; continue; }
      if (/[0-9,]/.test(c)) {
        let j = i; while (j < src.length && /[0-9,]/.test(src[j])) j++;
        const s = src.slice(i, j);
        if ((s.match(/,/g) || []).length > 1) throw new Error('For mange kommaer');
        tokens.push({ t: 'n', v: parseFloat(s.replace(',', '.')) || 0 });
        i = j; continue;
      }
      if ('+−·:()'.includes(c)) { tokens.push({ t: c }); i++; continue; }
      throw new Error('Ukendt tegn');
    }
    let p = 0;
    const peek = () => tokens[p];
    function expr() {
      let v = term();
      while (peek() && (peek().t === '+' || peek().t === '−')) { const op = tokens[p++].t; const r = term(); v = op === '+' ? v + r : v - r; }
      return v;
    }
    function term() {
      let v = unary();
      while (peek() && (peek().t === '·' || peek().t === ':' || peek().t === '(' || peek().t === 'n')) {
        let op = peek().t;
        if (op === '(' || op === 'n') op = '·'; else p++;
        const r = unary();
        if (op === ':' && r === 0) throw new Error('Man kan ikke dividere med 0');
        v = op === '·' ? v * r : v / r;
      }
      return v;
    }
    function unary() {
      if (peek() && peek().t === '−') { p++; return -unary(); }
      if (peek() && peek().t === '+') { p++; return unary(); }
      return atom();
    }
    function atom() {
      const tk = tokens[p++];
      if (!tk) throw new Error('Mangler et tal');
      if (tk.t === 'n') return tk.v;
      if (tk.t === '(') { const v = expr(); if (peek() && peek().t === ')') p++; return v; }
      throw new Error('Fejl i regnestykket');
    }
    const v = expr();
    if (p < tokens.length) throw new Error('Fejl i regnestykket');
    return v;
  }

  function fmt(v) {
    if (!isFinite(v)) return 'Fejl';
    const r = Math.round(v * 1e10) / 1e10;
    let s = String(r);
    if (/e/.test(s)) s = r.toPrecision(12);
    return s.replace('-', '−').replace('.', ',');
  }

  function calculator(el) {
    if (!el) return;
    const exprEl = el.querySelector('[data-expr]');
    const valEl = el.querySelector('[data-val]');
    let input = '';
    let justEvaluated = false;
    const show = () => { valEl.textContent = input || '0'; };
    function press(k) {
      if (k === 'C') { input = ''; exprEl.textContent = ''; justEvaluated = false; return show(); }
      if (k === '⌫') { input = input.slice(0, -1); return show(); }
      if (k === '=') {
        if (!input) return;
        try { const v = fmt(evaluate(input)); exprEl.textContent = input + ' ='; input = v; justEvaluated = true; }
        catch (e) { exprEl.textContent = e.message; }
        return show();
      }
      if (justEvaluated && /[0-9,(]/.test(k)) input = '';
      justEvaluated = false;
      input += k;
      show();
    }
    el.addEventListener('click', e => { const b = e.target.closest('button[data-k]'); if (b) press(b.dataset.k); });
    document.addEventListener('keydown', e => {
      if (e.target.closest('input, textarea')) return;
      const map = { '*': '·', 'x': '·', '/': ':', '-': '−', '.': ',', 'Enter': '=', 'Backspace': '⌫', 'Escape': 'C', 'Delete': 'C' };
      const k = map[e.key] || e.key;
      if (/^[0-9+−·:(),=C⌫]$/.test(k)) { e.preventDefault(); press(k); }
    });
  }

  // ------------------------------------------------------------------ coordinate grid (click to answer)
  function gridPicker(svg, onPick) {
    const min = +svg.dataset.min, max = +svg.dataset.max, cell = +svg.dataset.cell, pad = +svg.dataset.pad;
    const ns = 'http://www.w3.org/2000/svg';
    const ghost = document.createElementNS(ns, 'circle'); ghost.setAttribute('r', '9'); ghost.setAttribute('class', 'ghost'); ghost.style.display = 'none';
    const pick = document.createElementNS(ns, 'circle'); pick.setAttribute('r', '8'); pick.setAttribute('class', 'pick'); pick.style.display = 'none';
    svg.append(ghost, pick);
    const toPoint = e => {
      const pt = svg.createSVGPoint(); pt.x = e.clientX; pt.y = e.clientY;
      const p = pt.matrixTransform(svg.getScreenCTM().inverse());
      const x = Math.round((p.x - pad) / cell) + min, y = max - Math.round((p.y - pad) / cell);
      if (x < min || x > max || y < min || y > max) return null;
      return { x, y, cx: pad + (x - min) * cell, cy: pad + (max - y) * cell };
    };
    svg.addEventListener('mousemove', e => {
      const p = toPoint(e);
      if (!p) { ghost.style.display = 'none'; return; }
      ghost.style.display = ''; ghost.setAttribute('cx', p.cx); ghost.setAttribute('cy', p.cy);
    });
    svg.addEventListener('mouseleave', () => { ghost.style.display = 'none'; });
    svg.addEventListener('click', e => {
      const p = toPoint(e); if (!p) return;
      pick.style.display = ''; pick.setAttribute('cx', p.cx); pick.setAttribute('cy', p.cy);
      onPick(p.x, p.y);
    });
  }

  // ------------------------------------------------------------------ celebration
  function confetti() {
    const colors = ['#f26b1d', '#ffb020', '#12805c', '#2563eb', '#db2777', '#7a5af8'];
    for (let i = 0; i < 90; i++) {
      const c = document.createElement('div');
      c.className = 'confetti';
      c.style.left = Math.random() * 100 + 'vw';
      c.style.background = colors[i % colors.length];
      c.style.animationDuration = 1.6 + Math.random() * 1.8 + 's';
      c.style.animationDelay = Math.random() * .4 + 's';
      document.body.appendChild(c);
      setTimeout(() => c.remove(), 4200);
    }
  }

  function levelUpDialog(lu) {
    const title = lu.newTitles && lu.newTitles.length ? lu.newTitles[lu.newTitles.length - 1] : null;
    const o = document.createElement('div');
    o.className = 'overlay';
    o.innerHTML = `<div class="dialog">
        <div class="emoji">${title ? esc(title.icon) : '🎉'}</div>
        <h2>Level ${lu.to}!</h2>
        ${title ? `<p>Du har låst en ny titel op:</p><p class="title-name">${esc(title.name)}</p><p class="muted">${esc(title.description)}</p>`
                : `<p>Flot klaret – du er steget i level.</p>`}
        <button class="btn big mt" type="button">Fedt! 🦊</button></div>`;
    o.querySelector('button').addEventListener('click', () => o.remove());
    o.addEventListener('click', e => { if (e.target === o) o.remove(); });
    document.body.appendChild(o);
    confetti();
  }

  // ------------------------------------------------------------------ game task page
  function gameTask(card) {
    if (!card) return;
    const id = card.dataset.task;
    const form = card.querySelector('[data-answer-form]');
    const feedback = card.querySelector('[data-feedback]');
    const solution = card.querySelector('[data-solution]');
    const helpRow = card.querySelector('[data-help-row]');
    const nextRow = card.querySelector('[data-next-row]');
    const xpBadge = card.querySelector('[data-xp-badge]');
    const svg = card.querySelector('svg.grid-svg');

    if (svg && form && card.dataset.kind === 'Point') {
      const xi = form.querySelector('[name=x]'), yi = form.querySelector('[name=y]');
      gridPicker(svg, (x, y) => { xi.value = x; yi.value = y; });
    }

    card.querySelector('[data-hint-btn]')?.addEventListener('click', e => {
      card.querySelector('[data-hint]').classList.remove('hidden');
      e.currentTarget.remove();
    });

    function finish(r) {
      form.classList.add('hidden');
      helpRow.classList.add('hidden');
      const steps = (r.solution || []).map(s => `<li>${esc(s)}</li>`).join('');
      solution.innerHTML = (r.correct ? '' : `<div class="feedback tip">Svaret er <b>${esc(r.answerText)}</b>.</div>`) +
        `<h3 class="mt">Sådan regner du den</h3><ol class="steps">${steps}</ol>`;
      solution.classList.remove('hidden');
      nextRow.classList.remove('hidden');
      nextRow.querySelector('a, button')?.focus();
      // Mark the current daily tile.
      const tile = document.querySelector(`.daily a[href="/spil/opgave/${id}"]`);
      if (tile) {
        tile.classList.add(r.correct ? 'solved' : 'revealed');
        tile.querySelector('.xp').textContent = r.correct ? `+${r.xpAwarded} XP` : '0 XP';
        if (r.correct) tile.querySelector('.sym').textContent = '✓';
      }
      // Update the sidebar XP bar.
      const bar = document.querySelector('.side-profile .xpbar i');
      if (bar) bar.style.width = r.levelPercent + '%';
      const lvlText = document.querySelector('.side-profile .who > span');
      if (lvlText) lvlText.textContent = lvlText.textContent.replace(/Level \d+/, 'Level ' + r.level);
    }

    function show(r) {
      const cls = r.correct ? 'ok' : (r.finished ? 'tip' : 'no');
      let html = esc(r.message);
      if (r.xpAwarded > 0) html += ` <span class="xp-pop">+${r.xpAwarded} XP</span>`;
      if (r.dailyBonus > 0) html += `<br>🎁 Alle dagens opgaver er klaret! Bonus: <span class="xp-pop">+${r.dailyBonus} XP</span> · 🔥 ${r.streak} ${r.streak === 1 ? 'dag' : 'dage i træk'}`;
      feedback.innerHTML = `<div class="feedback ${cls}">${html}</div>`;
      if (r.correct) xpBadge.textContent = `+${r.xpAwarded} XP`;
      else if (!r.finished) xpBadge.textContent = 'Halv XP';
      else xpBadge.textContent = '0 XP';
      if (r.finished) finish(r);
      if (r.levelUp) setTimeout(() => levelUpDialog(r.levelUp), 500);
      else if (r.dailyBonus > 0) confetti();
    }

    form?.addEventListener('submit', async e => {
      e.preventDefault();
      const fd = new FormData(form);
      const body = { answer: fd.get('answer'), x: fd.get('x'), y: fd.get('y') };
      if (card.dataset.kind === 'Point' ? !(String(body.x || '').trim() && String(body.y || '').trim()) : !String(body.answer || '').trim()) {
        feedback.innerHTML = '<div class="feedback no">Skriv dit svar først.</div>';
        return;
      }
      const btn = form.querySelector('button[type=submit]');
      btn.disabled = true;
      try {
        const r = await post(`/spil/opgave/${id}/svar`, body);
        show(r);
        if (!r.finished) { const i = form.querySelector('input[name=answer], input[name=x]'); i?.select(); i?.focus(); }
      } catch (err) {
        feedback.innerHTML = `<div class="feedback no">${esc(err.message)}</div>`;
      } finally { btn.disabled = false; }
    });

    card.querySelector('[data-giveup]')?.addEventListener('click', async () => {
      if (!confirm('Vil du se løsningen? Så får du ingen XP for opgaven.')) return;
      try { show(await post(`/spil/opgave/${id}/opgiv`)); }
      catch (err) { feedback.innerHTML = `<div class="feedback no">${esc(err.message)}</div>`; }
    });
  }

  // ------------------------------------------------------------------ save-problem form: live tag suggestions
  function problemForm(isNew) {
    const form = document.querySelector('[data-problem-form]');
    if (!form) return;
    const text = form.querySelector('textarea[name=Text]');
    const boxes = [...form.querySelectorAll('input[data-topic]')];
    const touched = new Set();
    boxes.forEach(b => b.addEventListener('change', () => touched.add(b.value)));
    let autoChecked = new Set();
    const run = debounce(async () => {
      const q = text.value.trim();
      let slugs = [];
      if (q) {
        try { slugs = await (await fetch('/opgaver/forslag?tekst=' + encodeURIComponent(q), { headers: { Accept: 'application/json' } })).json(); }
        catch { return; }
      }
      boxes.forEach(b => {
        const chip = b.nextElementSibling;
        const suggested = slugs.includes(b.value);
        chip.classList.toggle('suggested', suggested);
        if (!isNew || touched.has(b.value)) return;
        if (suggested && !b.checked) { b.checked = true; autoChecked.add(b.value); }
        if (!suggested && autoChecked.has(b.value)) { b.checked = false; autoChecked.delete(b.value); }
      });
    }, 350);
    text.addEventListener('input', run);
    if (text.value.trim()) run();
  }

  // ------------------------------------------------------------------ tables page
  function tables() {
    const tabs = document.querySelector('[data-tabs]');
    tabs?.addEventListener('click', e => {
      const b = e.target.closest('button[data-tab]'); if (!b) return;
      tabs.querySelectorAll('button').forEach(x => x.classList.toggle('on', x === b));
      document.querySelectorAll('[data-panel]').forEach(p => p.classList.toggle('hidden', p.dataset.panel !== b.dataset.tab));
    });

    // Times table: highlight row/column, hide 11–12 by default.
    const table = document.getElementById('times');
    const show12 = document.getElementById('show12');
    const apply12 = () => table.querySelectorAll('.x12').forEach(el => el.style.display = show12.checked ? '' : 'none');
    show12?.addEventListener('change', apply12);
    if (table) apply12();
    table?.addEventListener('mouseover', e => {
      const td = e.target.closest('td'); if (!td) return;
      const r = td.dataset.r, c = td.dataset.c;
      table.querySelectorAll('td').forEach(x => {
        x.classList.toggle('hl', (x.dataset.r === r && +x.dataset.c <= +c) || (x.dataset.c === c && +x.dataset.r <= +r));
        x.classList.toggle('hit', x === td);
      });
    });
    table?.addEventListener('mouseleave', () => table.querySelectorAll('td').forEach(x => x.classList.remove('hl', 'hit')));

    // Practice one table.
    const pick = document.getElementById('practice-pick');
    const wrap = document.getElementById('practice');
    const rows = document.getElementById('practice-rows');
    const score = document.getElementById('practice-score');
    let current = 0;
    function render(n, shuffle) {
      current = n;
      let order = Array.from({ length: 10 }, (_, i) => i + 1);
      if (shuffle) order = order.sort(() => Math.random() - .5);
      rows.innerHTML = order.map(i =>
        `<label class="row" style="gap:6px">${i} · ${n} = <input type="text" inputmode="numeric" data-ans="${i * n}" style="width:70px;padding:6px;text-align:center"></label>`).join('');
      wrap.classList.remove('hidden');
      update();
      rows.querySelector('input')?.focus();
    }
    function update() {
      const inputs = [...rows.querySelectorAll('input')];
      const ok = inputs.filter(i => i.value.trim() === i.dataset.ans).length;
      score.textContent = ok === inputs.length ? `🎉 Alle ${ok} rigtige!` : `${ok} af ${inputs.length} rigtige`;
      if (ok === inputs.length && inputs.length) confetti();
    }
    rows?.addEventListener('input', e => {
      const i = e.target;
      const v = i.value.trim();
      i.style.background = !v ? '' : v === i.dataset.ans ? 'var(--green-soft)' : (i.dataset.ans.startsWith(v) ? '' : 'var(--red-soft)');
      update();
      if (v === i.dataset.ans) {
        const all = [...rows.querySelectorAll('input')];
        const next = all.slice(all.indexOf(i) + 1).find(x => x.value.trim() !== x.dataset.ans);
        next?.focus();
      }
    });
    pick?.addEventListener('click', e => {
      const b = e.target.closest('button[data-n]'); if (!b) return;
      pick.querySelectorAll('button').forEach(x => x.classList.toggle('on', x === b));
      render(+b.dataset.n, false);
    });
    document.getElementById('practice-shuffle')?.addEventListener('click', () => current && render(current, true));
  }

  window.rr = { post, calculator, gameTask, problemForm, tables, confetti };
})();
