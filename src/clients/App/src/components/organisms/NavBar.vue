<script setup lang="ts">
import { ref } from 'vue'
import { useRoute } from 'vue-router'
import AppLogo from '../atoms/AppLogo.vue'
import UserAvatar from '../atoms/UserAvatar.vue'
import type { UserProfile } from '@hellarch/types'

defineProps<{
  user: UserProfile | null
}>()

const route = useRoute()
const drawer = ref(false)

const navItems = [
  { title: 'Dashboard', to: '/', icon: 'mdi-view-dashboard' },
  { title: 'Settings', to: '/settings', icon: 'mdi-cog' },
]

function isActive(to: string) {
  return route.path === to
}
</script>

<template>
  <v-app-bar class="navbar" :elevation="0">
    <v-app-bar-nav-icon
      class="d-md-none"
      @click="drawer = !drawer"
    />

    <v-app-bar-title>
      <AppLogo link-to="/" />
    </v-app-bar-title>

    <template #append>
      <!-- Desktop Navigation -->
      <div class="d-none d-md-flex align-center ga-1">
        <v-btn
          v-for="item in navItems"
          :key="item.to"
          :to="item.to"
          variant="text"
          class="nav-link text-none"
          :class="{ 'nav-link--active': isActive(item.to) }"
        >
          {{ item.title }}
        </v-btn>

        <div class="nav-divider mx-2" />

        <template v-if="user">
          <v-menu offset="8" :close-on-content-click="true">
            <template #activator="{ props: menuProps }">
              <v-btn v-bind="menuProps" icon variant="text" class="user-menu-btn">
                <UserAvatar
                  :avatar-url="user.avatar_url"
                  :display-name="user.display_name"
                  :email="user.email"
                  :size="34"
                />
              </v-btn>
            </template>
            <v-card class="user-menu" min-width="220">
              <div class="user-menu__header pa-4">
                <div class="text-body-2 font-weight-medium">
                  {{ user.display_name || user.email }}
                </div>
                <div class="text-caption text-medium-emphasis">
                  {{ user.email }}
                </div>
              </div>
              <v-divider />
              <v-list density="compact" class="py-2">
                <v-list-item href="/admin" prepend-icon="mdi-shield-crown" class="menu-item">
                  <v-list-item-title class="text-body-2">Admin Panel</v-list-item-title>
                </v-list-item>
              </v-list>
              <v-divider />
              <v-list density="compact" class="py-2">
                <v-list-item href="/api/auth/logout" prepend-icon="mdi-logout" class="menu-item menu-item--danger">
                  <v-list-item-title class="text-body-2">Sign out</v-list-item-title>
                </v-list-item>
              </v-list>
            </v-card>
          </v-menu>
        </template>
        <v-btn v-else to="/login" color="primary" variant="flat" class="text-none sign-in-btn">
          Sign in
        </v-btn>
      </div>
    </template>
  </v-app-bar>

  <!-- Mobile Navigation Drawer -->
  <v-navigation-drawer v-model="drawer" temporary class="mobile-drawer">
    <div class="mobile-drawer__header pa-5">
      <AppLogo size="lg" />
    </div>

    <v-divider />

    <v-list nav class="px-2 py-3">
      <v-list-item
        v-for="item in navItems"
        :key="item.to"
        :to="item.to"
        :prepend-icon="item.icon"
        :title="item.title"
        rounded="lg"
        class="mb-1"
        :class="{ 'mobile-nav-active': isActive(item.to) }"
        @click="drawer = false"
      />
    </v-list>

    <v-divider />

    <v-list v-if="user" nav class="px-2 py-3">
      <v-list-item class="px-3 py-3 mb-2" rounded="lg">
        <template #prepend>
          <UserAvatar
            :avatar-url="user.avatar_url"
            :display-name="user.display_name"
            :email="user.email"
            :size="40"
            class="mr-3"
          />
        </template>
        <v-list-item-title class="text-body-2 font-weight-medium">
          {{ user.display_name || user.email }}
        </v-list-item-title>
        <v-list-item-subtitle class="text-caption">
          {{ user.email }}
        </v-list-item-subtitle>
      </v-list-item>
      <v-list-item
        href="/admin"
        prepend-icon="mdi-shield-crown"
        title="Admin Panel"
        rounded="lg"
        class="mb-1"
        @click="drawer = false"
      />
      <v-list-item
        href="/api/auth/logout"
        prepend-icon="mdi-logout"
        title="Sign out"
        rounded="lg"
        class="mobile-nav-danger"
      />
    </v-list>
    <v-list v-else nav class="px-2 py-3">
      <v-list-item
        to="/login"
        prepend-icon="mdi-login"
        title="Sign in"
        rounded="lg"
        class="mobile-nav-primary"
        @click="drawer = false"
      />
    </v-list>
  </v-navigation-drawer>
</template>

<style scoped>
.navbar {
  background: rgba(15, 15, 20, 0.8) !important;
  backdrop-filter: blur(12px);
  border-bottom: 1px solid rgba(255, 255, 255, 0.06);
}

.nav-link {
  font-weight: 500;
  color: rgba(255, 255, 255, 0.7) !important;
  transition: all 0.2s ease;
}

.nav-link:hover {
  color: rgba(255, 255, 255, 0.95) !important;
  background: rgba(255, 255, 255, 0.05) !important;
}

.nav-link--active {
  color: #ff4d8d !important;
  background: rgba(255, 77, 141, 0.1) !important;
}

.nav-divider {
  width: 1px;
  height: 24px;
  background: rgba(255, 255, 255, 0.1);
}

.user-menu-btn {
  transition: transform 0.2s ease;
}

.user-menu-btn:hover {
  transform: scale(1.05);
}

.user-menu {
  background: #16161e !important;
  border: 1px solid rgba(255, 255, 255, 0.08);
}

.user-menu__header {
  background: rgba(255, 77, 141, 0.05);
}

.menu-item {
  margin: 0 8px;
  border-radius: 8px;
}

.menu-item:hover {
  background: rgba(255, 255, 255, 0.05) !important;
}

.menu-item--danger {
  color: #ef5350 !important;
}

.menu-item--danger:hover {
  background: rgba(239, 83, 80, 0.1) !important;
}

.sign-in-btn {
  font-weight: 500;
}

/* Mobile Drawer Styles */
.mobile-drawer {
  background: #0f0f14 !important;
}

.mobile-drawer__header {
  background: linear-gradient(135deg, rgba(255, 77, 141, 0.08) 0%, rgba(255, 138, 101, 0.04) 100%);
}

.mobile-nav-active {
  background: rgba(255, 77, 141, 0.1) !important;
  color: #ff4d8d !important;
}

.mobile-nav-primary {
  background: rgba(255, 77, 141, 0.1) !important;
  color: #ff4d8d !important;
}

.mobile-nav-danger {
  color: #ef5350 !important;
}

.mobile-nav-danger:hover {
  background: rgba(239, 83, 80, 0.1) !important;
}
</style>
