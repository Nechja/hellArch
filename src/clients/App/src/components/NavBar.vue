<script setup lang="ts">
import { ref } from 'vue'
import AppLogo from './atoms/AppLogo.vue'
import UserAvatar from './atoms/UserAvatar.vue'
import type { UserProfile } from '@hellarch/types'

defineProps<{
  user: UserProfile | null
}>()

const drawer = ref(false)

const navItems = [
  { title: 'Dashboard', to: '/', icon: 'mdi-view-dashboard' },
  { title: 'Settings', to: '/settings', icon: 'mdi-cog' },
]
</script>

<template>
  <v-app-bar color="surface-variant" elevation="1">
    <v-app-bar-nav-icon
      class="d-md-none"
      @click="drawer = !drawer"
    />

    <v-app-bar-title>
      <AppLogo link-to="/" />
    </v-app-bar-title>

    <template #append>
      <!-- Desktop Navigation -->
      <div class="d-none d-md-flex align-center ga-2">
        <v-btn
          v-for="item in navItems"
          :key="item.to"
          :to="item.to"
          variant="text"
          class="text-none"
        >
          {{ item.title }}
        </v-btn>

        <template v-if="user">
          <v-menu>
            <template #activator="{ props: menuProps }">
              <v-btn v-bind="menuProps" icon variant="text">
                <UserAvatar
                  :avatar-url="user.avatar_url"
                  :display-name="user.display_name"
                  :email="user.email"
                  :size="32"
                />
              </v-btn>
            </template>
            <v-list density="compact" min-width="200">
              <v-list-item>
                <v-list-item-title class="text-body-2 font-weight-medium">
                  {{ user.display_name || user.email }}
                </v-list-item-title>
                <v-list-item-subtitle class="text-caption">
                  {{ user.email }}
                </v-list-item-subtitle>
              </v-list-item>
              <v-divider />
              <v-list-item href="/admin" prepend-icon="mdi-shield-crown">
                <v-list-item-title>Admin Panel</v-list-item-title>
              </v-list-item>
              <v-divider />
              <v-list-item href="/api/auth/logout" prepend-icon="mdi-logout" base-color="error">
                <v-list-item-title>Logout</v-list-item-title>
              </v-list-item>
            </v-list>
          </v-menu>
        </template>
        <v-btn v-else to="/login" color="primary" variant="outlined" class="text-none">
          Sign in
        </v-btn>
      </div>
    </template>
  </v-app-bar>

  <!-- Mobile Navigation Drawer -->
  <v-navigation-drawer v-model="drawer" temporary>
    <v-list-item class="pa-4">
      <AppLogo size="lg" />
      <v-list-item-subtitle class="text-caption text-uppercase mt-1">
        Menu
      </v-list-item-subtitle>
    </v-list-item>

    <v-divider />

    <v-list nav>
      <v-list-item
        v-for="item in navItems"
        :key="item.to"
        :to="item.to"
        :prepend-icon="item.icon"
        :title="item.title"
        @click="drawer = false"
      />
    </v-list>

    <v-divider />

    <v-list v-if="user" nav>
      <v-list-item class="px-4 py-2">
        <template #prepend>
          <UserAvatar
            :avatar-url="user.avatar_url"
            :display-name="user.display_name"
            :email="user.email"
            :size="32"
            class="mr-3"
          />
        </template>
        <v-list-item-title class="text-body-2">
          {{ user.display_name || user.email }}
        </v-list-item-title>
      </v-list-item>
      <v-list-item
        href="/admin"
        prepend-icon="mdi-shield-crown"
        title="Admin Panel"
        @click="drawer = false"
      />
      <v-list-item
        href="/api/auth/logout"
        prepend-icon="mdi-logout"
        title="Logout"
        base-color="error"
      />
    </v-list>
    <v-list v-else nav>
      <v-list-item
        to="/login"
        prepend-icon="mdi-login"
        title="Sign in"
        base-color="primary"
        @click="drawer = false"
      />
    </v-list>
  </v-navigation-drawer>
</template>
