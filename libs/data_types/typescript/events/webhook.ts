import type { WebhookDelivery } from './webhook_delivery';

export type WebhookFormat = 'raw' | 'discord' | 'slack';

/** Webhook endpoint registration for event delivery. */
export interface Webhook {
  id: string;
  name: string;
  url: string;
  secret: string;
  event_types: string[];
  format: WebhookFormat;
  is_active: boolean;
  created_at: string;
  updated_at: string;
  deliveries: WebhookDelivery[];
}
