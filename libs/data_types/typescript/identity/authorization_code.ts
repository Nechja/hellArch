import type { IdentityUser } from './identity_user';

/** OAuth 2.0 authorization code grant (short-lived, 5 minutes). */
export interface AuthorizationCode {
  code: string;
  client_id: string;
  user_id: string;
  redirect_uri: string;
  scope: string;
  code_challenge?: string | null;
  code_challenge_method?: string | null;
  expires_at: string;
  created_at: string;
  user?: IdentityUser | null;
}
