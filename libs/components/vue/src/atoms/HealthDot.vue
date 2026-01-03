<script setup lang="ts">
import { computed } from 'vue'

export type HealthStatus = 'healthy' | 'warning' | 'danger'

interface Props {
  status?: HealthStatus
  title?: string
}

const props = withDefaults(defineProps<Props>(), {
  status: 'healthy'
})

const dotClass = computed(() => {
  const statusMap: Record<HealthStatus, string> = {
    healthy: 'health-dot-healthy',
    warning: 'health-dot-warning',
    danger: 'health-dot-danger'
  }
  return statusMap[props.status]
})
</script>

<template>
  <span :class="dotClass" :title="title" />
</template>
