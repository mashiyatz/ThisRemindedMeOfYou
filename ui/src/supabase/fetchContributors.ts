import { supabase } from './client';

function getRoomId(): string {
  return new URLSearchParams(window.location.search).get('room') ?? 'default';
}

function normalizeKey(title: string, author: string): string {
  return `${title.trim().toLowerCase()}|${author.trim().toLowerCase()}`;
}

export async function fetchContributors(): Promise<Map<string, string>> {
  const { data, error } = await supabase
    .schema('reminded_me')
    .from('books')
    .select('title, author, contributor_name')
    .eq('room_id', getRoomId())
    .not('contributor_name', 'is', null);

  const map = new Map<string, string>();
  if (error || !data) {
    console.error('[fetchContributors]', error?.message);
    return map;
  }
  for (const row of data as { title: string; author: string; contributor_name: string | null }[]) {
    if (row.contributor_name) {
      map.set(normalizeKey(row.title, row.author), row.contributor_name);
    }
  }
  return map;
}

export function addContributor(
  map: Map<string, string>,
  title: string,
  author: string,
  name: string,
): void {
  map.set(normalizeKey(title, author), name);
}

export function lookupContributor(
  map: Map<string, string>,
  title: string,
  author: string,
): string | undefined {
  return map.get(normalizeKey(title, author));
}
