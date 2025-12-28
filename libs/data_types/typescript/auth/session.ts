/** User session for cookie-based authentication. */
export interface Session {
  id: string;
  user_id: string;
  token: string;
  ip_address?: string | null;
  user_agent?: string | null;
  expires_at: string;
  created_at: string;
  user?: User | null;
}
