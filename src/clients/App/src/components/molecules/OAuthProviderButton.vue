<script setup lang="ts">
import { computed } from 'vue'

type Provider = 'discord' | 'github' | 'google'

const props = defineProps<{
  provider: Provider
  disabled?: boolean
  loading?: boolean
}>()

const emit = defineEmits<{
  click: []
}>()

const providerConfig = {
  discord: {
    color: '#5865F2',
    hoverColor: '#4752c4',
    icon: 'mdi-discord',
    label: 'Continue with Discord',
  },
  github: {
    color: '#24292e',
    hoverColor: '#1b1f23',
    icon: 'mdi-github',
    label: 'Continue with GitHub',
  },
  google: {
    color: '#ea4335',
    hoverColor: '#d33426',
    icon: 'mdi-google',
    label: 'Continue with Google',
  },
}

const config = computed(() => providerConfig[props.provider])
</script>

<template>
  <v-btn
    :style="{ '--btn-color': config.color, '--btn-hover': config.hoverColor }"
    class="oauth-btn"
    size="large"
    block
    :disabled="disabled"
    :loading="loading"
    @click="emit('click')"
  >
    <v-icon start size="20">{{ config.icon }}</v-icon>
    {{ config.label }}
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
</style>
