/** Core user identity in the auth system. */
export interface User {
  id: string;
  email: string;
  username: string;
  created_at: string;
  updated_at: string;
  sessions: Session[];
  linked_accounts: LinkedAccount[];
}
