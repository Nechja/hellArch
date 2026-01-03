import type { StoredFile } from './stored_file';

/** Represents a processed variant of a stored file (e.g., thumbnail, optimized version). */
export interface FileVariant {
  id: string;
  file_id: string;
  variant_type: string;
  object_key: string;
  mime_type: string;
  size_bytes: number;
  created_at: string;
  file?: StoredFile | null;
}
