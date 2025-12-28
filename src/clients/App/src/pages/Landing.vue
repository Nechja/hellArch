<script setup lang="ts">
interface User {
  id: string
  email: string
  username: string
}

defineProps<{
  user: User | null
}>()
</script>

<template>
  <div class="space-y-8">
    <!-- Welcome Section -->
    <section class="card">
      <h1 class="text-heading mb-2">
        Welcome{{ user ? `, ${user.username}` : '' }}
      </h1>
      <p class="text-text-secondary">
        This is your HellArch dashboard. A reusable microservices template for building modern web applications.
      </p>
    </section>

    <!-- Quick Actions -->
    <section>
      <h2 class="text-subheading mb-4">Quick Actions</h2>
      <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
        <RouterLink to="/settings" class="card hover:border-accent transition-colors group">
          <div class="flex items-start gap-4">
            <div class="w-10 h-10 rounded-lg bg-accent/10 flex-center text-accent">
              <div class="i-carbon-settings text-xl" />
            </div>
            <div>
              <h3 class="font-semibold text-text-primary group-hover:text-accent transition-colors">
                Settings
              </h3>
              <p class="text-caption">Manage your account preferences</p>
            </div>
          </div>
        </RouterLink>

        <a href="/admin" class="card hover:border-accent transition-colors group">
          <div class="flex items-start gap-4">
            <div class="w-10 h-10 rounded-lg bg-accent/10 flex-center text-accent">
              <div class="i-carbon-dashboard text-xl" />
            </div>
            <div>
              <h3 class="font-semibold text-text-primary group-hover:text-accent transition-colors">
                Admin Panel
              </h3>
              <p class="text-caption">Access administrative tools</p>
            </div>
          </div>
        </a>

        <div class="card opacity-60">
          <div class="flex items-start gap-4">
            <div class="w-10 h-10 rounded-lg bg-bg-elevated flex-center text-text-muted">
              <div class="i-carbon-add text-xl" />
            </div>
            <div>
              <h3 class="font-semibold text-text-muted">
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
      <h2 class="text-subheading mb-4">Architecture</h2>
      <div class="grid grid-cols-2 md:grid-cols-4 gap-4 text-center">
        <div class="p-4 rounded-lg bg-bg-secondary">
          <div class="text-2xl font-bold text-accent">Vue 3</div>
          <div class="text-caption">This app</div>
        </div>
        <div class="p-4 rounded-lg bg-bg-secondary">
          <div class="text-2xl font-bold text-accent">Blazor</div>
          <div class="text-caption">Login & Admin</div>
        </div>
        <div class="p-4 rounded-lg bg-bg-secondary">
          <div class="text-2xl font-bold text-accent">Go</div>
          <div class="text-caption">Auth Service</div>
        </div>
        <div class="p-4 rounded-lg bg-bg-secondary">
          <div class="text-2xl font-bold text-accent">F#</div>
          <div class="text-caption">User Service</div>
        </div>
      </div>
    </section>
  </div>
</template>
