<script setup lang="ts">
defineProps<{
  label: string
  variant?: 'default' | 'success' | 'warning' | 'error' | 'info'
  size?: 'sm' | 'md'
  dot?: boolean
}>()

const variantColors = {
  default: { bg: 'rgba(255, 255, 255, 0.1)', text: 'rgba(255, 255, 255, 0.7)' },
  success: { bg: 'rgba(102, 187, 106, 0.15)', text: '#66bb6a' },
  warning: { bg: 'rgba(255, 183, 77, 0.15)', text: '#ffb74d' },
  error: { bg: 'rgba(239, 83, 80, 0.15)', text: '#ef5350' },
  info: { bg: 'rgba(100, 181, 246, 0.15)', text: '#64b5f6' },
}
</script>

<template>
  <span
    class="badge"
    :class="[`badge--${size ?? 'md'}`, { 'badge--dot': dot }]"
    :style="{
      '--badge-bg': variantColors[variant ?? 'default'].bg,
      '--badge-text': variantColors[variant ?? 'default'].text,
    }"
  >
    <span v-if="dot" class="badge__dot" />
    <span class="badge__label">{{ label }}</span>
  </span>
</template>

<style scoped>
.badge {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 4px 10px;
  border-radius: 100px;
  background: var(--badge-bg);
  color: var(--badge-text);
  font-weight: 500;
  font-size: 12px;
  line-height: 1;
  white-space: nowrap;
}

.badge--sm {
  padding: 2px 8px;
  font-size: 11px;
}

.badge__dot {
  width: 6px;
  height: 6px;
  border-radius: 50%;
  background: currentColor;
  animation: pulse 2s ease-in-out infinite;
}

@keyframes pulse {
  0%, 100% {
    opacity: 1;
  }
  50% {
    opacity: 0.5;
  }
}
</style>
