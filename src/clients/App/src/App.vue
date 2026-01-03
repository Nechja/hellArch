<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import NavBar from './components/NavBar.vue'
import type { UserProfile } from '@hellarch/types'

const route = useRoute()
const user = ref<UserProfile | null>(null)
const loading = ref(true)

const hideNav = computed(() => route.meta.hideNav === true)
const isPublicRoute = computed(() => route.meta.public === true)

onMounted(async () => {
  if (isPublicRoute.value) {
    loading.value = false
    return
  }

  try {
    const res = await fetch('/api/user/me')
    if (res.ok) {
      user.value = await res.json()
    }
  } catch {
    // Not authenticated or error
  } finally {
    loading.value = false
  }
})
</script>

<template>
  <v-app>
    <NavBar v-if="!hideNav" :user="user" />
    <v-main>
      <v-container v-if="!hideNav" class="py-8">
        <v-progress-circular
          v-if="loading"
          indeterminate
          color="primary"
          size="64"
          class="d-flex mx-auto mt-16"
        />
        <RouterView v-else :user="user" />
      </v-container>
      <RouterView v-else :user="user" />
    </v-main>
  </v-app>
</template>
