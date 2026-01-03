<script setup lang="ts">
defineProps<{
  icon?: string
  title: string
  description?: string
  actionLabel?: string
  actionIcon?: string
}>()

const emit = defineEmits<{
  action: []
}>()
</script>

<template>
  <div class="empty-state">
    <div class="empty-state__icon">
      <v-icon size="48">{{ icon ?? 'mdi-inbox-outline' }}</v-icon>
    </div>
    <h3 class="empty-state__title">{{ title }}</h3>
    <p v-if="description" class="empty-state__description">
      {{ description }}
    </p>
    <v-btn
      v-if="actionLabel"
      color="primary"
      variant="outlined"
      class="mt-4"
      @click="emit('action')"
    >
      <v-icon v-if="actionIcon" start>{{ actionIcon }}</v-icon>
      {{ actionLabel }}
    </v-btn>
  </div>
</template>

<style scoped>
.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  text-align: center;
  padding: 48px 24px;
}

.empty-state__icon {
  width: 80px;
  height: 80px;
  border-radius: 20px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, rgba(255, 77, 141, 0.1) 0%, rgba(255, 138, 101, 0.05) 100%);
  color: rgba(255, 255, 255, 0.4);
  margin-bottom: 20px;
}

.empty-state__title {
  font-size: 18px;
  font-weight: 600;
  color: rgba(255, 255, 255, 0.9);
  margin: 0 0 8px;
}

.empty-state__description {
  font-size: 14px;
  color: rgba(255, 255, 255, 0.5);
  margin: 0;
  max-width: 300px;
}
</style>
