import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    // Same-origin proxy so the API's auth cookie works in development.
    proxy: { '/api': 'http://localhost:5080' },
  },
  build: {
    // The API serves the built app from its wwwroot in production.
    outDir: '../backend/Unfiltered.Api/wwwroot',
    emptyOutDir: true,
  },
});
