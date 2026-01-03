<script setup lang="ts">
import { useSlots } from 'vue'

interface Props {
  vertical?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  vertical: false
})

const slots = useSlots()
const hasContent = !!slots.default
</script>

<template>
  <div v-if="vertical" class="divider-vertical" />
  <div v-else-if="hasContent" class="divider-text">
    <span class="divider flex-1" />
    <slot />
    <span class="divider flex-1" />
  </div>
  <hr v-else class="divider" />
</template>
