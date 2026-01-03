<script setup lang="ts">
import { computed } from 'vue'

export type AlertVariant = 'info' | 'success' | 'warning' | 'danger'

interface Props {
  variant?: AlertVariant
  title?: string
  dismissible?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  variant: 'info',
  dismissible: false
})

const emit = defineEmits<{
  dismiss: []
}>()

const alertClass = computed(() => `alert alert-${props.variant}`)

const iconMap: Record<AlertVariant, string> = {
  info: 'ℹ',
  success: '✓',
  warning: '⚠',
  danger: '✕'
}
</script>

<template>
  <div :class="alertClass" role="alert">
    <span class="alert-icon">{{ iconMap[variant] }}</span>
    <div class="flex-1">
      <strong v-if="title" class="block">{{ title }}</strong>
      <slot />
    </div>
    <button
      v-if="dismissible"
      type="button"
      class="btn-icon"
      aria-label="Dismiss"
      @click="emit('dismiss')"
    >
      ✕
    </button>
  </div>
</template>
