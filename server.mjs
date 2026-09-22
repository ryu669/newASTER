import http from 'node:http';
import { readFile } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import path from 'node:path';

const root = fileURLToPath(new URL('./', import.meta.url));
const types = { '.html': 'text/html; charset=utf-8', '.css': 'text/css; charset=utf-8', '.mjs': 'text/javascript; charset=utf-8' };
const port = Number(process.env.PORT || 4173);
const server = http.createServer(async (req, res) => {
  try {
    const name = decodeURIComponent(new URL(req.url, 'http://localhost').pathname);
    const relative = name === '/' ? 'index.html' : name.slice(1);
    const file = path.resolve(root, relative);
    if (!file.startsWith(root) || !['index.html', 'style.css', 'src/engine.mjs', 'src/main.mjs'].includes(relative)) {
      res.writeHead(404).end('Not found'); return;
    }
    const data = await readFile(file);
    res.writeHead(200, { 'Content-Type': types[path.extname(file)] || 'application/octet-stream', 'Cache-Control': 'no-store' });
    res.end(data);
  } catch { res.writeHead(404).end('Not found'); }
});
server.on('error', error => {
  console.error(error.code === 'EADDRINUSE'
    ? `Port ${port} is already in use. Open the running game, or set PORT to another number.`
    : `Unable to start newASTER: ${error.message}`);
  process.exitCode = 1;
});
if (!Number.isInteger(port) || port < 1 || port > 65535) {
  console.error('PORT must be an integer between 1 and 65535.'); process.exitCode = 1;
} else server.listen(port, '127.0.0.1', () => console.log(`newASTER: http://127.0.0.1:${port}`));
