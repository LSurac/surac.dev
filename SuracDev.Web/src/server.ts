import {
  AngularNodeAppEngine,
  createNodeRequestHandler,
  isMainModule,
  writeResponseToNodeResponse,
} from '@angular/ssr/node';
import { createReadStream, statSync } from 'node:fs';
import { createServer, IncomingMessage, ServerResponse } from 'node:http';
import { extname, join, normalize, sep } from 'node:path';
import { Readable } from 'node:stream';
import { pipeline } from 'node:stream/promises';
import type { ServerRequestContext } from './app/core/http/server-request-context';
import { buildSitemap } from './app/core/seo/sitemap';

/**
 * Minimal FileReader for the generated API clients. NSwag's Angular template reads every response
 * as a Blob through FileReader, which Node does not have; without this shim every API call fails
 * during server rendering (docs/gotchas.md). On the server the bodies arrive as ArrayBuffer (see
 * ApiBaseUrlBackend), so both are read.
 */
class NodeFileReader {
  onload: ((event: { target: NodeFileReader }) => void) | null = null;
  onerror: ((event: { target: NodeFileReader }) => void) | null = null;
  result: string | null = null;
  error: unknown = null;

  readAsText(body: Blob | ArrayBuffer): void {
    const text = body instanceof ArrayBuffer ? Promise.resolve(new TextDecoder().decode(body)) : body.text();
    text.then(
      (text) => {
        this.result = text;
        this.onload?.({ target: this });
      },
      (error: unknown) => {
        this.error = error;
        this.onerror?.({ target: this });
      },
    );
  }
}

(globalThis as Record<string, unknown>)['FileReader'] ??= NodeFileReader;

const browserDistFolder = join(import.meta.dirname, '../browser');
const apiBaseUrl = process.env['API_BASE_URL'] ?? 'http://localhost:5180';

/**
 * Angular SSR rejects every request whose Host header is not allowed (SSRF protection). The public
 * host names come from angular.json (`security.allowedHosts`, merged by the engine); the hosts of a
 * given deployment (staging name, loopback) come from NG_ALLOWED_HOSTS, so no private host name
 * is in the repository. NG_TRUST_PROXY_HEADERS names the X-Forwarded-* headers nginx sets.
 */
const listFromEnv = (name: string): string[] | undefined =>
  process.env[name]
    ?.split(',')
    .map((entry) => entry.trim())
    .filter(Boolean);

const angularApp = new AngularNodeAppEngine({
  allowedHosts: listFromEnv('NG_ALLOWED_HOSTS') ?? ['localhost', '127.0.0.1'],
  trustProxyHeaders: listFromEnv('NG_TRUST_PROXY_HEADERS') ?? ['x-forwarded-proto', 'x-forwarded-for'],
});

const contentTypes: Record<string, string> = {
  '.css': 'text/css; charset=utf-8',
  '.html': 'text/html; charset=utf-8',
  '.ico': 'image/x-icon',
  '.js': 'text/javascript; charset=utf-8',
  '.json': 'application/json; charset=utf-8',
  '.mjs': 'text/javascript; charset=utf-8',
  '.png': 'image/png',
  '.svg': 'image/svg+xml',
  '.txt': 'text/plain; charset=utf-8',
  '.webmanifest': 'application/manifest+json; charset=utf-8',
  '.webp': 'image/webp',
  '.woff2': 'font/woff2',
  '.xml': 'application/xml; charset=utf-8',
};

/** Hashed build output (`main-ABC12345.js`) and versioned fonts (`-v1.woff2`) never change under their name. */
const immutablePattern = /(-[A-Z0-9]{8}\.[a-z0-9]+|-v\d+\.woff2)$/;

function tryServeStatic(req: IncomingMessage, res: ServerResponse): boolean {
  if (req.method !== 'GET' && req.method !== 'HEAD') {
    return false;
  }

  const url = new URL(req.url ?? '/', 'http://localhost');
  const relativePath = normalize(decodeURIComponent(url.pathname)).replace(/^(\.\.(\/|\\|$))+/, '');
  const filePath = join(browserDistFolder, relativePath);
  if (!filePath.startsWith(browserDistFolder + sep)) {
    return false;
  }

  let isFile: boolean;
  try {
    isFile = statSync(filePath).isFile();
  } catch {
    return false;
  }
  if (!isFile) {
    return false;
  }

  res.setHeader('Content-Type', contentTypes[extname(filePath)] ?? 'application/octet-stream');
  res.setHeader(
    'Cache-Control',
    immutablePattern.test(filePath)
      ? 'public, max-age=31536000, immutable'
      : 'public, max-age=0, must-revalidate',
  );
  if (req.method === 'HEAD') {
    res.end();
    return true;
  }
  createReadStream(filePath).pipe(res);
  return true;
}

