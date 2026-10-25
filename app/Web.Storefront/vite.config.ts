import { fileURLToPath, URL } from 'node:url'

import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import vueDevTools from 'vite-plugin-vue-devtools'
import stylex from '@stylexjs/unplugin'

const stylexPlugin = stylex.vite({ useCSSLayers: true })

// Validate: the stylex dev-server hook keeps Vitest alive ~10s after tests
// unless it is removed under Vitest (documented @stylexjs/unplugin caveat).
if (process.env.VITEST) {
  delete stylexPlugin.configureServer
}

// https://vite.dev/config/
export default defineConfig({
  plugins: [stylexPlugin, vue(), vueDevTools()],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
  server: {
    port: 5173,
    proxy: {
      '/health': {
        target: process.env.API_BASE_URL ?? 'http://localhost:5080',
        changeOrigin: true,
      },
      '/api': {
        target: process.env.API_BASE_URL ?? 'http://localhost:5080',
        changeOrigin: true,
      },
    },
  },
})
