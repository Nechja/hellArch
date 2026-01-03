/** Audit log entry for tracking system events. */
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
