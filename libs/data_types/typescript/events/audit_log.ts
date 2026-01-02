/** Stores a record of every event that passes through the system for audit purposes. */
export interface AuditLog {
  id: string;
  event_type: string;
  event_id: string;
  source: string;
  actor_id?: string | null;
  target_id?: string | null;
  metadata?: string | null;
  created_at: string;
}
