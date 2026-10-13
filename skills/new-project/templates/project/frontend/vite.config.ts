import { fileURLToPath, URL } from 'node:url'
import { defineConfig, loadEnv } from 'vite'
import vue from '@vitejs/plugin-vue'
import tailwindcss from '@tailwindcss/vite'

const root = fileURLToPath(new URL('.', import.meta.url))

export default defineConfig(({ command, mode }) => {
  const env = loadEnv(mode, root, 'VITE_')

  return {
    // 部署子路徑只在建置時套用，開發固定為 /
    base: command === 'build' ? env.VITE_APP_BASE || '/' : '/',
    plugins: [vue(), tailwindcss()],
    resolve: {
      alias: {
        '@': fileURLToPath(new URL('./src', import.meta.url)),
      },
    },
    server: {
      port: 5173,
      strictPort: true,
      proxy: {
        '/api': {
          target: 'http://localhost:__BACKEND_PORT__',
        },
      },
    },
  }
})
