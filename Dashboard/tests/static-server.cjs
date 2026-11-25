const { createServer } = require("node:http");
const { readFile } = require("node:fs/promises");
const path = require("node:path");

const dashboardDirectory = __dirname;
const publicDirectory = path.resolve(dashboardDirectory, "..", "public");
const port = Number(process.env.PORT ?? 4173);
const mimeTypes = new Map([
  [".css", "text/css; charset=utf-8"],
  [".html", "text/html; charset=utf-8"],
  [".js", "text/javascript; charset=utf-8"],
  [".svg", "image/svg+xml"],
]);

const server = createServer(async (request, response) => {
  if (request.method !== "GET" && request.method !== "HEAD") {
    response.writeHead(405, { Allow: "GET, HEAD" });
    response.end();
    return;
  }

  let pathname;
  try {
    pathname = decodeURIComponent(new URL(request.url, "http://localhost").pathname);
  } catch {
    response.writeHead(400);
    response.end("Invalid request path");
    return;
  }

  const filePath = path.resolve(publicDirectory, `.${pathname === "/" ? "/index.html" : pathname}`);
  const relativePath = path.relative(publicDirectory, filePath);
  if (relativePath.startsWith("..") || path.isAbsolute(relativePath)) {
    response.writeHead(403);
    response.end("Forbidden");
    return;
  }

  let file;
  try {
    file = await readFile(filePath);
  } catch (error) {
    if (error.code === "ENOENT" || error.code === "EISDIR") {
      response.writeHead(404);
      response.end("Not found");
      return;
    }

    response.writeHead(500);
    response.end("Unable to read requested file");
    console.error(`Unable to serve ${filePath}:`, error);
    return;
  }

  response.writeHead(200, {
    "Content-Security-Policy": "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data: https://tile.openstreetmap.org; connect-src 'self'; object-src 'none'; base-uri 'self'; frame-ancestors 'none'",
    "Content-Type": mimeTypes.get(path.extname(filePath)) ?? "application/octet-stream",
    "X-Content-Type-Options": "nosniff",
  });
  response.end(request.method === "HEAD" ? undefined : file);
});

server.listen(port, "127.0.0.1", () => {
  console.log(`Dashboard test server listening on http://127.0.0.1:${port}`);
});
