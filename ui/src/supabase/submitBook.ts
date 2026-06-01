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
      contributor_name: null,
      submitted_at:     entry.submittedAt,
    });
  if (error) console.error('[submitBook]', error.message);
  return !error;
}

export async function updateContributor(
  title: string,
  author: string,
  submittedAt: string,
  name: string,
): Promise<boolean> {
  const { error } = await supabase
    .schema('reminded_me')
    .from('books')
    .update({ contributor_name: name.trim() || null })
    .eq('title', title)
    .eq('author', author)
    .eq('submitted_at', submittedAt);
  if (error) console.error('[updateContributor]', error.message);
  return !error;
}
