<script setup lang="ts">
import type { UserProfile } from '@hellarch/types'

defineProps<{
  user: UserProfile | null
}>()

const architectureItems = [
  { title: 'Vue 3', subtitle: 'App & Login', color: 'primary' },
  { title: 'Blazor', subtitle: 'Admin Panel', color: 'secondary' },
  { title: 'Go', subtitle: 'Auth Service', color: 'accent' },
  { title: 'F#', subtitle: 'User Service', color: 'warning' },
]
</script>

<template>
  <div>
    <!-- Welcome Section -->
    <v-card class="mb-8" color="surface-variant">
      <v-card-text class="pa-8">
        <h1 class="text-h3 font-weight-bold text-primary mb-2">
          Welcome{{ user?.display_name ? `, ${user.display_name}` : '' }}
        </h1>
        <p class="text-body-1 text-medium-emphasis">
          This is your HellArch dashboard. A reusable microservices template for building modern web applications.
        </p>
      </v-card-text>
    </v-card>

    <!-- Quick Actions -->
    <section class="mb-8">
      <h2 class="text-h6 font-weight-bold mb-4 text-uppercase text-medium-emphasis">
        Quick Actions
      </h2>
      <v-row>
        <v-col cols="12" md="4">
          <v-card :to="'/settings'" hover class="h-100">
            <v-card-text class="d-flex align-start ga-4 pa-4">
              <v-avatar color="primary" size="48">
                <v-icon>mdi-cog</v-icon>
              </v-avatar>
              <div>
                <h3 class="text-subtitle-1 font-weight-bold mb-1">Settings</h3>
                <p class="text-caption text-medium-emphasis mb-0">Manage your account preferences</p>
              </div>
            </v-card-text>
          </v-card>
        </v-col>

        <v-col cols="12" md="4">
          <v-card href="/admin" hover class="h-100">
            <v-card-text class="d-flex align-start ga-4 pa-4">
              <v-avatar color="secondary" size="48">
                <v-icon>mdi-shield-crown</v-icon>
              </v-avatar>
              <div>
                <h3 class="text-subtitle-1 font-weight-bold mb-1">Admin Panel</h3>
                <p class="text-caption text-medium-emphasis mb-0">Access administrative tools</p>
              </div>
            </v-card-text>
          </v-card>
        </v-col>

        <v-col cols="12" md="4">
          <v-card disabled class="h-100">
            <v-card-text class="d-flex align-start ga-4 pa-4">
              <v-avatar color="grey" size="48">
                <v-icon>mdi-plus</v-icon>
              </v-avatar>
              <div>
                <h3 class="text-subtitle-1 font-weight-bold text-disabled mb-1">Your Feature</h3>
                <p class="text-caption text-disabled mb-0">Add your own modules here</p>
              </div>
            </v-card-text>
          </v-card>
        </v-col>
      </v-row>
    </section>

    <!-- Architecture Info -->
    <section>
      <h2 class="text-h6 font-weight-bold mb-4 text-uppercase text-medium-emphasis">
        Architecture
      </h2>
      <v-row>
        <v-col
          v-for="item in architectureItems"
          :key="item.title"
          cols="6"
          md="3"
        >
          <v-card hover class="text-center pa-4">
            <p class="text-h6 font-weight-bold" :class="`text-${item.color}`">
              {{ item.title }}
            </p>
            <p class="text-caption text-medium-emphasis mb-0">
              {{ item.subtitle }}
            </p>
          </v-card>
        </v-col>
      </v-row>
    </section>
  </div>
</template>
