import type { LinkedAccount } from './linked_account';
import type { Session } from './session';

/** Core user identity in the auth system. */
export interface User {
  id: string;
  email: string;
  username: string;
  is_active: boolean;
  permissions?: Record<string, boolean> | null;
  created_at: string;
  updated_at: string;
  sessions: Session[];
  linked_accounts: LinkedAccount[];
}
