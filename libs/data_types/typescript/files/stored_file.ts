import type { FileVariant } from './file_variant';

/** Represents a file stored in the object storage system. */
export interface StoredFile {
  id: string;
  owner_id: string;
  bucket: string;
  object_key: string;
  filename: string;
  mime_type: string;
  size_bytes: number;
  checksum_sha256?: string | null;
  metadata?: Record<string, string> | null;
  created_at: string;
  updated_at: string;
  deleted_at?: string | null;
  variants: FileVariant[];
}
