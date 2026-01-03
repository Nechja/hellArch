<script setup lang="ts">
defineProps<{
  variant?: 'text' | 'circular' | 'rectangular' | 'card'
  width?: string | number
  height?: string | number
  lines?: number
}>()

function formatSize(value: string | number | undefined, fallback: string): string {
  if (value === undefined) return fallback
  return typeof value === 'number' ? `${value}px` : value
}
</script>

<template>
  <!-- Multiple lines of text -->
  <div v-if="variant === 'text' && (lines ?? 1) > 1" class="skeleton-lines">
    <div
      v-for="i in lines"
      :key="i"
      class="skeleton skeleton--text"
      :style="{
        width: i === lines ? '60%' : '100%',
        height: formatSize(height, '16px'),
      }"
    />
  </div>

  <!-- Single skeleton -->
  <div
    v-else
    class="skeleton"
    :class="`skeleton--${variant ?? 'rectangular'}`"
    :style="{
      width: formatSize(width, variant === 'circular' ? '40px' : '100%'),
      height: formatSize(height, variant === 'text' ? '16px' : variant === 'card' ? '120px' : '40px'),
    }"
  />
</template>

<style scoped>
.skeleton {
  background: linear-gradient(
    90deg,
    rgba(255, 255, 255, 0.04) 25%,
    rgba(255, 255, 255, 0.08) 50%,
    rgba(255, 255, 255, 0.04) 75%
  );
  background-size: 200% 100%;
  animation: shimmer 1.5s ease-in-out infinite;
}

.skeleton--text {
  border-radius: 4px;
}

.skeleton--circular {
  border-radius: 50%;
}

.skeleton--rectangular {
  border-radius: 8px;
}

.skeleton--card {
  border-radius: 12px;
}

.skeleton-lines {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

@keyframes shimmer {
  0% {
    background-position: 200% 0;
  }
  100% {
    background-position: -200% 0;
  }
}
</style>
