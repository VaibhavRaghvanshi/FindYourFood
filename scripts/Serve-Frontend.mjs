import http from 'node:http';
import { createReadStream } from 'node:fs';
import { stat } from 'node:fs/promises';
import path from 'node:path';
import { pipeline } from 'node:stream/promises';

const [directory, portText] = process.argv.slice(2);
const root = path.resolve(directory ?? '');
const port = Number(portText);
if (!directory || !Number.isInteger(port) || port < 1024 || port > 65535) throw new Error('Supply a built frontend directory and port.');
await stat(path.join(root, 'index.html'));
const types = { '.html': 'text/html; charset=utf-8', '.js': 'text/javascript; charset=utf-8',
  '.css': 'text/css; charset=utf-8', '.json': 'application/json', '.png': 'image/png',
  '.svg': 'image/svg+xml', '.ico': 'image/x-icon', '.woff2': 'font/woff2', '.webp': 'image/webp' };
const server = http.createServer(async (request, response) => {
  try {
    const url = new URL(request.url ?? '/', 'http://localhost');
    if (url.pathname.startsWith('/api/')) {
      // Development reverse proxy; business authorization and body limits remain in YARP and the service.
      const headers = { ...request.headers, host: 'localhost:5100' };
      delete headers.connection;
      const upstream = http.request({ hostname: 'localhost', port: 5100, path: url.pathname + url.search,
        method: request.method, headers }, (result) => {
        response.writeHead(result.statusCode ?? 502, { ...result.headers, 'cache-control': 'no-store' });
        result.pipe(response);
      });
      upstream.setTimeout(20_000, () => upstream.destroy(new Error('Gateway timeout.')));
      upstream.on('error', () => {
        if (!response.headersSent) response.writeHead(502, { 'content-type': 'application/json' });
        response.end('{"title":"Gateway unavailable"}');
      });
      request.on('aborted', () => upstream.destroy());
      request.pipe(upstream);
      return;
    }
    if (request.method !== 'GET' && request.method !== 'HEAD') {
      response.writeHead(405, { allow: 'GET, HEAD' }); response.end(); return;
    }
    const decoded = decodeURIComponent(url.pathname);
    if (decoded.split(/[\\/]/).some((segment) => segment.startsWith('.') || segment.includes(':'))) {
      response.writeHead(404); response.end(); return;
    }
    let file = path.resolve(root, '.' + decoded);
    if (file !== root && !file.startsWith(root + path.sep)) { response.writeHead(404); response.end(); return; }
    let details = await stat(file).catch(() => null);
    if (details?.isDirectory()) { file = path.join(file, 'index.html'); details = await stat(file).catch(() => null); }
    if (!details?.isFile()) {
      if (path.extname(decoded)) { response.writeHead(404); response.end(); return; }
      file = path.join(root, 'index.html');
      details = await stat(file);
    }
    response.writeHead(200, { 'content-type': types[path.extname(file)] ?? 'application/octet-stream',
      'content-length': details.size, 'cache-control': 'no-cache', 'x-content-type-options': 'nosniff',
      'referrer-policy': 'same-origin' });
    if (request.method === 'HEAD') response.end();
    else await pipeline(createReadStream(file), response);
  } catch {
    if (!response.headersSent) response.writeHead(400);
    response.end();
  }
});
server.listen(port, 'localhost', () => process.stdout.write('Built frontend is listening on localhost:' + port + '\n'));
for (const signal of ['SIGINT', 'SIGTERM']) process.on(signal, () => server.close(() => process.exit(0)));
