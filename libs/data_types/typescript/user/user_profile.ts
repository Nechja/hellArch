/** Extended user profile information.
Linked to auth user via AuthUserId.
Email is denormalized from auth for read performance. */
export interface UserProfile {
  id: string;
  auth_user_id: string;
  email: string;
  display_name?: string | null;
  bio?: string | null;
  avatar_url?: string | null;
  created_at: string;
  updated_at: string;
  preferences?: UserPreferences | null;
}
