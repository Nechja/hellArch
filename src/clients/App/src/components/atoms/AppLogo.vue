<script setup lang="ts">
defineProps<{
  size?: 'sm' | 'md' | 'lg' | 'xl'
  linkTo?: string
}>()

const sizeWidths = {
  sm: '100px',
  md: '140px',
  lg: '200px',
  xl: '280px',
}
</script>

<template>
  <component
    :is="linkTo ? 'RouterLink' : 'span'"
    :to="linkTo"
    class="app-logo text-decoration-none d-inline-block"
  >
    <img
      src="/logo.png"
      alt="HellArch"
      class="logo-img"
      :style="{ maxWidth: sizeWidths[size ?? 'md'] }"
    />
  </component>
</template>

<style scoped>
.app-logo {
  display: inline-flex;
  align-items: center;
}

.logo-img {
  height: auto;
}
</style>
