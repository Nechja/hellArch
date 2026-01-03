<script setup lang="ts">
import { computed } from 'vue'

export type PillVariant = 'success' | 'warning' | 'danger' | 'info' | 'neutral' | 'online' | 'offline' | 'pending'

interface Props {
  variant?: PillVariant
  pulse?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  variant: 'neutral',
  pulse: false
})

const pillClass = computed(() => {
  const classes = ['pill', `pill-${props.variant}`]
  if (props.pulse) classes.push('animate-pulse')
  return classes.join(' ')
})
</script>

<template>
  <span :class="pillClass">
    <slot />
  </span>
</template>
