# SEO

What the site does for search engines and link previews, the baseline, and every change with the
date it is measured on. Without a row in "Changes" a later measurement has nothing to compare
against. Access to Search Console and Seq lives in the maintainer's private tooling, not here.

## Setup

- Search Console: domain property `sc-domain:surac.dev` (DNS TXT verification). Sitemap
  `https://surac.dev/sitemap.xml` (16 URLs: 8 pages in German and English), submitted on 2026-11-09
  and again on 2026-11-11.
- Per page (`core/seo/seo.ts`): title, description, canonical, hreflang `de`/`en`/`x-default`,
  Open Graph and `twitter:card`. Pages without an image of their own show `public/og-image.png`
  (name and title line, no photo).
- JSON-LD (`core/seo/structured-data.ts`): `Person` and `WebSite` on the start page, `ProfilePage`
  on the about page, `CreativeWork` on case studies. Legal pages have none.
- One URL per page: a trailing slash answers 301 to the form without it (`SuracDev.Web/nginx.conf`).
- On-site behaviour after the click is in Seq (page views with referrer host on the first page of a
  visit). Seq is a lower bound: no beacon without JavaScript. The maintainer stays signed in on every
  device, so the maintainer's own clicks are logged as the admin and drop out of the visitor figures.

## Interpreting the data

- Search Console data is final after 2 to 3 days. Compare equal-length periods, never single days.
- A personal portfolio gets small numbers. The realistic queries are the name and links shared in
  applications; judge a change over 4 weeks or more.

## Baseline (2026-11-11, two days after the launch on 2026-11-09)

| Source | Figure |
|---|---|
| Search Console, 2026-11-04 to 2026-11-10 | 0 clicks, 1 impression (`/en`, position 3), no queries |
| Index | 14 of 16 sitemap URLs indexed; `/impressum` "Gefunden – zurzeit nicht indexiert", `/projekte/surac-dev` "URL ist Google nicht bekannt" (manual indexing request still to do) |
| Seq, 3 days | 38 anonymous visits, 24 with one page; about half from the maintainer, before staying signed in; the rest mostly crawlers and link previews from cloud networks; no external referrer |

## Changes

| Date | Change | Measure on | How |
|---|---|---|---|
| 2026-11-11 | JSON-LD per page, preview card for pages without an image, descriptions for the about and legal pages, 301 for trailing slashes | 2026-12-09 | Search Console totals and queries with the name (2026-11-11 to 2026-12-08 against the baseline), index status of all sitemap URLs, detected rich results (URL inspection), Seq visits and referrers over 30 days |

## On-site usage (Seq)

One row per monthly review, aggregates only (Seq keeps 30 days).

| Date | Period | Visits | One page | Projects | About | English | Google | LinkedIn | Application links | CV PDF |
|---|---|---|---|---|---|---|---|---|---|---|
| 2026-11-11 | 3 days | 38 | 24 | 4 | 4 | 2 | 0 | 0 | 1 | 13 |
