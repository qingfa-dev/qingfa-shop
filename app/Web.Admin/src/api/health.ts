export type HealthState = 'checking' | 'up' | 'down'

export async function fetchApiHealth(): Promise<boolean> {
  const response = await fetch('/health')
  return response.ok
}
