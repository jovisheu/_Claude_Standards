import { fileURLToPath, URL } from 'node:url'
import { defineConfig, loadEnv } from 'vite'
import vue from '@vitejs/plugin-vue'
import tailwindcss from '@tailwindcss/vite'

// 部署子路徑（VITE_APP_BASE）只在建置時套用，開發固定為 /
export default defineConfig(({ command, mode }) => {
  const env = loadEnv(mode, fileURLToPath(new URL('.', import.meta.url)), 'VITE_')
  return {
    base: command === 'build' ? env.VITE_APP_BASE || '/' : '/',
    plugins: [vue(), tailwindcss()],
    resolve: {
      alias: {
        '@': fileURLToPath(new URL('./src', import.meta.url)),
      },
    },
    server: {
      // 固定 5173（後端 Development 的 App:FrontendUrl 指向此 port，被占用時直接失敗而不改用其他 port）
      port: 5173,
      strictPort: true,
      proxy: {
        '/api': 'http://localhost:__BACKEND_PORT__',
      },
    },
  }
})
