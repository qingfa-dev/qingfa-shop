import { flushPromises, mount, type VueWrapper } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import App from '../../App.vue'
import router from '../../router'

describe('App health integration', () => {
  let wrapper: VueWrapper | undefined

  beforeEach(async () => {
    await router.push('/')
    await router.isReady()
  })

  afterEach(() => {
    wrapper?.unmount()
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  it('reports the API as unreachable when /health answers with a failure status', async () => {
    const fetchMock = vi.fn().mockResolvedValue({ ok: false })
    vi.stubGlobal('fetch', fetchMock)

    wrapper = mount(App, { global: { plugins: [router] } })
    await flushPromises()

    expect(wrapper.get('[data-test="health-chip"]').text()).toContain('unreachable')
    expect(fetchMock).toHaveBeenCalledWith('/health')
  })

  it('reports the API as unreachable when /health cannot be reached', async () => {
    const fetchMock = vi.fn().mockRejectedValue(new Error('offline'))
    vi.stubGlobal('fetch', fetchMock)

    wrapper = mount(App, { global: { plugins: [router] } })
    await flushPromises()

    expect(wrapper.get('[data-test="health-chip"]').text()).toContain('unreachable')
  })
})
