import type { IdentityUser } from './identity_user';

/** OAuth 2.0 access token (short-lived, 1 hour).
Stored as SHA-256 hash for security.
Bound to client fingerprint to prevent token theft. */
export interface AccessToken {
  token_hash: string;
  user_id: string;
  client_id: string;
  scope: string;
  expires_at: string;
  created_at: string;
  client_fingerprint?: string | null;
  user?: IdentityUser | null;
}
