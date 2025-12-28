import type { UserProfile } from './user_profile';

export type Theme = 'system' | 'light' | 'dark';

/** User preferences and settings. */
export interface UserPreferences {
  id: string;
  user_profile_id: string;
  theme: Theme;
  locale?: string | null;
  timezone?: string | null;
  email_notifications: boolean;
  push_notifications: boolean;
  in_app_notifications: boolean;
  created_at: string;
  updated_at: string;
  user_profile?: UserProfile | null;
}
