import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

const apiTarget = process.env.SCDC_DEV_API_TARGET || 'http://localhost:5026'
const mediaTarget = process.env.SCDC_DEV_MEDIA_TARGET || apiTarget

export default defineConfig({
  plugins: [react()],
  server: {
    host: '0.0.0.0',
    port: 3000,
    proxy: {
      '/api/v1/media': mediaTarget,
      '/hubs/media': { target: mediaTarget, ws: true },
      '/livekit': { target: mediaTarget, ws: true },
      '/api': apiTarget,
      '/hubs': {
        target: apiTarget,
        ws: true,
      },
      '/swagger': apiTarget,
    },
  },
})
