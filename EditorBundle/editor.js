import { Editor, rootCtx, defaultValueCtx, editorViewCtx } from '@milkdown/kit/core';
import { commonmark } from '@milkdown/kit/preset/commonmark';
import { gfm } from '@milkdown/kit/preset/gfm';
import { history } from '@milkdown/kit/plugin/history';
import { clipboard } from '@milkdown/kit/plugin/clipboard';
import { listener, listenerCtx } from '@milkdown/kit/plugin/listener';
import { replaceAll, $prose } from '@milkdown/kit/utils';
import { Plugin, PluginKey } from '@milkdown/kit/prose/state';
import { Decoration, DecorationSet } from '@milkdown/kit/prose/view';
import { createStarryNight, common } from '@wooorm/starry-night';
import { toHtml } from 'hast-util-to-html';

const DEFAULT_MARKDOWN = '';

let lastMarkdown = '';

let starryNightRef = null;
const highlightCache = new Map();

function getStarryNight() {
  return createStarryNight(common).then((sn) => {
    starryNightRef = sn;
    return sn;
  });
}

function parseTokenHtml(html, basePos) {
  const parsed = new DOMParser().parseFromString(html, 'text/html');
  const root = parsed.body;
  if (!root) return [];

  const decos = [];
  let offset = 0;

  const walk = (el) => {
    for (const child of el.childNodes) {
      if (child.nodeType === Node.TEXT_NODE) {
        offset += child.textContent.length;
      } else if (child.nodeType === Node.ELEMENT_NODE) {
        const cls = child.getAttribute('class');
        if (cls) {
          const len = child.textContent.length;
          if (len > 0) {
            decos.push(Decoration.inline(basePos + offset, basePos + offset + len, { class: cls }));
          }
        }
        walk(child);
      }
    }
  };

  walk(root);
  return decos;
}

function highlightCodeBlock(doc) {
  const allDecos = [];
  let changed = false;

  doc.descendants((node, pos) => {
    if (node.type.name !== 'code_block') return;

    const lang = node.attrs.language || null;
    const text = node.textContent;
    const key = (lang || '') + '\u0000' + text;

    const cached = highlightCache.get(node);
    if (cached && cached.key === key) {
      if (cached.decos.length > 0) changed = true;
      allDecos.push(...cached.decos);
      return;
    }

    let decos = [];
    if (lang && text && starryNightRef) {
      try {
        const scope = starryNightRef.flagToScope(lang) || lang;
        const tree = starryNightRef.highlight(text, scope);
        if (tree) {
          decos = parseTokenHtml(toHtml(tree), pos + 1);
        }
      } catch {
        // unsupported language — plain text
      }
    }

    highlightCache.set(node, { key, decos });
    if (decos.length > 0) changed = true;
    allDecos.push(...decos);
  });

  return changed ? DecorationSet.create(doc, allDecos) : DecorationSet.empty;
}

const highlightProsePlugin = $prose(() => highlightPlugin);

const highlightPlugin = new Plugin({
  key: new PluginKey('md-highlight'),
  state: {
    init: (_config, state) => highlightCodeBlock(state.doc),
    apply: (tr, old) => (tr.docChanged ? highlightCodeBlock(tr.doc) : old),
  },
  props: {
    decorations(state) {
      return highlightPlugin.getState(state);
    },
  },
});

function postToHost(message) {
  if (window.chrome?.webview) {
    window.chrome.webview.postMessage(JSON.stringify(message));
  }
}

function applyTheme(dark) {
  const link = document.getElementById('md-theme');
  if (link) {
    link.href = dark ? 'github-markdown-dark.css' : 'github-markdown.css';
  }
  document.body.dataset.theme = dark ? 'dark' : 'light';
}

export async function initEditor(mount) {
  getStarryNight().catch(() => {});
  const editor = await Editor.make()
    .config((ctx) => {
      ctx.set(rootCtx, mount);
      ctx.set(defaultValueCtx, DEFAULT_MARKDOWN);
    })
    .use(commonmark)
    .use(gfm)
    .use(history)
    .use(clipboard)
    .use(listener)
    .use(highlightProsePlugin)
    .config((ctx) => {
      const l = ctx.get(listenerCtx);
      l.markdownUpdated((_ctx, markdown) => {
        lastMarkdown = markdown;
        postToHost({ type: 'content', markdown });
      });
    })
    .create();

  window.MarkdownEditor.editor = editor;

  mount.addEventListener(
    'pointerdown',
    () => {
      const editorInstance = window.MarkdownEditor?.editor;
      if (!editorInstance) return;
      try {
        editorInstance.action((ctx) => ctx.get(editorViewCtx)).focus();
      } catch {
        const editable = mount.querySelector('.ProseMirror') || mount;
        if (editable && typeof editable.focus === 'function') {
          editable.focus();
        }
      }
    },
    true,
  );

  postToHost({ type: 'ready' });
  return editor;
}

window.MarkdownEditor = {
  editor: null,
  onMarkdownChange: null,
  async init(mount) {
    this.editor = await initEditor(mount);
  },
  getMarkdown() {
    return lastMarkdown;
  },
  loadMarkdown(markdown) {
    if (this.editor) {
      lastMarkdown = markdown;
      this.editor.action(replaceAll(markdown));
    }
  },
};

if (window.chrome?.webview) {
  window.chrome.webview.addEventListener('message', (event) => {
    try {
      const data = JSON.parse(event.data);
      if (data.type === 'load') {
        window.MarkdownEditor.loadMarkdown(data.markdown ?? '');
      } else if (data.type === 'theme') {
        applyTheme(!!data.dark);
      }
    } catch {
      // ignore malformed messages
    }
  });
}
