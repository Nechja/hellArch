<script setup lang="ts">
import type { UserProfile } from '@hellarch/types'

defineProps<{
  user: UserProfile | null
}>()
</script>

<template>
  <div class="space-y-8">
    <!-- Welcome Section -->
    <section class="card-featured">
      <h1 class="text-display text-gradient mb-2">
        Welcome{{ user?.display_name ? `, ${user.display_name}` : '' }}
      </h1>
      <p class="text-text-secondary text-lg">
        This is your HellArch dashboard. A reusable microservices template for building modern web applications.
      </p>
    </section>

    <!-- Quick Actions -->
    <section>
      <h2 class="section-header">Quick Actions</h2>
      <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
        <RouterLink to="/settings" class="card-glow group enter-up stagger-1">
          <div class="flex items-start gap-4">
            <div class="icon-box-pink group-hover:shadow-glow-pink group-hover:scale-110 transition-all duration-normal">
              <div class="i-carbon-settings text-2xl" />
            </div>
            <div>
              <h3 class="font-bold text-text-primary group-hover:text-neon-pink transition-colors">
                Settings
              </h3>
              <p class="text-caption">Manage your account preferences</p>
            </div>
          </div>
        </RouterLink>

        <a href="/admin" class="card-glow group enter-up stagger-2">
          <div class="flex items-start gap-4">
            <div class="icon-box-rose group-hover:shadow-glow-rose group-hover:scale-110 transition-all duration-normal">
              <div class="i-carbon-dashboard text-2xl" />
            </div>
            <div>
              <h3 class="font-bold text-text-primary group-hover:text-neon-rose transition-colors">
                Admin Panel
              </h3>
              <p class="text-caption">Access administrative tools</p>
            </div>
          </div>
        </a>

        <div class="card opacity-50 cursor-not-allowed enter-up stagger-3">
          <div class="flex items-start gap-4">
            <div class="w-12 h-12 rounded-sm bg-bg-elevated flex-center text-text-muted">
              <div class="i-carbon-add text-2xl" />
            </div>
            <div>
              <h3 class="font-bold text-text-muted">
                Your Feature
              </h3>
              <p class="text-caption">Add your own modules here</p>
            </div>
          </div>
        </div>
      </div>
    </section>

    <!-- Architecture Info -->
    <section class="card">
      <h2 class="section-header">Architecture</h2>
      <div class="grid grid-cols-2 md:grid-cols-4 gap-4 text-center">
        <div class="p-4 rounded-sm bg-bg-secondary border border-border hover:border-neon-pink/50 card-lift enter-up stagger-1">
          <div class="text-heading text-neon-pink">Vue 3</div>
          <div class="text-caption">This app</div>
        </div>
        <div class="p-4 rounded-sm bg-bg-secondary border border-border hover:border-neon-rose/50 card-lift enter-up stagger-2">
          <div class="text-heading text-neon-rose">Blazor</div>
          <div class="text-caption">Login & Admin</div>
        </div>
        <div class="p-4 rounded-sm bg-bg-secondary border border-border hover:border-neon-peach/50 card-lift enter-up stagger-3">
          <div class="text-heading text-neon-peach">Go</div>
          <div class="text-caption">Auth Service</div>
        </div>
        <div class="p-4 rounded-sm bg-bg-secondary border border-border hover:border-neon-orange/50 card-lift enter-up stagger-4">
          <div class="text-heading text-neon-orange">F#</div>
          <div class="text-caption">User Service</div>
        </div>
      </div>
    </section>
  </div>
</template>
