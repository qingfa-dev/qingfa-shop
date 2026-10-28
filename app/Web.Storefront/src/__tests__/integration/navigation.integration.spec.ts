import { flushPromises, mount, type VueWrapper } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import App from '../../App.vue'
import router from '../../router'

describe('App navigation integration', () => {
  let wrapper: VueWrapper | undefined

  beforeEach(async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ ok: true }))
    await router.push('/definitely-missing')
    await router.isReady()
    wrapper = mount(App, { global: { plugins: [router] } })
    await flushPromises()
  })

  afterEach(() => {
    wrapper?.unmount()
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  it('renders the not-found view for an unknown route', () => {
    expect(wrapper?.get('[data-test="not-found"]').text()).toBe('Page not found')
  })

  it('navigates from the not-found view to home through the router link', async () => {
    await wrapper?.get('a[href="/"]').trigger('click')
    await flushPromises()

    expect(wrapper?.get('[data-test="app-title"]').text()).toBe('QingFa.Shop')
    expect(wrapper?.get('[data-test="health-chip"]').text()).toContain('healthy')
  })
})
