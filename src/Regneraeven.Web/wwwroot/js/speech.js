// Regneræven – read aloud with the computer's own Danish voice (Web Speech API / Windows voices).
(function () {
  'use strict';
  const synth = window.speechSynthesis;
  const MALE = /jeppe|mads|christian|frederik|male|mand/i;
  const FEMALE = /helle|christel|sara|pernille|female|kvinde/i;

  /** Voices load asynchronously in Chromium/WebView2. */
  function voices() {
    return new Promise(resolve => {
      if (!synth) return resolve([]);
      const now = synth.getVoices();
      if (now.length) return resolve(now);
      let done = false;
      const finish = () => { if (!done) { done = true; resolve(synth.getVoices()); } };
      synth.addEventListener('voiceschanged', finish, { once: true });
      setTimeout(finish, 1500);
    });
  }

  /** The Danish voice to use: the wanted gender if available, otherwise any Danish voice. */
  async function pick() {
    const pref = document.body.dataset.voice === 'mand' ? 'mand' : 'kvinde';
    const danish = (await voices()).filter(v => (v.lang || '').toLowerCase().startsWith('da'));
    const wanted = danish.find(v => (pref === 'mand' ? MALE : FEMALE).test(v.name))
      || (pref === 'kvinde' ? danish.find(v => !MALE.test(v.name)) : null);
    const voice = wanted || danish[0] || null;
    return { voice, danish, pref, gotWanted: !!wanted };
  }

  const rate = () => document.body.dataset.rate === 'normal' ? 1.0 : 0.85;

  /** Short pieces read more reliably than one long text (Chromium cuts long utterances). */
  function sentences(text) {
    return text.match(/[^.!?]+[.!?]*/g)?.map(s => s.trim()).filter(Boolean) || [text];
  }

  let token = 0;

  /** Reads a list of {text, el} one after another, marking el while it is read. */
  async function speak(items, onDone) {
    stop();
    const my = ++token;
    const { voice } = await pick();
    const queue = [];
    items.forEach(it => sentences(it.text).forEach((s, i) => queue.push({ text: s, el: it.el, first: i === 0 })));
    let i = 0;
    const next = () => {
      if (my !== token) return;
      document.querySelectorAll('.speaking').forEach(e => e.classList.remove('speaking'));
      if (i >= queue.length) { onDone && onDone(); return; }
      const q = queue[i++];
      q.el?.classList.add('speaking');
      if (q.first) q.el?.scrollIntoView({ block: 'nearest', behavior: 'smooth' });
      const u = new SpeechSynthesisUtterance(q.text);
      u.lang = 'da-DK';
      if (voice) u.voice = voice;
      u.rate = rate();
      u.onend = next;
      u.onerror = next;
      synth.speak(u);
    };
    next();
  }

  function stop() {
    token++;
    if (synth) synth.cancel();
    document.querySelectorAll('.speaking').forEach(e => e.classList.remove('speaking'));
  }

  const NO_VOICE = 'Der er ingen dansk stemme på computeren. Tilføj den i Windows: Indstillinger → Tid og sprog → Tale → Tilføj stemmer → Dansk.';

  /** A rule page: "Læs hele reglen op" plus a small 🔊 per section. */
  async function rulePage(root) {
    if (!root || !synth) return;
    const all = root.querySelector('[data-speak-all]');
    const note = root.querySelector('[data-speak-note]');
    const chunks = [...root.querySelectorAll('[data-speak]')];
    const info = await pick();
    if (!info.voice) { note.textContent = '🔇 ' + NO_VOICE; note.classList.remove('hidden'); }
    document.querySelectorAll('[data-speak-ui]').forEach(e => e.classList.remove('hidden'));

    let playing = null;
    const reset = () => {
      playing = null;
      all.textContent = '🔊 Læs reglen op';
      root.querySelectorAll('[data-speak-one]').forEach(b => b.textContent = '🔊');
    };
    all.addEventListener('click', () => {
      if (playing === 'all') { stop(); reset(); return; }
      reset();
      playing = 'all';
      all.textContent = '⏹ Stop oplæsning';
      speak(chunks.map(el => ({ text: el.dataset.speak, el })), reset);
    });
    root.querySelectorAll('[data-speak-one]').forEach(btn => btn.addEventListener('click', () => {
      const el = btn.closest('[data-speak]');
      if (playing === el) { stop(); reset(); return; }
      reset();
      playing = el;
      btn.textContent = '⏹';
      speak([{ text: el.dataset.speak, el }], reset);
    }));
    window.addEventListener('pagehide', stop);
  }

  /** Profile settings: show which voice is used, and a test button. */
  async function settings(root) {
    if (!root) return;
    const status = root.querySelector('[data-voice-status]');
    const render = async () => {
      const info = await pick();
      if (!synth || !info.voice) { status.innerHTML = '🔇 ' + NO_VOICE; return; }
      let html = `Stemme: <b>${info.voice.name.replace(/ - Danish.*$/i, '').replace(/^Microsoft /, '')}</b>`;
      if (info.pref === 'mand' && !info.gotWanted)
        html += '<br><span class="muted">Der er ingen dansk mandestemme på denne computer, så kvindestemmen bruges. Kommer der en (fx i Windows-indstillinger under Tale), bruges den automatisk.</span>';
      if (info.danish.length > 1) html += `<br><span class="muted">${info.danish.length} danske stemmer fundet.</span>`;
      status.innerHTML = html;
    };
    root.querySelectorAll('input[name=voice], input[name=rate]').forEach(i => i.addEventListener('change', () => {
      document.body.dataset.voice = root.querySelector('input[name=voice]:checked').value;
      document.body.dataset.rate = root.querySelector('input[name=rate]:checked').value;
      render();
    }));
    root.querySelector('[data-voice-test]')?.addEventListener('click', () =>
      speak([{ text: 'Hej, jeg er Regneræven. Tre fjerdedele plus en sjettedel er lig med elleve tolvtedele.' }]));
    render();
  }

  window.rrSpeech = { speak, stop, rulePage, settings, pick };
})();
