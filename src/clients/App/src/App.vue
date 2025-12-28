<script setup lang="ts">
import { ref, onMounted } from 'vue'
import NavBar from './components/NavBar.vue'
import type { UserProfile } from '@hellarch/types'

const user = ref<UserProfile | null>(null)
const loading = ref(true)

onMounted(async () => {
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
  <div class="min-h-screen bg-bg-primary">
    <NavBar :user="user" />
    <main class="max-w-5xl mx-auto px-4 py-8">
      <div v-if="loading" class="flex-center min-h-[50vh]">
        <div class="text-text-secondary">Loading...</div>
      </div>
      <RouterView v-else :user="user" />
    </main>
  </div>
</template>
