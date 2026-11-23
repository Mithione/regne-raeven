// Regneræven – 10-finger typing trainer with an on-screen Danish keyboard.
(function () {
  'use strict';

  // ---------------------------------------------------------------- Danish keyboard (ISO)
  // Each key: [normal, shifted, finger, width]. Fingers: lp lr lm li (left) ri rm rr rp (right), th (thumbs).
  const K = (n, s, f, w) => ({ n, s, f, w: w || 1 });
  const L = (ch, f) => K(ch, ch.toUpperCase(), f);
  const ROWS = [
    [K('½', '§', 'lp'), K('1', '!', 'lp'), K('2', '"', 'lr'), K('3', '#', 'lm'), K('4', '¤', 'li'), K('5', '%', 'li'), K('6', '&', 'ri'),
     K('7', '/', 'ri'), K('8', '(', 'rm'), K('9', ')', 'rr'), K('0', '=', 'rp'), K('+', '?', 'rp'), K('´', '`', 'rp'), K('⌫', null, 'rp', 2)],
    [K('Tab', null, 'lp', 1.5), L('q', 'lp'), L('w', 'lr'), L('e', 'lm'), L('r', 'li'), L('t', 'li'), L('y', 'ri'), L('u', 'ri'),
     L('i', 'rm'), L('o', 'rr'), L('p', 'rp'), L('å', 'rp'), K('¨', '^', 'rp'), K('', null, 'rp', 1.5)],
    [K('Caps', null, 'lp', 1.75), L('a', 'lp'), L('s', 'lr'), L('d', 'lm'), L('f', 'li'), L('g', 'li'), L('h', 'ri'), L('j', 'ri'),
     L('k', 'rm'), L('l', 'rr'), L('æ', 'rp'), L('ø', 'rp'), K("'", '*', 'rp'), K('Enter', null, 'rp', 1.25)],
    [K('Shift', null, 'lp', 1.25), K('<', '>', 'lp'), L('z', 'lp'), L('x', 'lr'), L('c', 'lm'), L('v', 'li'), L('b', 'li'), L('n', 'ri'),
     L('m', 'ri'), K(',', ';', 'rm'), K('.', ':', 'rr'), K('-', '_', 'rp'), K('Shift', null, 'rp', 2.75)],
    [K(' ', null, 'th', 6.5)],
  ];
  const FINGER_NAMES = {
    lp: 'venstre lillefinger', lr: 'venstre ringfinger', lm: 'venstre langefinger', li: 'venstre pegefinger',
    ri: 'højre pegefinger', rm: 'højre langefinger', rr: 'højre ringfinger', rp: 'højre lillefinger', th: 'tommelfingeren',
  };
  const HOME = new Set(['a', 's', 'd', 'f', 'j', 'k', 'l', 'æ']);
  const isLeft = f => f && f[0] === 'l';

  /** Which key (and whether Shift) produces a character. */
  function keyFor(ch) {
    for (const row of ROWS) for (const k of row) {
      if (k.n === ch) return { key: k, shift: false };
      if (k.s === ch) return { key: k, shift: true };
    }
    return null;
  }

  function renderKeyboard(host) {
    host.innerHTML = '';
    const els = new Map();
    const shifts = [];
    ROWS.forEach(row => {
      const r = document.createElement('div');
      r.className = 'kb-row';
      row.forEach(k => {
        const el = document.createElement('div');
        el.className = `kb-key f-${k.f}` + (HOME.has(k.n) ? ' home' : '') + (k.n.length > 1 || k.n === '' ? ' wide' : '');
        el.style.flexGrow = k.w;
        if (k.n === ' ') el.textContent = 'mellemrum';
        else if (k.s && k.s !== k.n.toUpperCase()) el.innerHTML = `<small>${k.s}</small><span>${k.n}</span>`;
        else el.textContent = k.n.length === 1 ? k.n.toUpperCase() : k.n;
        if (k.n === 'Shift') shifts.push({ el, left: isLeft(k.f) });
        if (k.n === 'f' || k.n === 'j') el.classList.add('bump');
        els.set(k, el);
        r.appendChild(el);
      });
      host.appendChild(r);
    });
    return { els, shifts };
  }

  // ---------------------------------------------------------------- the lesson
  function lesson(root) {
    const text = root.dataset.text;
    const lessonId = root.dataset.lesson;
    const token = document.querySelector('meta[name="rr-token"]')?.content || '';
    const textEl = root.querySelector('[data-typing-text]');
    const kb = renderKeyboard(root.querySelector('[data-keyboard]'));
    const fingerEl = root.querySelector('[data-finger]');
    const accEl = root.querySelector('[data-acc]');
    const wpmEl = root.querySelector('[data-wpm]');
    const errEl = root.querySelector('[data-errors]');
    const progressEl = root.querySelector('[data-progress]');
    const startHint = root.querySelector('[data-start-hint]');
    const resultEl = root.querySelector('[data-result]');

    // One span per character, grouped per word so a line never breaks inside "2,5" or "3/4".
    // Calculations/sentences are separated by three spaces: a single space inside one keeps it together.
    let word = null;
    const items = text.includes('   ');
    const chars = [...text];
    const spans = chars.map((ch, i) => {
      const s = document.createElement('span');
      s.className = 'ch' + (ch === ' ' ? ' sp' : '');
      s.textContent = ch === ' ' ? '·' : ch;
      const inside = items && ch === ' ' && chars[i - 1] !== ' ' && chars[i + 1] !== ' ';
      if (ch === ' ' && !inside) {
        textEl.appendChild(s);
        textEl.appendChild(document.createTextNode('​')); // the "·" isn't a real space – allow a line break here
        word = null;
      }
      else {
        if (!word) { word = document.createElement('span'); word.className = 'w'; textEl.appendChild(word); }
        word.appendChild(s);
      }
      return s;
    });

    let pos = 0, errors = 0, started = 0, done = false, errorHere = false;

    function highlight() {
      kb.els.forEach(el => el.classList.remove('next'));
      kb.shifts.forEach(s => s.el.classList.remove('next'));
      spans.forEach((s, i) => s.classList.toggle('current', i === pos));
      if (pos >= text.length) { fingerEl.textContent = ''; return; }
      const hit = keyFor(text[pos]);
      if (!hit) { fingerEl.textContent = ''; return; }
      kb.els.get(hit.key)?.classList.add('next');
      let hint = `Brug <b class="dot f-${hit.key.f}"></b> ${FINGER_NAMES[hit.key.f]}`;
      if (hit.shift) {
        // Shift with the OTHER hand's little finger.
        const left = !isLeft(hit.key.f);
        kb.shifts.filter(s => s.left === left).forEach(s => s.el.classList.add('next'));
        hint += ` + ${left ? 'venstre' : 'højre'} Shift`;
      }
      fingerEl.innerHTML = hint;
      // Keep the current letter in view in long texts.
      spans[pos].scrollIntoView({ block: 'nearest' });
    }

    function stats() {
      const acc = pos + errors === 0 ? 100 : Math.floor(100 * pos / (pos + errors));
      const minutes = started ? (performance.now() - started) / 60000 : 0;
      const wpm = minutes > 0.02 ? Math.round(pos / 5 / minutes) : 0;
      accEl.textContent = acc + ' %';
      wpmEl.textContent = wpm;
      errEl.textContent = errors;
      progressEl.style.width = (100 * pos / text.length) + '%';
    }

    function wrong() {
      errors++;
      errorHere = true;
      spans[pos].classList.add('err');
      textEl.classList.remove('shake');
      void textEl.offsetWidth; // restart the animation
      textEl.classList.add('shake');
    }

    async function finish() {
      done = true;
      highlight();
      const seconds = (performance.now() - started) / 1000;
      let r;
      try {
        const res = await fetch(`/skriv/${encodeURIComponent(lessonId)}/resultat`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json', 'Accept': 'application/json', 'RequestVerificationToken': token },
          body: JSON.stringify({ chars: text.length, errors, seconds }),
        });
        r = await res.json();
        if (!res.ok) throw new Error(r.error || 'Resultatet kunne ikke gemmes.');
      } catch (err) {
        resultEl.innerHTML = `<div class="feedback no">${rr.esc(err.message)}</div>`;
        resultEl.classList.remove('hidden');
        return;
      }
      const stars = '★'.repeat(r.stars) + '☆'.repeat(3 - r.stars);
      const xpText = r.xp > 0 ? `<span class="xp-pop">+${r.xp} XP</span>${r.practice ? ' <span class="muted small">(øve-XP)</span>' : ''}` : '<span class="muted">0 XP</span>';
      const nextBtn = r.next && r.next.open
        ? `<a class="btn big" href="/skriv/${encodeURIComponent(r.next.id)}">Næste: ${rr.esc(r.next.title)} →</a>` : '';
      resultEl.innerHTML = `
        <div class="typing-result">
          <div class="big-stars s${r.stars}">${stars}</div>
          ${r.newBest && r.stars > 0 ? '<div class="badge green">Ny rekord!</div>' : ''}
          <div class="grid three mt">
            <div class="stat"><b>${r.accuracy} %</b><span>Præcision</span></div>
            <div class="stat"><b>${r.wpm}</b><span>Ord pr. minut</span></div>
            <div class="stat"><b>${xpText}</b><span>XP</span></div>
          </div>
          <p class="mt">${rr.esc(r.message)}</p>
          <div class="row" style="justify-content:center">
            ${nextBtn}
            <a class="btn ${nextBtn ? 'secondary' : 'big'}" href="/skriv/${encodeURIComponent(lessonId)}">↻ Prøv igen</a>
            <a class="btn ghost" href="/skriv">Alle lektioner</a>
          </div>
        </div>`;
      resultEl.classList.remove('hidden');
      resultEl.scrollIntoView({ block: 'nearest', behavior: 'smooth' });
      rr.updateSidebar(r);
      if (r.levelUp) setTimeout(() => rr.levelUpDialog(r.levelUp), 500);
      else if (r.stars === 3) rr.confetti();
    }

    document.addEventListener('keydown', e => {
      if (done || e.ctrlKey || e.metaKey || (e.altKey && !e.getModifierState('AltGraph'))) return;
      if (e.key.length !== 1) return; // Shift, CapsLock, arrows, dead keys …
      e.preventDefault();
      if (!started) { started = performance.now(); startHint.classList.add('hidden'); }
      if (e.key === text[pos]) {
        spans[pos].classList.add(errorHere ? 'fixed' : 'ok');
        spans[pos].classList.remove('err');
        errorHere = false;
        pos++;
        stats();
        if (pos >= text.length) { finish(); return; }
      } else {
        wrong();
        stats();
      }
      highlight();
    });

    highlight();
    stats();
  }

  window.rrTyping = { lesson, renderKeyboard };
})();
