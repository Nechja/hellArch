import type { IdentityUser } from './identity_user';

/** OAuth 2.0 access token (short-lived, 1 hour).
Stored for validation until expiry. */
export interface AccessToken {
  token: string;
  user_id: string;
  client_id: string;
  scope: string;
  expires_at: string;
  created_at: string;
  user?: IdentityUser | null;
}
