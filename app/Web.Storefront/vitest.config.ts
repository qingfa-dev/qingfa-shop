import { fileURLToPath } from 'node:url'
import { mergeConfig, defineConfig, configDefaults } from 'vitest/config'
import viteConfig from './vite.config.ts'

const lines = Number(process.env.COVERAGE_LINES ?? 90)
const branches = Number(process.env.COVERAGE_BRANCHES ?? 80)

export default mergeConfig(
  viteConfig,
  defineConfig({
    test: {
      environment: 'jsdom',
      exclude: [...configDefaults.exclude, 'e2e/**'],
      root: fileURLToPath(new URL('./', import.meta.url)),
      coverage: {
        provider: 'v8',
        reporter: ['text', 'json-summary', 'cobertura'],
        reportsDirectory: '../../artifacts/coverage/web-storefront',
        include: ['src/**/*.{ts,vue}'],
        exclude: [
          'src/main.ts',
          'src/**/*.d.ts',
          'src/**/__tests__/**',
          'src/**/*.spec.ts',
          'src/**/*.test.ts',
        ],
        thresholds: { lines, branches },
      },
    },
  }),
)
