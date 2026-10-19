const { execSync } = require('child_process');

let defaultVersion = 'v1.2.0';
try {
  const gitTag = execSync('git describe --tags --abbrev=0', { stdio: ['ignore', 'pipe', 'ignore'] })
    .toString()
    .trim();
  if (gitTag) {
    defaultVersion = gitTag.startsWith('v') ? gitTag : `v${gitTag}`;
  }
} catch {
  // Ignore and use fallback
}

/** @type {import('next').NextConfig} */
const nextConfig = {
  output: 'export',
  basePath: '/fluentpass',
  images: {
    unoptimized: true,
  },
  env: {
    NEXT_PUBLIC_APP_VERSION: process.env.NEXT_PUBLIC_APP_VERSION || defaultVersion,
  },
};

module.exports = nextConfig;
