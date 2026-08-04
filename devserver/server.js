'use strict';

const http = require('http');
const fs = require('fs');
const path = require('path');

const PORT = process.env.PORT || 8080;
const CONFIG_DIR = process.env.CONFIG_DIR || path.join(__dirname, 'config');
const FIXTURES_DIR = process.env.FIXTURES_DIR || path.join(__dirname, 'fixtures');
const WWWROOT = path.join(__dirname, 'wwwroot');
const JELLYFIN_WEB = path.join(__dirname, 'jellyfin-web');

const MIME = {
    '.html':  'text/html',
    '.js':    'application/javascript',
    '.json':  'application/json',
    '.css':   'text/css',
    '.png':   'image/png',
    '.jpg':   'image/jpeg',
    '.gif':   'image/gif',
    '.svg':   'image/svg+xml',
    '.ico':   'image/x-icon',
    '.woff':  'font/woff',
    '.woff2': 'font/woff2',
    '.map':   'application/json',
};

// --- Parse Jellyfin CSS bundle paths and inline styles from its index.html ---
const jellyfinAssets = { css: [], inlineStyles: '' };
try {
    const jfIndex = fs.readFileSync(path.join(JELLYFIN_WEB, 'index.html'), 'utf8');

    // Extract <link rel="stylesheet"> hrefs
    const linkTag = /<link\b([^>]*)>/gi;
    let m;
    while ((m = linkTag.exec(jfIndex)) !== null) {
        const attrs = m[1];
        if (/rel=["']stylesheet["']/i.test(attrs)) {
            const href = (attrs.match(/href=["']([^"']+)["']/i) || [])[1];
            if (href) jellyfinAssets.css.push(href.replace(/^\.\//, ''));
        }
    }

    // Extract inline <style> blocks (contains .preload { background-color: #101010 } etc.)
    const styleBlocks = [];
    for (const sm of jfIndex.matchAll(/<style[^>]*>([\s\S]*?)<\/style>/gi)) {
        styleBlocks.push(sm[1]);
    }
    jellyfinAssets.inlineStyles = styleBlocks.join('\n');

    console.log(`Jellyfin web: ${jellyfinAssets.css.length} CSS bundle(s), ${styleBlocks.length} inline style block(s) found`);
} catch {
    console.warn('jellyfin-web/ not found — Jellyfin styles will not be applied');
    console.warn('Rebuild the container to include Jellyfin web assets');
}

// --- SSE clients for hot reload ---
const sseClients = new Set();

fs.watch(CONFIG_DIR, { recursive: true }, (_event, filename) => {
    for (const client of sseClients) {
        client.write(`data: ${filename}\n\n`);
    }
});

// --- Helpers ---
function serveFile(res, filePath) {
    fs.readFile(filePath, (err, data) => {
        if (err) { res.writeHead(404); res.end('Not found'); return; }
        const ext = path.extname(filePath).split('?')[0];
        res.writeHead(200, { 'Content-Type': MIME[ext] || 'application/octet-stream' });
        res.end(data);
    });
}

function readFixture(name, fallback) {
    try { return JSON.parse(fs.readFileSync(path.join(FIXTURES_DIR, name), 'utf8')); }
    catch { return fallback; }
}

function writeFixture(name, data) {
    fs.writeFileSync(path.join(FIXTURES_DIR, name), JSON.stringify(data, null, 2));
}

function json(res, data, status = 200) {
    res.writeHead(status, { 'Content-Type': 'application/json' });
    res.end(JSON.stringify(data));
}

function readBody(req) {
    return new Promise((resolve, reject) => {
        let body = '';
        req.on('data', chunk => body += chunk);
        req.on('end', () => { try { resolve(JSON.parse(body)); } catch (e) { reject(e); } });
    });
}

// --- Server ---
const server = http.createServer(async (req, res) => {
    const { pathname } = new URL(req.url, `http://localhost:${PORT}`);

    res.setHeader('Access-Control-Allow-Origin', '*');
    res.setHeader('Access-Control-Allow-Headers', 'Content-Type');

    if (req.method !== 'OPTIONS' && !pathname.startsWith('/web/')) {
        console.log(`${req.method} ${pathname}`);
    }

    if (req.method === 'OPTIONS') { res.writeHead(204); res.end(); return; }

    // Hot reload SSE
    if (pathname === '/api/hotreload') {
        res.writeHead(200, {
            'Content-Type':  'text/event-stream',
            'Cache-Control': 'no-cache',
            'Connection':    'keep-alive',
        });
        res.write(':\n\n');
        sseClients.add(res);
        req.on('close', () => sseClients.delete(res));
        return;
    }

    // Jellyfin CSS bundle paths discovered at startup
    if (pathname === '/api/jellyfin-assets' && req.method === 'GET') {
        json(res, jellyfinAssets);
        return;
    }

    // Mock: virtual folders (read-only fixture)
    if (pathname === '/api/virtualfolders' && req.method === 'GET') {
        json(res, readFixture('virtual-folders.json', []));
        return;
    }

    // Mock: plugin configuration (read + write)
    if (pathname === '/api/pluginconfiguration') {
        if (req.method === 'GET') {
            json(res, readFixture('plugin-config.json', {}));
            return;
        }
        if (req.method === 'POST') {
            try {
                const config = await readBody(req);
                writeFixture('plugin-config.json', config);
                json(res, { success: true });
            } catch {
                json(res, { error: 'invalid json' }, 400);
            }
            return;
        }
    }

    // Jellyfin web static assets (CSS bundles, fonts, icons)
    if (pathname.startsWith('/web/')) {
        serveFile(res, path.join(JELLYFIN_WEB, pathname.slice('/web/'.length)));
        return;
    }

    // Mounted Configuration directory at /config/*
    if (pathname.startsWith('/config/')) {
        serveFile(res, path.join(CONFIG_DIR, pathname.slice('/config/'.length)));
        return;
    }

    // Static files from wwwroot
    serveFile(res, path.join(WWWROOT, pathname === '/' ? 'index.html' : pathname));
});

server.listen(PORT, () => {
    console.log(`Cinema Mode dev server → http://localhost:${PORT}`);
    console.log(`  config dir : ${CONFIG_DIR}`);
    console.log(`  fixtures   : ${FIXTURES_DIR}`);
});
