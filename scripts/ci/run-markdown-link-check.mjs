#!/usr/bin/env node
import fs from 'node:fs';
import path from 'node:path';
import http from 'node:http';
import https from 'node:https';

const files = process.argv.slice(2).filter((file) => file && file.trim().length > 0);

if (files.length === 0) {
  console.log('No Markdown files selected for link checking.');
  process.exit(0);
}

const repoRoot = path.join(path.dirname(new URL(import.meta.url).pathname), '..', '..');
const ignorePatterns = [
  /^mailto:/i,
  /^tel:/i,
  /^javascript:/i,
  /^#/,
];

const okStatusCodes = new Set([200, 201, 202, 203, 204, 206, 301, 302, 307, 308, 401, 403, 429]);
const timeoutMs = 15000;
const maxConcurrent = 6;
const failures = [];

const requestUrl = (urlObject, method) => {
  const client = urlObject.protocol === 'https:' ? https : http;
  return new Promise((resolve) => {
    const request = client.request(urlObject, { method, timeout: timeoutMs }, (response) => {
      const statusCode = response.statusCode ?? 0;
      const location = response.headers.location;
      response.resume();
      resolve({ statusCode, location });
    });

    request.on('timeout', () => {
      request.destroy(new Error('timeout'));
      resolve({ statusCode: 0, location: undefined });
    });

    request.on('error', () => resolve({ statusCode: 0, location: undefined }));
    request.end();
  });
};

const checkHttpLink = async (url, depth = 0) => {
  if (depth > 5) {
    return false;
  }

  const parsed = new URL(url);
  let { statusCode, location } = await requestUrl(parsed, 'HEAD');

  if ([405, 501].includes(statusCode)) {
    ({ statusCode } = await requestUrl(parsed, 'GET'));
  }

  if ([301, 302, 307, 308].includes(statusCode) && location) {
    const resolved = new URL(location, parsed);
    return checkHttpLink(resolved.toString(), depth + 1);
  }

  return okStatusCodes.has(statusCode);
};

const checkRelativeLink = (link, originatingFile) => {
  const baseDir = path.dirname(path.resolve(originatingFile));
  const normalized = link.startsWith('/') ? path.join(repoRoot, link) : path.resolve(baseDir, link);
  return fs.existsSync(normalized);
};

const extractLinks = (content) => {
  const links = [];
  const referenceDefinitions = new Map();
  const referenceDefRegex = /^\s*\[([^\]]+)\]:\s*(\S+)/gm;
  let match;
  while ((match = referenceDefRegex.exec(content)) !== null) {
    referenceDefinitions.set(match[1].toLowerCase(), match[2]);
  }

  const inlineRegex = /(!)?\[[^\]]+\]\(([^)\s]+)(?:\s+"[^"]*")?\)/g;
  while ((match = inlineRegex.exec(content)) !== null) {
    if (match[1] === '!') {
      continue;
    }
    links.push(match[2].replace(/^<|>$/g, ''));
  }

  const referenceRegex = /(!)?\[[^\]]+\]\[([^\]]+)\]/g;
  while ((match = referenceRegex.exec(content)) !== null) {
    if (match[1] === '!') {
      continue;
    }
    const target = referenceDefinitions.get(match[2].toLowerCase());
    if (target) {
      links.push(target.replace(/^<|>$/g, ''));
    }
  }

  const autoLinkRegex = /<(https?:\/\/[^>]+)>/g;
  while ((match = autoLinkRegex.exec(content)) !== null) {
    links.push(match[1]);
  }

  return links;
};

const checkLink = async (link, file) => {
  if (ignorePatterns.some((pattern) => pattern.test(link))) {
    return true;
  }

  if (link.startsWith('http://') || link.startsWith('https://')) {
    return await checkHttpLink(link);
  }

  return checkRelativeLink(link, file);
};

const queue = [];
const enqueue = async (task) => {
  if (queue.length >= maxConcurrent) {
    await Promise.race(queue);
  }

  const work = task().finally(() => {
    const index = queue.indexOf(work);
    if (index >= 0) {
      queue.splice(index, 1);
    }
  });

  queue.push(work);
  return work;
};

for (const file of files) {
  const absoluteFile = path.resolve(file);
  if (!fs.existsSync(absoluteFile)) {
    continue;
  }

  console.log(`Checking links in ${file}...`);
  const contents = fs.readFileSync(absoluteFile, 'utf8');
  const links = extractLinks(contents);

  for (const link of links) {
    await enqueue(async () => {
      const ok = await checkLink(link, absoluteFile);
      if (!ok) {
        failures.push({ file, link });
        console.error(`  ✗ ${link}`);
      }
    });
  }
}

await Promise.all(queue);

if (failures.length > 0) {
  console.error(`Detected ${failures.length} broken link(s).`);
  process.exit(1);
}

console.log('All checked Markdown links look good.');
