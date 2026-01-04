/** OAuth 2.0 client application registration. */
export interface OAuthClient {
  id: string;
  secret_hash: string;
  name: string;
  redirect_uris: string[];
  is_active: boolean;
  created_at: string;
}
