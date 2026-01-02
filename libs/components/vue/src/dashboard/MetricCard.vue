<script setup lang="ts">
import { computed } from 'vue'

export type TrendDirection = 'up' | 'down' | 'neutral'

interface Props {
  label: string
  value: string | number
  trend?: number
  trendDirection?: TrendDirection
  icon?: string
}

const props = withDefaults(defineProps<Props>(), {
  trendDirection: 'neutral'
})

const trendClass = computed(() => {
  if (!props.trend) return ''
  return props.trendDirection === 'up'
    ? 'metric-trend-up'
    : props.trendDirection === 'down'
      ? 'metric-trend-down'
      : 'metric-trend-neutral'
})

const trendArrow = computed(() => {
  if (!props.trend) return ''
  return props.trendDirection === 'up' ? '↑' : props.trendDirection === 'down' ? '↓' : ''
})
</script>

<template>
  <div class="metric-card">
    <span class="metric-label">{{ label }}</span>
    <span class="metric-value">{{ value }}</span>
    <span v-if="trend !== undefined" :class="['metric-trend', trendClass]">
      {{ trendArrow }} {{ Math.abs(trend) }}%
    </span>
  </div>
</template>
