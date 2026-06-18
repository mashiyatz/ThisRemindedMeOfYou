import { supabase } from './client';
import type { BookEntry } from '../types/BookData';

function getRoomId(): string {
  return new URLSearchParams(window.location.search).get('room') ?? 'default';
}

export async function submitBook(entry: BookEntry): Promise<boolean> {
  const { error } = await supabase
    .schema('reminded_me')
    .from('books')
    .insert({
      room_id:          getRoomId(),
      title:            entry.title,
      author:           entry.author,
      cover_image_url:  entry.coverImageUrl,
      response_text:    entry.responseText,
      is_handwritten:   entry.isHandwritten,
      wants_narrated:   entry.wantsNarrated,
      contributor_name: entry.contributorName?.trim() || null,
      submitted_at:     entry.submittedAt,
    });
  if (error) console.error('[submitBook]', error.message);
  return !error;
}
