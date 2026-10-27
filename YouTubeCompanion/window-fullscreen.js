(() => {
  'use strict';
  if (window.__paneLockYouTubeInstalled) return;
  window.__paneLockYouTubeInstalled = true;
  window.__paneLockVersion = '0.3.1';
  const marker = ' [PaneLock video]';
  const rawElement = Object.getOwnPropertyDescriptor(Document.prototype, 'fullscreenElement');
  const nativeExit = Document.prototype.exitFullscreen;
  let active = null, ancestors = [], recoveringNative = false;

  function syncTitle() {
    const clean = document.title.split(marker).join('');
    const desired = active ? clean + marker : clean;
    if (document.title !== desired) document.title = desired;
  }
  function playerFor(element) {
    const selector = '#movie_player, .html5-video-player';
    if (element instanceof Element) {
      if (element.matches(selector)) return element;
      const parent = element.closest(selector);
      if (parent) return parent;
      const child = element.querySelector(selector);
      if (child) return child;
    }
    return document.querySelector(selector);
  }
  function notify(element) {
    element.dispatchEvent(new Event('fullscreenchange', { bubbles: true }));
    element.dispatchEvent(new Event('webkitfullscreenchange', { bubbles: true }));
    window.dispatchEvent(new Event('resize'));
  }
  function leave() {
    if (!active) return;
    const previous = active; active = null;
    previous.removeAttribute('data-panelock-player');
    for (const ancestor of ancestors) ancestor.removeAttribute('data-panelock-ancestor');
    ancestors = [];
    document.documentElement.removeAttribute('data-panelock-active');
    syncTitle(); notify(previous.isConnected ? previous : document);
  }
  function enter(player) {
    if (active === player) return Promise.resolve();
    leave(); active = player;
    player.setAttribute('data-panelock-player', '');
    for (let parent = player.parentElement; parent; parent = parent.parentElement) {
      parent.setAttribute('data-panelock-ancestor', ''); ancestors.push(parent);
    }
    document.documentElement.setAttribute('data-panelock-active', '');
    syncTitle(); notify(player);
    return Promise.resolve();
  }
  function request(element) {
    const player = playerFor(element);
    // Never delegate a YouTube fullscreen request to monitor-sized fullscreen.
    if (!player) return Promise.reject(new DOMException('PaneLock cannot locate the YouTube player.', 'NotSupportedError'));
    if (rawElement && rawElement.get.call(document)) return nativeExit.call(document).then(() => enter(player));
    return enter(player);
  }
  function exit(...args) {
    if (active) { leave(); return Promise.resolve(); }
    return nativeExit.apply(this, args);
  }
  for (const name of ['requestFullscreen', 'webkitRequestFullscreen', 'webkitRequestFullScreen']) {
    if (name in Element.prototype) Element.prototype[name] = function () { return request(this); };
  }
  for (const name of ['exitFullscreen', 'webkitExitFullscreen', 'webkitCancelFullScreen']) {
    if (name in Document.prototype) Document.prototype[name] = exit;
  }
  for (const name of ['fullscreenElement', 'webkitFullscreenElement', 'webkitCurrentFullScreenElement']) {
    const descriptor = Object.getOwnPropertyDescriptor(Document.prototype, name);
    if (descriptor && descriptor.configurable) Object.defineProperty(Document.prototype, name, {
      configurable: true, enumerable: descriptor.enumerable,
      get() { return active || descriptor.get.call(this); }
    });
  }
  for (const name of ['webkitIsFullScreen', 'fullscreen']) {
    const descriptor = Object.getOwnPropertyDescriptor(Document.prototype, name);
    if (descriptor && descriptor.configurable) Object.defineProperty(Document.prototype, name, {
      configurable: true, enumerable: descriptor.enumerable,
      get() { return !!active || descriptor.get.call(this); }
    });
  }
  function toggle() { if (active) leave(); else request(null).catch(() => {}); }
  // Intercept the actual control before YouTube or another extension calls a cached native API.
  window.addEventListener('click', event => {
    if (!event.composedPath().some(node => node instanceof Element && node.matches('.ytp-fullscreen-button'))) return;
    event.preventDefault(); event.stopImmediatePropagation(); toggle();
  }, true);
  window.addEventListener('keydown', event => {
    if (event.key === 'Escape' && active) {
      event.preventDefault(); event.stopImmediatePropagation(); leave(); return;
    }
    const target = event.target;
    if (event.key.toLowerCase() === 'f' && !event.ctrlKey && !event.altKey && !event.metaKey &&
        !(target instanceof Element && (target.closest('input,textarea,select,[contenteditable="true"]') || target.isContentEditable)) && playerFor(null)) {
      event.preventDefault(); event.stopImmediatePropagation(); if (!event.repeat) toggle();
    }
  }, true);
  window.addEventListener('dblclick', event => {
    if (!(event.target instanceof Element) || !event.target.closest('.html5-video-container,video')) return;
    event.preventDefault(); event.stopImmediatePropagation(); toggle();
  }, true);
  // Recover a cached native fullscreen call that bypassed all prototype hooks.
  document.addEventListener('fullscreenchange', () => {
    if (!rawElement || !rawElement.get.call(document) || recoveringNative) return;
    recoveringNative = true;
    nativeExit.call(document).then(() => {
      const player = playerFor(null); if (player) return enter(player);
    }).catch(() => {}).finally(() => { recoveringNative = false; });
  }, true);
  document.addEventListener('yt-navigate-start', leave);
  window.addEventListener('pagehide', leave);
  new MutationObserver(() => {
    if (active && !active.isConnected) leave();
    syncTitle();
  }).observe(document, { subtree: true, childList: true, characterData: true });
})();
