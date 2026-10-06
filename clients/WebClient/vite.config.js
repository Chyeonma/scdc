import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

const apiTarget = process.env.SCDC_DEV_API_TARGET || 'http://localhost:5026'

export default defineConfig({
  plugins: [react()],
  server: {
    host: '0.0.0.0',
    port: 3000,
    proxy: {
      '/api': apiTarget,
      '/hubs': {
        target: apiTarget,
        ws: true,
      },
      '/swagger': apiTarget,
    },
  },
})
