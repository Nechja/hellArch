<script setup lang="ts">
import { computed } from 'vue'

interface User {
  id: string
  email: string
  display_name: string
  avatar_url: string
}

const props = defineProps<{
  user: User | null
}>()

const initials = computed(() => {
  const name = props.user?.display_name || props.user?.email
  if (!name) return '?'
  return name.slice(0, 2).toUpperCase()
})
</script>

<template>
  <nav class="bg-bg-secondary border-b border-border">
    <div class="max-w-5xl mx-auto px-4">
      <div class="flex-between h-16">
        <!-- Logo -->
        <RouterLink to="/" class="text-xl font-bold text-text-primary hover:text-accent transition-colors">
          HellArch
        </RouterLink>

        <!-- Navigation -->
        <div class="flex items-center gap-6">
          <RouterLink to="/" class="text-text-secondary hover:text-text-primary transition-colors">
            Dashboard
          </RouterLink>
          <RouterLink to="/settings" class="text-text-secondary hover:text-text-primary transition-colors">
            Settings
          </RouterLink>

          <!-- User Menu -->
          <div v-if="user" class="flex items-center gap-3">
            <img
              v-if="user.avatar_url"
              :src="user.avatar_url"
              :alt="user.display_name || user.email"
              class="w-8 h-8 rounded-full"
            />
            <div v-else class="w-8 h-8 rounded-full bg-accent flex-center text-sm font-medium text-white">
              {{ initials }}
            </div>
            <a
              href="/api/auth/logout"
              class="text-sm text-text-muted hover:text-text-secondary transition-colors"
            >
              Logout
            </a>
          </div>
          <a v-else href="/login" class="btn-primary text-sm">
            Sign in
          </a>
        </div>
      </div>
    </div>
  </nav>
</template>
