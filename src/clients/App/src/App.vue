<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, watch } from 'vue'
import { useRoute } from 'vue-router'
import { NavBar, NotificationCenter } from './components/organisms'
import { PageSkeleton, ErrorState } from './components/molecules'
import { useSignalR } from './composables'
import type { UserProfile } from '@hellarch/types'

const route = useRoute()
const { connect, disconnect, isConnected } = useSignalR()

const user = ref<UserProfile | null>(null)
const loading = ref(true)
const error = ref<string | null>(null)

const hideNav = computed(() => route.meta.hideNav === true)
const isPublicRoute = computed(() => route.meta.public === true)

const skeletonVariant = computed(() => {
  if (route.path === '/settings') return 'settings'
  if (route.path === '/') return 'dashboard'
  return 'default'
})

async function fetchUser() {
  if (isPublicRoute.value) {
    loading.value = false
    return
  }

  loading.value = true
  error.value = null

  try {
    const res = await fetch('/api/user/me')
    if (res.ok) {
      user.value = await res.json()
      // Connect to SignalR once authenticated
      if (!isConnected()) {
        connect()
      }
    } else if (res.status === 401) {
      // Not authenticated - redirect will happen via gateway
    } else if (res.status === 404) {
      // Profile not yet created - may happen if event processing is delayed
      // Retry after a short delay
      setTimeout(fetchUser, 2000)
    } else {
      error.value = 'Failed to load user data'
    }
  } catch {
    error.value = 'Network error. Please check your connection.'
  } finally {
    loading.value = false
  }
}

// Listen for user-updated events from SignalR
function handleUserUpdated() {
  fetchUser()
}

onMounted(() => {
  fetchUser()
  window.addEventListener('user-updated', handleUserUpdated)
})

onUnmounted(() => {
  window.removeEventListener('user-updated', handleUserUpdated)
  disconnect()
})

watch(() => route.path, () => {
  // Reset error state on navigation
  error.value = null
})
</script>

<template>
  <v-app>
    <NavBar v-if="!hideNav && !loading" :user="user" />
    <NotificationCenter />
    <v-main>
      <!-- Public routes (login) - no container -->
      <template v-if="hideNav">
        <RouterView :user="user" />
      </template>

      <!-- Protected routes with container -->
      <v-container v-else class="py-8">
        <!-- Loading state -->
        <PageSkeleton v-if="loading" :variant="skeletonVariant" />

        <!-- Error state -->
        <ErrorState
          v-else-if="error"
          :message="error"
          retry-label="Try again"
          full-height
          @retry="fetchUser"
        />

        <!-- Content -->
        <Transition v-else name="fade" mode="out-in">
          <RouterView :user="user" />
        </Transition>
      </v-container>
    </v-main>
  </v-app>
</template>

<style>
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.2s ease;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}
</style>
