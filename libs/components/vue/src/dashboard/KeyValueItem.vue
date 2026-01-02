<script setup lang="ts">
interface Props {
  label: string
  value?: string | number | null
}

defineProps<Props>()
</script>

<template>
  <div class="kv-row">
    <span class="kv-key">{{ label }}</span>
    <span class="kv-value">{{ value }}</span>
  </div>
</template>
