/** Dead letter entry for tracking failed events that exhausted retries. */
export interface DeadLetter {
  id: string;
  event_type: string;
  event_id: string;
  source: string;
  original_queue: string;
  death_reason: string;
  retry_count: number;
  body: string;
  error_message?: string | null;
  created_at: string;
}
