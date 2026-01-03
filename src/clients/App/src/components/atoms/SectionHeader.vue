<script setup lang="ts">
defineProps<{
  title: string
}>()
</script>

<template>
  <h2 class="section-header text-overline text-medium-emphasis mb-4 font-weight-medium">
    {{ title }}
  </h2>
</template>

<style scoped>
.section-header {
  letter-spacing: 0.1em;
}
</style>
