<script setup lang="ts">
import { computed } from 'vue'
import Badge from '../atoms/Badge.vue'

type Provider = 'discord' | 'github' | 'google' | 'apple' | 'microsoft' | 'twitch' | 'hellarch'

const props = defineProps<{
  provider: Provider
  connected?: boolean
  comingSoon?: boolean
}>()

const emit = defineEmits<{
  connect: []
  disconnect: []
}>()

const providerConfig = {
  discord: {
    color: '#5865F2',
    icon: 'mdi-discord',
    label: 'Discord',
  },
  github: {
    color: '#24292e',
    icon: 'mdi-github',
    label: 'GitHub',
  },
  google: {
    color: '#ea4335',
    icon: 'mdi-google',
    label: 'Google',
  },
  apple: {
    color: '#000000',
    icon: 'mdi-apple',
    label: 'Apple',
  },
  microsoft: {
    color: '#00a4ef',
    icon: 'mdi-microsoft',
    label: 'Microsoft',
  },
  twitch: {
    color: '#9146ff',
    icon: 'mdi-twitch',
    label: 'Twitch',
  },
  hellarch: {
    color: '#ff4d8d',
    icon: 'mdi-key',
    label: 'HellArch',
  },
}

const config = computed(() => providerConfig[props.provider])
const isDisabled = computed(() => props.comingSoon || false)
</script>

<template>
  <div class="linked-account d-flex align-center py-4 px-4" :class="{ 'linked-account--disabled': isDisabled }">
    <div
      class="linked-account__icon mr-4"
      :style="{ background: isDisabled ? 'rgba(255,255,255,0.05)' : config.color }"
    >
      <v-icon color="white" size="20">{{ config.icon }}</v-icon>
    </div>
    <div class="flex-grow-1">
      <div class="text-body-1 font-weight-medium" :class="{ 'text-disabled': isDisabled }">
        {{ config.label }}
      </div>
      <div class="text-caption" :class="isDisabled ? 'text-disabled' : 'text-medium-emphasis'">
        {{ comingSoon ? 'Coming soon' : (connected ? 'Connected' : 'Not connected') }}
      </div>
    </div>
    <Badge
      v-if="connected"
      label="Connected"
      variant="success"
      dot
    />
    <Badge
      v-else-if="comingSoon"
      label="Soon"
      variant="default"
      size="sm"
    />
    <v-btn
      v-else
      variant="outlined"
      size="small"
      color="primary"
      @click="emit('connect')"
    >
      Connect
    </v-btn>
  </div>
</template>

<style scoped>
.linked-account__icon {
  width: 40px;
  height: 40px;
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: transform 0.2s ease;
}

.linked-account:not(.linked-account--disabled):hover .linked-account__icon {
  transform: scale(1.05);
}
</style>
