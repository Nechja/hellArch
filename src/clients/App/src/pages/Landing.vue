<script setup lang="ts">
import { computed } from 'vue'
import SectionHeader from '../components/atoms/SectionHeader.vue'
import ActionCard from '../components/molecules/ActionCard.vue'
import UserAvatar from '../components/atoms/UserAvatar.vue'
import type { UserProfile } from '@hellarch/types'

const props = defineProps<{
  user: UserProfile | null
}>()

const greeting = computed(() => {
  const hour = new Date().getHours()
  if (hour < 12) return 'Good morning'
  if (hour < 18) return 'Good afternoon'
  return 'Good evening'
})

const displayName = computed(() => {
  if (props.user?.display_name) {
    return props.user.display_name.split(' ')[0]
  }
  return null
})
</script>

<template>
  <div class="landing-page">
    <!-- Hero Section -->
    <div class="hero-section mb-10">
      <div class="hero-bg" />
      <div class="hero-content">
        <div class="d-flex align-center ga-4 mb-4">
          <UserAvatar
            v-if="user"
            :avatar-url="user.avatar_url"
            :display-name="user.display_name"
            :email="user.email"
            :size="56"
          />
          <div>
            <h1 class="text-h4 font-weight-bold mb-1">
              {{ greeting }}<template v-if="displayName">, {{ displayName }}</template>
            </h1>
            <p class="text-body-1 text-medium-emphasis">
              Here's what's happening with your account
            </p>
          </div>
        </div>
      </div>
    </div>

    <!-- Quick Actions -->
    <SectionHeader title="Quick Actions" />
    <v-row class="mb-8">
      <v-col cols="12" sm="6" md="4">
        <ActionCard
          title="Account Settings"
          description="Manage your profile and preferences"
          icon="mdi-account-cog"
          to="/settings"
        />
      </v-col>
      <v-col cols="12" sm="6" md="4">
        <ActionCard
          title="Linked Accounts"
          description="Connect your social accounts"
          icon="mdi-link-variant"
          to="/settings"
        />
      </v-col>
      <v-col cols="12" sm="6" md="4">
        <ActionCard
          title="Add Module"
          description="Extend your dashboard"
          icon="mdi-plus-circle-outline"
          disabled
        />
      </v-col>
    </v-row>

    <!-- Stats Preview -->
    <SectionHeader title="Overview" />
    <v-row>
      <v-col v-for="stat in [
        { label: 'Connected Services', value: '1', icon: 'mdi-link' },
        { label: 'Active Sessions', value: '1', icon: 'mdi-devices' },
        { label: 'Account Age', value: 'New', icon: 'mdi-calendar' },
      ]" :key="stat.label" cols="12" sm="4">
        <v-card class="stat-card">
          <v-card-text class="d-flex align-center ga-4 pa-5">
            <div class="stat-icon">
              <v-icon size="24">{{ stat.icon }}</v-icon>
            </div>
            <div>
              <div class="text-h5 font-weight-bold">{{ stat.value }}</div>
              <div class="text-caption text-medium-emphasis">{{ stat.label }}</div>
            </div>
          </v-card-text>
        </v-card>
      </v-col>
    </v-row>
  </div>
</template>

<style scoped>
.landing-page {
  max-width: 1200px;
  margin: 0 auto;
}

.hero-section {
  position: relative;
  padding: 32px;
  border-radius: 16px;
  overflow: hidden;
}

.hero-bg {
  position: absolute;
  inset: 0;
  background: linear-gradient(135deg, rgba(255, 77, 141, 0.1) 0%, rgba(255, 138, 101, 0.05) 100%);
  border: 1px solid rgba(255, 77, 141, 0.15);
  border-radius: 16px;
}

.hero-content {
  position: relative;
  z-index: 1;
}

.stat-card {
  background: rgba(22, 22, 30, 0.6) !important;
  border: 1px solid rgba(255, 255, 255, 0.06);
  transition: all 0.2s ease;
}

.stat-card:hover {
  border-color: rgba(255, 77, 141, 0.2);
  transform: translateY(-2px);
}

.stat-icon {
  width: 48px;
  height: 48px;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: rgba(255, 77, 141, 0.1);
  color: #ff4d8d;
}
</style>
