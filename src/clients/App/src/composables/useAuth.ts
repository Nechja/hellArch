import { ref, readonly } from 'vue'

interface AuthState {
  userId: string | null
  linkedProviders: string[]
  isAuthenticated: boolean
  isLoading: boolean
}

const state = ref<AuthState>({
  userId: null,
  linkedProviders: [],
  isAuthenticated: false,
  isLoading: true,
})

let checkPromise: Promise<boolean> | null = null

async function checkAuth(): Promise<boolean> {
  if (checkPromise) return checkPromise

  checkPromise = (async () => {
    state.value.isLoading = true
    try {
      const response = await fetch('/api/auth/me', {
        credentials: 'include',
      })

      if (response.ok) {
        const data = await response.json()
        state.value.userId = data.user_id
        state.value.linkedProviders = data.linked_providers || []
        state.value.isAuthenticated = true
        return true
      }

      state.value.userId = null
      state.value.linkedProviders = []
      state.value.isAuthenticated = false
      return false
    } catch {
      state.value.userId = null
      state.value.linkedProviders = []
      state.value.isAuthenticated = false
      return false
    } finally {
      state.value.isLoading = false
      checkPromise = null
    }
  })()

  return checkPromise
}

function logout() {
  state.value.userId = null
  state.value.isAuthenticated = false
  window.location.href = '/api/auth/logout'
}

export function useAuth() {
  return {
    state: readonly(state),
    checkAuth,
    logout,
  }
}
