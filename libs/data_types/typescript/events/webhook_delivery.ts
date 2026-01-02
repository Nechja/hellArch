import type { Webhook } from './webhook';

/** Tracks each attempt to deliver an event to a webhook endpoint. */
export interface WebhookDelivery {
  id: string;
  webhook_id: string;
  event_id: string;
  event_type: string;
  payload: string;
  status_code?: number | null;
  response_body?: string | null;
  error?: string | null;
  attempt_number: number;
  created_at: string;
  webhook: Webhook;
}
