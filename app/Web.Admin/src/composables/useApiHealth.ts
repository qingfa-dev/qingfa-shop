import { computed, onMounted, ref } from 'vue'
import { fetchApiHealth, type HealthState } from '../api/health'

const labels: Record<HealthState, string> = {
  checking: 'checking',
  up: 'healthy',
  down: 'unreachable',
}

export function useApiHealth() {
  const state = ref<HealthState>('checking')
  const label = computed(() => labels[state.value])

  onMounted(async () => {
    try {
      state.value = (await fetchApiHealth()) ? 'up' : 'down'
    } catch {
      state.value = 'down'
    }
  })

  return { label }
}
