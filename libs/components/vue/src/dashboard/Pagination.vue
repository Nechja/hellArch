<script setup lang="ts">
import { computed } from 'vue'

interface Props {
  currentPage: number
  totalPages: number
  visiblePages?: number
}

const props = withDefaults(defineProps<Props>(), {
  visiblePages: 5
})

const emit = defineEmits<{
  'update:currentPage': [page: number]
}>()

const goToPage = (page: number) => {
  if (page < 1 || page > props.totalPages || page === props.currentPage) return
  emit('update:currentPage', page)
}

const visiblePageNumbers = computed(() => {
  if (props.totalPages <= props.visiblePages) {
    return Array.from({ length: props.totalPages }, (_, i) => i + 1)
  }

  const pages: number[] = []
  const half = Math.floor(props.visiblePages / 2)
  let start = Math.max(1, props.currentPage - half)
  let end = Math.min(props.totalPages, start + props.visiblePages - 1)

  if (end - start < props.visiblePages - 1) {
    start = Math.max(1, end - props.visiblePages + 1)
  }

  if (start > 1) {
    pages.push(1)
    if (start > 2) pages.push(-1) // ellipsis
  }

  for (let i = start; i <= end; i++) {
    pages.push(i)
  }

  if (end < props.totalPages) {
    if (end < props.totalPages - 1) pages.push(-1) // ellipsis
    pages.push(props.totalPages)
  }

  return pages
})

const getPageClass = (page: number) => {
  if (page === props.currentPage) return 'page-btn-active'
  return 'page-btn'
}
</script>

<template>
  <div class="pagination">
    <button
      type="button"
      :class="currentPage <= 1 ? 'page-btn-disabled' : 'page-btn'"
      :disabled="currentPage <= 1"
      @click="goToPage(currentPage - 1)"
    >
      ←
    </button>

    <template v-for="page in visiblePageNumbers" :key="page">
      <span v-if="page === -1" class="page-ellipsis">…</span>
      <button v-else type="button" :class="getPageClass(page)" @click="goToPage(page)">
        {{ page }}
      </button>
    </template>

    <button
      type="button"
      :class="currentPage >= totalPages ? 'page-btn-disabled' : 'page-btn'"
      :disabled="currentPage >= totalPages"
      @click="goToPage(currentPage + 1)"
    >
      →
    </button>
  </div>
</template>
