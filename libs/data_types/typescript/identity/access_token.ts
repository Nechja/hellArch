import type { IdentityUser } from './identity_user';

/** OAuth 2.0 access token (short-lived, 1 hour).
Stored as SHA-256 hash for security. */
export interface AccessToken {
  token_hash: string;
  user_id: string;
  client_id: string;
  scope: string;
  expires_at: string;
  created_at: string;
  user?: IdentityUser | null;
}
