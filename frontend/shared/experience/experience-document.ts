export const palettes = ['forest', 'plum', 'ocean'] as const;
export const typographyOptions = ['editorial', 'system'] as const;
export const componentKinds = ['hero', 'banner', 'note'] as const;

export interface ExperienceLink {
  target: 'home' | 'account';
  label: string;
}

export interface ExperienceSection {
  kind: (typeof componentKinds)[number];
  title: string;
  body: string;
}

export interface ExperienceDocument {
  schemaVersion: 1;
  brand: string;
  palette: (typeof palettes)[number];
  typography: (typeof typographyOptions)[number];
  showNavigation: boolean;
  navigation: ExperienceLink[];
  sections: ExperienceSection[];
}

export interface PublishedExperience {
  version: number;
  configuration: ExperienceDocument;
}

export const bundledExperience: ExperienceDocument = {
  schemaVersion: 1,
  brand: 'FindYourFood',
  palette: 'forest',
  typography: 'editorial',
  showNavigation: true,
  navigation: [
    { target: 'home', label: 'Home' },
    { target: 'account', label: 'My account' },
  ],
  sections: [
    { kind: 'hero', title: 'Good food. Close by.', body: 'Your next favorite meal starts here.' },
    {
      kind: 'note',
      title: 'Make yourself at home',
      body: 'Create an account or sign in to get started with FindYourFood.',
    },
  ],
};

function isObject(value: unknown): value is Record<string, unknown> {
  return !!value && typeof value === 'object' && !Array.isArray(value);
}

function isText(value: unknown, max: number): value is string {
  return typeof value === 'string' && value.trim().length > 0 &&
    value.length <= max && !/[\u0000-\u001f\u007f-\u009f]/u.test(value);
}

export function isExperienceDocument(value: unknown): value is ExperienceDocument {
  if (!isObject(value)) return false;
  const { schemaVersion, brand, palette, typography, showNavigation, navigation, sections } = value;
  if (
    schemaVersion !== 1 || !isText(brand, 40) ||
    !palettes.some((option) => option === palette) ||
    !typographyOptions.some((option) => option === typography) ||
    typeof showNavigation !== 'boolean' || !Array.isArray(navigation) || navigation.length > 2 ||
    !Array.isArray(sections) || sections.length === 0 || sections.length > 3
  ) return false;

  const targets = new Set<unknown>();
  for (const link of navigation) {
    if (
      !isObject(link) || (link['target'] !== 'home' && link['target'] !== 'account') ||
      !isText(link['label'], 30) || targets.has(link['target'])
    ) return false;
    targets.add(link['target']);
  }

  const kinds = new Set<unknown>();
  for (const section of sections) {
    if (
      !isObject(section) || !componentKinds.some((kind) => kind === section['kind']) ||
      !isText(section['title'], 80) || !isText(section['body'], 280) || kinds.has(section['kind'])
    ) return false;
    kinds.add(section['kind']);
  }

  return kinds.has('hero');
}

export function isPublishedExperience(value: unknown): value is PublishedExperience {
  return isObject(value) && typeof value['version'] === 'number' && Number.isSafeInteger(value['version']) &&
    value['version'] > 0 && isExperienceDocument(value['configuration']);
}
