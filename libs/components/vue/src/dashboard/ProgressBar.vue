<script setup lang="ts">
import { computed } from 'vue'

export type ProgressVariant = 'default' | 'success' | 'warning' | 'danger'

interface Props {
  value: number
  max?: number
  variant?: ProgressVariant
  showLabel?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  max: 100,
  variant: 'default',
  showLabel: false
})

const percentage = computed(() =>
  Math.min(100, Math.max(0, (props.value / props.max) * 100))
)

const fillClass = computed(() => {
  const variantMap: Record<ProgressVariant, string> = {
    default: 'progress-fill',
    success: 'progress-fill-success',
    warning: 'progress-fill-warning',
    danger: 'progress-fill-danger'
  }
  return variantMap[props.variant]
})
</script>

<template>
  <div class="flex items-center gap-2">
    <div class="progress-bar" role="progressbar" :aria-valuenow="value" :aria-valuemax="max">
      <div :class="fillClass" :style="{ width: `${percentage}%` }" />
    </div>
    <span v-if="showLabel" class="text-xs text-muted">{{ Math.round(percentage) }}%</span>
  </div>
</template>
