<script setup lang="ts">
import { computed } from 'vue'

export type BadgeVariant = 'default' | 'success' | 'warning' | 'danger' | 'info'

interface Props {
  variant?: BadgeVariant
  dot?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  variant: 'default',
  dot: false
})

const badgeClass = computed(() => {
  const variantMap: Record<BadgeVariant, string> = {
    default: 'badge',
    success: 'badge-success',
    warning: 'badge-warning',
    danger: 'badge-danger',
    info: 'badge-info'
  }

  const classes = [variantMap[props.variant]]
  if (props.dot) classes.push('badge-dot')
  return classes.join(' ')
})
</script>

<template>
  <span :class="badgeClass">
    <span v-if="dot" class="w-1.5 h-1.5 rounded-full bg-current" />
    <slot />
  </span>
</template>
