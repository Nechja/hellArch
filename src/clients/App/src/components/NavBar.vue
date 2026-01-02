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

    </div>
  </nav>

  <!-- Mobile Menu Overlay -->
  <Teleport to="body">
    <div v-if="mobileMenuOpen" class="mobile-nav-overlay md:hidden" @click="mobileMenuOpen = false" />
    <aside
      class="mobile-nav-panel md:hidden"
      :class="mobileMenuOpen ? 'translate-x-0' : '-translate-x-full'"
    >
      <div class="p-4 border-b border-border mb-4 flex-between">
        <div>
          <h2 class="text-xl font-bold text-gradient">HellArch</h2>
          <span class="text-xs text-text-muted uppercase tracking-wider">Menu</span>
        </div>
        <button
          class="text-text-secondary hover:text-neon-pink p-2 transition-colors"
          @click="mobileMenuOpen = false"
          aria-label="Close menu"
        >
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
          </svg>
        </button>
      </div>

      <nav class="flex flex-col gap-1 px-2">
        <RouterLink
          to="/"
          class="nav-link"
          active-class="nav-link-active"
          @click="mobileMenuOpen = false"
        >
          <span class="text-neon-pink">&#9632;</span>
          <span>Dashboard</span>
        </RouterLink>
        <RouterLink
          to="/settings"
          class="nav-link"
          active-class="nav-link-active"
          @click="mobileMenuOpen = false"
        >
          <span class="text-neon-rose">&#9679;</span>
          <span>Settings</span>
        </RouterLink>

        <div class="h-px bg-border my-3" />

        <div v-if="user" class="px-3 py-2">
          <div class="flex items-center gap-3 mb-3">
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
          </div>
          <a
            href="/api/auth/logout"
            class="nav-link text-neon-coral hover:bg-neon-coral/10"
          >
            <span>&#10140;</span>
            <span>Logout</span>
          </a>
        </div>
        <a v-else href="/login" class="btn-neon text-sm mx-2">
          Sign in
        </a>
      </nav>
    </aside>
  </Teleport>
</template>
