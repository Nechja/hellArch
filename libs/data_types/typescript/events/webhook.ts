import type { WebhookDelivery } from './webhook_delivery';

/** A registered webhook endpoint that receives event notifications. */
export interface Webhook {
  id: string;
  url: string;
  secret: string;
  event_types: string[];
  is_active: boolean;
  created_at: string;
  updated_at: string;
  deliveries: WebhookDelivery[];
}