/**
 * Forwards `/api/*` to the API. In the containers nginx does this before the request reaches node;
 * here it makes `npm run serve:ssr` and the end-to-end suite complete without nginx.
 */
async function proxyApi(req: IncomingMessage, res: ServerResponse): Promise<void> {
  const headers = new Headers();
  for (const [name, value] of Object.entries(req.headers)) {
    if (value !== undefined && name !== 'host' && name !== 'connection') {
      headers.set(name, Array.isArray(value) ? value.join(', ') : value);
    }
  }
  const hasBody = req.method !== 'GET' && req.method !== 'HEAD';
  const upstream = await fetch(apiBaseUrl + req.url, {
    method: req.method,
    headers,
    body: hasBody ? (Readable.toWeb(req) as ReadableStream) : undefined,
    // Required by fetch when streaming a request body.
    ...(hasBody ? { duplex: 'half' } : {}),
    redirect: 'manual',
  } as RequestInit);

  res.statusCode = upstream.status;
  upstream.headers.forEach((value, name) => {
    if (name !== 'content-encoding' && name !== 'transfer-encoding') {
      res.setHeader(name, value);
    }
  });
  if (upstream.body) {
    await pipeline(Readable.fromWeb(upstream.body as import('node:stream/web').ReadableStream), res);
  } else {
    res.end();
  }
}

/**
 * Drops X-Forwarded-* headers that arrive empty. nginx forwards `$http_x_forwarded_proto` as is,
 * which is an empty string when no proxy sits in front of it (staging). Angular trusts the header
 * and builds the request URL `://host/` from it, which makes every server-side API call fail
 * without a log line (docs/gotchas.md).
 */
function dropEmptyForwardedHeaders(req: IncomingMessage): void {
  for (const name of Object.keys(req.headers)) {
    if (name.startsWith('x-forwarded-') && !String(req.headers[name] ?? '').trim()) {
      delete req.headers[name];
    }
  }
}

/**
 * Answers /sitemap.xml with the start page, the legal pages and every visible case study. The slugs
 * come from the public API on each request, so a new project appears without a deploy.
 */
async function serveSitemap(res: ServerResponse): Promise<void> {
  const response = await fetch(`${apiBaseUrl}/api/public/projects?locale=de`);
  if (!response.ok) {
    throw new Error(`Projects for the sitemap answered ${response.status}.`);
  }

  const projects = (await response.json()) as { slug?: string }[];
  const slugs = projects.map((project) => project.slug).filter((slug): slug is string => !!slug);
  res.statusCode = 200;
  res.setHeader('Content-Type', 'application/xml; charset=utf-8');
  res.setHeader('Cache-Control', 'public, max-age=3600');
  res.end(buildSitemap(slugs));
}

function handle(req: IncomingMessage, res: ServerResponse): void {
  dropEmptyForwardedHeaders(req);
  if (req.method === 'GET' && req.url?.split('?')[0] === '/sitemap.xml') {
    serveSitemap(res).catch((error: unknown) => {
      console.error(error);
      res.statusCode = 503;
      res.end('Sitemap unavailable');
    });
    return;
  }

  if (req.url?.startsWith('/api/')) {
    proxyApi(req, res).catch((error: unknown) => {
      console.error(error);
      res.statusCode = 502;
      res.end('Bad gateway');
    });
    return;
  }

  if (tryServeStatic(req, res)) {
    return;
  }

  const context: ServerRequestContext = { apiBaseUrl };
  angularApp
    .handle(req, context)
    .then((response) => {
      if (response) {
        return writeResponseToNodeResponse(response, res);
      }
      res.statusCode = 404;
      res.setHeader('Content-Type', 'text/plain; charset=utf-8');
      res.end('Not found');
      return undefined;
    })
    .catch((error: unknown) => {
      console.error(error);
      res.statusCode = 500;
      res.end('Internal server error');
    });
}

if (isMainModule(import.meta.url)) {
  const port = Number(process.env['PORT'] ?? 4000);
  createServer(handle).listen(port, () => {
    console.log(`surac.dev SSR listening on http://localhost:${port}, API at ${apiBaseUrl}`);
  });
}

/** Used by the Angular CLI dev server and during the build. */
export const reqHandler = createNodeRequestHandler(handle);
