<script setup lang="ts">
defineProps<{
  title: string
  subtitle?: string
}>()
</script>

<template>
  <header class="page-header mb-10">
    <h1 class="text-h4 font-weight-bold mb-2">
      {{ title }}
    </h1>
    <p v-if="subtitle" class="text-body-1 text-medium-emphasis mb-0">
      {{ subtitle }}
    </p>
  </header>
</template>

<style scoped>
.page-header h1 {
  letter-spacing: -0.02em;
}
</style>
