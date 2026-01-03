<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { useRoute } from 'vue-router'
import NavBar from './components/organisms/NavBar.vue'
import PageSkeleton from './components/molecules/PageSkeleton.vue'
import ErrorState from './components/molecules/ErrorState.vue'
import type { UserProfile } from '@hellarch/types'

const route = useRoute()
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
    } else if (res.status === 401) {
      // Not authenticated - redirect will happen via gateway
    } else {
      error.value = 'Failed to load user data'
    }
  } catch {
    error.value = 'Network error. Please check your connection.'
  } finally {
    loading.value = false
  }
}

onMounted(fetchUser)

watch(() => route.path, () => {
  // Reset error state on navigation
  error.value = null
})
</script>

<template>
  <v-app>
    <NavBar v-if="!hideNav && !loading" :user="user" />
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
