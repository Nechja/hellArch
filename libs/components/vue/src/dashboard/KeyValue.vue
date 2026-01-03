<script setup lang="ts">
import { computed } from 'vue'

interface Props {
  grid?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  grid: false
})

const containerClass = computed(() => (props.grid ? 'kv-grid' : 'kv-list'))
</script>

<template>
  <div :class="containerClass">
    <slot />
  </div>
</template>
