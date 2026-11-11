import { caseStudyData, homeData, profilePageData } from './structured-data';

describe('structured data', () => {
  it('describes the start page as the person behind the site and the site itself', () => {
    const data = homeData(
      {
        fullName: 'Max Muster',
        titleLine: 'Fullstack-Entwickler · .NET & Angular',
        location: 'Musterstadt',
        gitHubUrl: 'https://github.com/example',
        linkedInUrl: undefined,
        imagePath: '/api/public/documents/7',
      },
      ['C#', 'Angular'],
      'de',
    );

    expect(data).toEqual({
      '@context': 'https://schema.org',
      '@graph': [
        {
          '@type': 'Person',
          '@id': 'https://surac.dev/#person',
          name: 'Max Muster',
          jobTitle: 'Fullstack-Entwickler · .NET & Angular',
          url: 'https://surac.dev/',
          image: 'https://surac.dev/api/public/documents/7',
          homeLocation: { '@type': 'Place', name: 'Musterstadt' },
          sameAs: ['https://github.com/example'],
          knowsAbout: ['C#', 'Angular'],
        },
        {
          '@type': 'WebSite',
          '@id': 'https://surac.dev/#website',
          url: 'https://surac.dev/',
          name: 'surac.dev',
          inLanguage: 'de',
          publisher: { '@id': 'https://surac.dev/#person' },
        },
      ],
    });
  });

  it('leaves out what the profile does not have', () => {
    const data = homeData({ fullName: 'Max Muster' }, [], 'en');
    const person = data['@graph'][0];

    expect(Object.keys(person)).toEqual(['@type', '@id', 'name', 'url']);
    expect(data['@graph'][1]).toMatchObject({ inLanguage: 'en' });
  });

  it('marks the about page as the profile page of that person', () => {
    expect(profilePageData('Max Muster', 'Über mich', '/ueber-mich', 'de')).toEqual({
      '@context': 'https://schema.org',
      '@type': 'ProfilePage',
      url: 'https://surac.dev/ueber-mich',
      name: 'Über mich',
      inLanguage: 'de',
      mainEntity: {
        '@type': 'Person',
        '@id': 'https://surac.dev/#person',
        name: 'Max Muster',
        url: 'https://surac.dev/',
      },
    });
  });

  it('describes a case study as a work of that person with its stack as keywords', () => {
    const data = caseStudyData(
      {
        title: 'Beispielprojekt',
        teaser: 'Ein Beispiel.',
        stackTags: ['.NET', 'PostgreSQL'],
        path: '/en/projects/beispiel',
        imagePath: '/api/public/documents/21',
      },
      'Max Muster',
      'en',
    );

    expect(data).toEqual({
      '@context': 'https://schema.org',
      '@type': 'CreativeWork',
      name: 'Beispielprojekt',
      description: 'Ein Beispiel.',
      url: 'https://surac.dev/en/projects/beispiel',
      inLanguage: 'en',
      keywords: '.NET, PostgreSQL',
      image: 'https://surac.dev/api/public/documents/21',
      author: {
        '@type': 'Person',
        '@id': 'https://surac.dev/#person',
        name: 'Max Muster',
        url: 'https://surac.dev/',
      },
    });
  });
});
