import type { IdentityUser } from './identity_user';

/** OAuth 2.0 refresh token (long-lived, 30 days).
Stored as SHA-256 hash for security. */
export interface RefreshToken {
  token_hash: string;
  user_id: string;
  client_id: string;
  scope: string;
  expires_at: string;
  created_at: string;
  user?: IdentityUser | null;
}
