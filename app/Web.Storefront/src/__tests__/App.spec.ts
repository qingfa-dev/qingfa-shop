import { flushPromises, mount, type VueWrapper } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import App from '../App.vue'
import router from '../router'

describe('App', () => {
  let wrapper: VueWrapper

  beforeEach(async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ ok: true }))
    await router.push('/')
    await router.isReady()
    wrapper = mount(App, { global: { plugins: [router] } })
    await flushPromises()
  })

  afterEach(() => {
    wrapper.unmount()
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  it('renders the shell with the storefront heading and a healthy API chip', () => {
    expect(wrapper.get('[data-test="app-title"]').text()).toBe('QingFa.Shop')
    expect(wrapper.get('[data-test="health-chip"]').text()).toContain('healthy')
  })

  it('shows the not-found view for unknown routes', async () => {
    await router.push('/definitely-missing')
    await flushPromises()

    expect(wrapper.get('[data-test="not-found"]').text()).toBe('Page not found')
  })
})
