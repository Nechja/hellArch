import type { RefreshToken } from './refresh_token';

/** Local identity user with username/password authentication. */
export interface IdentityUser {
  id: string;
  email: string;
  username: string;
  password_hash: string;
  is_active: boolean;
  created_at: string;
  updated_at: string;
  refresh_tokens: RefreshToken[];
}
