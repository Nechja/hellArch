<script setup lang="ts">
import { computed, ref } from 'vue'
import type { UserProfile } from '@hellarch/types'

const props = defineProps<{
  user: UserProfile | null
}>()

const mobileMenuOpen = ref(false)

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
        <RouterLink to="/" class="text-xl font-bold text-gradient hover-glow">
          HellArch
        </RouterLink>

        <!-- Desktop Navigation -->
        <div class="hidden md:flex items-center gap-8">
          <RouterLink
            to="/"
            class="topnav-link group"
          >
            Dashboard
            <span class="absolute -bottom-1 left-0 w-0 h-0.5 bg-neon-pink group-hover:w-full transition-all duration-normal" />
          </RouterLink>
          <RouterLink
            to="/settings"
            class="topnav-link group"
          >
            Settings
            <span class="absolute -bottom-1 left-0 w-0 h-0.5 bg-neon-pink group-hover:w-full transition-all duration-normal" />
          </RouterLink>

          <!-- User Menu -->
          <div v-if="user" class="flex items-center gap-3 ml-2">
            <img
              v-if="user.avatar_url"
              :src="user.avatar_url"
              :alt="user.display_name || user.email"
              class="w-8 h-8 rounded-full border-2 border-neon-pink/30 hover:border-neon-pink transition-colors"
            />
            <div v-else class="w-8 h-8 rounded-full bg-neon-pink flex-center text-sm font-bold text-bg-primary hover:shadow-glow-pink transition-shadow">
              {{ initials }}
            </div>
            <a
              href="/api/auth/logout"
              class="text-sm text-text-muted hover:text-neon-coral transition-colors"
            >
              Logout
            </a>
          </div>
          <a v-else href="/login" class="btn-neon text-sm">
            Sign in
          </a>
        </div>

        <!-- Mobile Hamburger -->
        <button
          class="hamburger md:hidden"
          @click="mobileMenuOpen = !mobileMenuOpen"
          aria-label="Toggle menu"
        >
          <span class="hamburger-line" :class="{ 'rotate-45 translate-y-2': mobileMenuOpen }" />
          <span class="hamburger-line" :class="{ 'opacity-0': mobileMenuOpen }" />
          <span class="hamburger-line" :class="{ '-rotate-45 -translate-y-2': mobileMenuOpen }" />
        </button>
      </div>

      <!-- Mobile Menu -->
      <div
        v-show="mobileMenuOpen"
        class="md:hidden py-4 border-t border-border"
      >
        <div class="flex flex-col gap-4">
          <RouterLink
            to="/"
            class="text-text-secondary hover:text-neon-pink transition-colors py-2"
            @click="mobileMenuOpen = false"
          >
            Dashboard
          </RouterLink>
          <RouterLink
            to="/settings"
            class="text-text-secondary hover:text-neon-pink transition-colors py-2"
            @click="mobileMenuOpen = false"
          >
            Settings
          </RouterLink>

          <div class="border-t border-border pt-4 mt-2">
            <div v-if="user" class="flex items-center gap-3">
              <img
                v-if="user.avatar_url"
                :src="user.avatar_url"
                :alt="user.display_name || user.email"
                class="w-8 h-8 rounded-full border border-neon-pink/30"
              />
              <div v-else class="w-8 h-8 rounded-full bg-neon-pink flex-center text-sm font-bold text-bg-primary">
                {{ initials }}
              </div>
              <span class="text-text-secondary text-sm">{{ user.display_name || user.email }}</span>
              <a
                href="/api/auth/logout"
                class="ml-auto text-sm text-text-muted hover:text-neon-coral transition-colors"
              >
                Logout
              </a>
            </div>
            <a v-else href="/login" class="btn-neon text-sm w-full text-center">
              Sign in
            </a>
          </div>
        </div>
      </div>
    </div>
  </nav>
</template>
