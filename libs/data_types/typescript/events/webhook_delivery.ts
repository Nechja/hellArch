import type { Webhook } from './webhook';

/** Record of a webhook delivery attempt. */
export interface WebhookDelivery {
  id: string;
  webhook_id: string;
  event_id: string;
  event_type: string;
  payload?: string | null;
  status_code: number;
  response_body?: string | null;
  success: boolean;
  attempt: number;
  created_at: string;
  webhook?: Webhook | null;
}
