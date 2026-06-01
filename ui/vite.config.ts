import { defineConfig } from 'vite'
import solid from 'vite-plugin-solid'
import path from 'node:path'
import { fileURLToPath } from 'node:url'

const __dirname = path.dirname(fileURLToPath(import.meta.url))

export default defineConfig({
  plugins: [solid()],
  build: {
    rollupOptions: {
      input: path.resolve(__dirname, 'src/entry.tsx'),
      output: {
        format: 'es',
        entryFileNames: 'bundle.js',
        chunkFileNames: 'bundle-[name].js',
        assetFileNames: 'bundle-[name][extname]',
      },
    },
  },
})
