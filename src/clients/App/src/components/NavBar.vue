<script setup lang="ts">
import { computed } from 'vue'
import type { UserProfile } from '@hellarch/types'

const props = defineProps<{
  user: UserProfile | null
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
        <RouterLink to="/" class="text-xl font-bold text-gradient hover:shadow-glow-cyan transition-all">
          HellArch
        </RouterLink>

        <!-- Navigation -->
        <div class="flex items-center gap-6">
          <RouterLink
            to="/"
            class="text-text-secondary hover:text-neon-cyan transition-colors relative group"
          >
            Dashboard
            <span class="absolute -bottom-1 left-0 w-0 h-0.5 bg-neon-cyan group-hover:w-full transition-all duration-normal" />
          </RouterLink>
          <RouterLink
            to="/settings"
            class="text-text-secondary hover:text-neon-cyan transition-colors relative group"
          >
            Settings
            <span class="absolute -bottom-1 left-0 w-0 h-0.5 bg-neon-cyan group-hover:w-full transition-all duration-normal" />
          </RouterLink>

          <!-- User Menu -->
          <div v-if="user" class="flex items-center gap-3">
            <img
              v-if="user.avatar_url"
              :src="user.avatar_url"
              :alt="user.display_name || user.email"
              class="w-8 h-8 rounded-full border border-neon-cyan/30"
            />
            <div v-else class="w-8 h-8 rounded-full bg-neon-cyan flex-center text-sm font-bold text-bg-primary">
              {{ initials }}
            </div>
            <a
              href="/api/auth/logout"
              class="text-sm text-text-muted hover:text-neon-red transition-colors"
            >
              Logout
            </a>
          </div>
          <a v-else href="/login" class="btn-neon text-sm">
            Sign in
          </a>
        </div>
      </div>
    </div>
  </nav>
</template>
