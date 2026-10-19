/** @type {import('next').NextConfig} */
const nextConfig = {
  output: 'export',
  basePath: '/fluentpass',
  images: {
    unoptimized: true,
  },
};

module.exports = nextConfig;
