<script setup lang="ts">
import { computed } from 'vue'

type Provider = 'discord' | 'github' | 'google' | 'apple' | 'microsoft' | 'twitch' | 'hellarch'

const props = defineProps<{
  provider: Provider
  disabled?: boolean
  loading?: boolean
  compact?: boolean
}>()

const emit = defineEmits<{
  click: []
}>()

const providerConfig = {
  discord: {
    color: '#5865F2',
    hoverColor: '#4752c4',
    icon: 'mdi-discord',
    label: 'Discord',
  },
  github: {
    color: '#24292e',
    hoverColor: '#1b1f23',
    icon: 'mdi-github',
    label: 'GitHub',
  },
  google: {
    color: '#ea4335',
    hoverColor: '#d33426',
    icon: 'mdi-google',
    label: 'Google',
  },
  apple: {
    color: '#000000',
    hoverColor: '#1a1a1a',
    icon: 'mdi-apple',
    label: 'Apple',
  },
  microsoft: {
    color: '#00a4ef',
    hoverColor: '#0078d4',
    icon: 'mdi-microsoft',
    label: 'Microsoft',
  },
  twitch: {
    color: '#9146ff',
    hoverColor: '#7c3aed',
    icon: 'mdi-twitch',
    label: 'Twitch',
  },
  hellarch: {
    color: '#ff4d8d',
    hoverColor: '#e63b75',
    icon: 'mdi-key',
    label: 'HellArch',
  },
}

const config = computed(() => providerConfig[props.provider])
</script>

<template>
  <v-btn
    :style="{ '--btn-color': config.color, '--btn-hover': config.hoverColor }"
    :class="['oauth-btn', { 'oauth-btn--compact': compact }]"
    :size="compact ? 'x-large' : 'large'"
    :block="!compact"
    :icon="compact"
    :disabled="disabled"
    :loading="loading"
    @click="emit('click')"
  >
    <v-icon :start="!compact" :size="compact ? 24 : 20">{{ config.icon }}</v-icon>
    <span v-if="!compact">{{ config.label }}</span>
  </v-btn>
</template>

<style scoped>
.oauth-btn {
  background: var(--btn-color) !important;
  color: white !important;
  font-weight: 500;
  letter-spacing: 0;
  text-transform: none;
  transition: all 0.2s ease;
}

.oauth-btn:hover {
  background: var(--btn-hover) !important;
  transform: translateY(-1px);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.3);
}

.oauth-btn:active {
  transform: translateY(0);
}

.oauth-btn--compact {
  border-radius: 12px !important;
  width: 52px !important;
  height: 52px !important;
}
</style>
