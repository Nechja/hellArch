<script setup lang="ts">
import { computed } from 'vue'

const props = defineProps<{
  avatarUrl?: string | null
  displayName?: string | null
  email?: string | null
  size?: number
}>()

const initials = computed(() => {
  const name = props.displayName || props.email
  if (!name) return '?'
  return name.slice(0, 2).toUpperCase()
})

const avatarSize = computed(() => props.size ?? 32)
</script>

<template>
  <v-avatar :size="avatarSize" class="user-avatar">
    <v-img v-if="avatarUrl" :src="avatarUrl" :alt="displayName || email || 'User'" />
    <span v-else class="text-white font-weight-medium text-caption">{{ initials }}</span>
  </v-avatar>
</template>

<style scoped>
.user-avatar {
  background: linear-gradient(135deg, #ff4d8d 0%, #ff6b9d 100%);
  border: 2px solid rgba(255, 255, 255, 0.1);
}

.user-avatar .v-img {
  border-radius: inherit;
}
</style>
