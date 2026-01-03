<script setup lang="ts">
defineProps<{
  size?: 'sm' | 'md' | 'lg' | 'xl'
  linkTo?: string
}>()

const sizeClasses = {
  sm: 'text-body-1',
  md: 'text-h6',
  lg: 'text-h5',
  xl: 'text-h4',
}
</script>

<template>
  <component
    :is="linkTo ? 'RouterLink' : 'span'"
    :to="linkTo"
    class="app-logo text-decoration-none d-inline-block"
  >
    <span
      class="font-weight-bold gradient-text"
      :class="sizeClasses[size ?? 'md']"
    >
      HellArch
    </span>
  </component>
</template>

<style scoped>
.app-logo {
  letter-spacing: -0.02em;
}
</style>
