<script setup lang="ts">
import { computed, ref } from 'vue'
import type { UserProfile } from '@hellarch/types'

const props = defineProps<{
  user: UserProfile | null
}>()

const drawer = ref(false)

const initials = computed(() => {
  const name = props.user?.display_name || props.user?.email
  if (!name) return '?'
  return name.slice(0, 2).toUpperCase()
})

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
      <RouterLink to="/" class="text-decoration-none">
        <span class="text-h6 font-weight-bold text-primary">HellArch</span>
      </RouterLink>
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
          <v-menu offset-y>
            <template #activator="{ props: menuProps }">
              <v-btn v-bind="menuProps" icon variant="text">
                <v-avatar v-if="user.avatar_url" size="32">
                  <v-img :src="user.avatar_url" :alt="user.display_name || user.email" />
                </v-avatar>
                <v-avatar v-else size="32" color="primary">
                  {{ initials }}
                </v-avatar>
              </v-btn>
            </template>
            <v-list density="compact">
              <v-list-item>
                <v-list-item-title class="text-body-2 text-medium-emphasis">
                  {{ user.display_name || user.email }}
                </v-list-item-title>
              </v-list-item>
              <v-divider />
              <v-list-item href="/api/auth/logout">
                <template #prepend>
                  <v-icon>mdi-logout</v-icon>
                </template>
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
      <v-list-item-title class="text-h6 font-weight-bold text-primary">
        HellArch
      </v-list-item-title>
      <v-list-item-subtitle class="text-caption text-uppercase">
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
          <v-avatar v-if="user.avatar_url" size="32" class="mr-3">
            <v-img :src="user.avatar_url" :alt="user.display_name || user.email" />
          </v-avatar>
          <v-avatar v-else size="32" color="primary" class="mr-3">
            {{ initials }}
          </v-avatar>
        </template>
        <v-list-item-title class="text-body-2">
          {{ user.display_name || user.email }}
        </v-list-item-title>
      </v-list-item>
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
