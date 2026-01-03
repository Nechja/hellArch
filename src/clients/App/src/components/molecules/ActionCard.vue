<script setup lang="ts">
defineProps<{
  title: string
  description: string
  icon: string
  to?: string
  href?: string
  disabled?: boolean
}>()
</script>

<template>
  <v-card
    :to="to"
    :href="href"
    :disabled="disabled"
    :class="{ 'action-card': !disabled, 'action-card--disabled': disabled }"
    class="h-100"
  >
    <v-card-text class="d-flex align-center ga-4 pa-5">
      <div class="action-card__icon" :class="{ 'action-card__icon--disabled': disabled }">
        <v-icon size="24">{{ icon }}</v-icon>
      </div>
      <div class="flex-grow-1">
        <h3
          class="text-body-1 font-weight-semibold mb-1"
          :class="disabled ? 'text-disabled' : 'text-high-emphasis'"
        >
          {{ title }}
        </h3>
        <p
          class="text-body-2 mb-0"
          :class="disabled ? 'text-disabled' : 'text-medium-emphasis'"
        >
          {{ description }}
        </p>
      </div>
      <v-icon
        v-if="!disabled"
        size="20"
        class="text-medium-emphasis action-card__arrow"
      >
        mdi-chevron-right
      </v-icon>
    </v-card-text>
  </v-card>
</template>

<style scoped>
.action-card {
  cursor: pointer;
}

.action-card__icon {
  width: 48px;
  height: 48px;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, rgba(255, 77, 141, 0.15) 0%, rgba(255, 138, 101, 0.1) 100%);
  color: #ff4d8d;
  transition: all 0.2s ease;
}

.action-card:hover .action-card__icon {
  background: linear-gradient(135deg, rgba(255, 77, 141, 0.25) 0%, rgba(255, 138, 101, 0.15) 100%);
  transform: scale(1.05);
}

.action-card__icon--disabled {
  background: rgba(255, 255, 255, 0.05);
  color: rgba(255, 255, 255, 0.3);
}

.action-card__arrow {
  opacity: 0;
  transform: translateX(-4px);
  transition: all 0.2s ease;
}

.action-card:hover .action-card__arrow {
  opacity: 1;
  transform: translateX(0);
}
</style>
