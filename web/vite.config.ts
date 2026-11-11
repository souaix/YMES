import { defineConfig, loadEnv } from 'vite'
import vue from '@vitejs/plugin-vue'

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), 'VITE_')
  const apiTarget = env.VITE_API_PROXY_TARGET?.trim() || 'http://127.0.0.1:5080'
  const explicitStripPathBase = env.VITE_API_PROXY_STRIP_PATH_BASE?.trim().toLowerCase()
  const isLocalApi = ['localhost', '127.0.0.1', '::1'].includes(new URL(apiTarget).hostname)
  const stripApiPathBase = explicitStripPathBase === 'true' ||
    (explicitStripPathBase !== 'false' && isLocalApi)

  return {
    base: '/YMES/',
    plugins: [vue()],
    server: {
      host: '127.0.0.1',
      port: 5173,
      strictPort: true,
      proxy: {
        '/YMES/API': {
          target: apiTarget,
          changeOrigin: true,
          ...(stripApiPathBase ? { rewrite: (path: string) => path.replace(/^\/YMES\/API/, '') } : {}),
        },
      },
    },
  }
})
