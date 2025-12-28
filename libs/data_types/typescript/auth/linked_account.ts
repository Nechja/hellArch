import type { User } from './user';

/** OAuth provider account linked to a user.
Stores provider-specific identity info. Token storage is optional
(only needed if we want to call provider APIs on behalf of user). */
export interface LinkedAccount {
  id: string;
  user_id: string;
  provider: string;
  provider_user_id: string;
  email?: string | null;
  username?: string | null;
  avatar_url?: string | null;
  access_token?: string | null;
  refresh_token?: string | null;
  token_expires_at?: string | null;
  created_at: string;
  updated_at: string;
  user?: User | null;
}
