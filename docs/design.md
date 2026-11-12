# Design

The visual system of the public site. Chosen on 2026-11-07 from five mock-ups: direction A
"Editorial" with the fact bar of direction B. The admin area reuses the tokens but is utilitarian.

## Principles

1. **Restraint is the differentiation.** The typical developer portfolio is dark, animated and
   gradient-heavy. This site is a well-set page: paper, type, rules, whitespace. It should read like
   the printed CV, only alive.
2. **HR-first above the fold.** Name, title line, keyword line, years, location, two buttons
   (CV PDF, e-mail), text links to GitHub and LinkedIn, photo. Nothing moves.
3. **Type does the work.** A serif with optical sizes for display and lede text, the system grotesk
   for UI, labels and metadata. One accent colour, used for kickers, links and the primary button
   hover.
4. **Content from the database, structure from the components.** No hardcoded text in templates.

## Tokens (`src/styles/_tokens.scss`)

Light (default):

| Token | Value | Use |
|---|---|---|
| `--color-paper` | `#f6f3ec` | page background |
| `--color-card` | `#fffdf8` | cards, project tiles |
| `--color-ink` | `#1b1a17` | text, primary button |
| `--color-ink-soft` | `#2d2b26` | lede text |
| `--color-muted` | `#6b665c` | metadata, labels |
| `--color-rule` | `#d9d3c4` | hairlines, borders |
| `--color-accent` | `#8a2f1f` | kickers, links, hover |
| `--color-focus` | `#1f3bd1` | focus ring (3:1 against paper) |

Dark (`@media (prefers-color-scheme: dark)`, no toggle):

| Token | Value |
|---|---|
| `--color-paper` | `#161513` |
| `--color-card` | `#1c1b19` |
| `--color-ink` | `#ece8df` |
| `--color-ink-soft` | `#c9c4b8` |
| `--color-muted` | `#9a958a` |
| `--color-rule` | `#2d2c28` |
| `--color-accent` | `#d49a7a` |
| `--color-focus` | `#8fa3ff` |

Typography:

| Token | Value |
|---|---|
| `--font-serif` | `'Source Serif 4', 'Source Serif 4 Fallback', 'Iowan Old Style', Palatino, Georgia, serif` |
| `--font-sans` | `system-ui, -apple-system, 'Segoe UI', Helvetica, Arial, sans-serif` |
| `--font-mono` | `ui-monospace, 'Cascadia Mono', Consolas, Menlo, monospace` |
| `--text-base` | `17px / 1.6` |
| `--measure` | `64ch` maximum line length for prose |

Spacing is an 8-px scale (`--space-1` = 4px … `--space-9` = 96px). Radii are 0 (editorial) except
form controls (2px). Shadows are not used.

## Typography rules

- `h1` serif 400, `clamp(44px, 7vw, 84px)`, line-height 0.98, letter-spacing −0.015em.
- Section headings are small caps labels in the grotesk (13px, letter-spacing 0.18em, muted) with a
  numbered counter in the accent colour, not big serif headings. The serif belongs to the hero and to
  project titles.
- Lede and pull quotes: serif 21–24px, max `--measure`.
- Body: serif 17px for prose, grotesk 14–15px for metadata, tables, buttons.
- Numbers in the fact bar: grotesk 700, 40px, tabular figures.

## Fonts

Source Serif 4 (Adobe, SIL OFL 1.1), Latin subset, self-hosted under `public/fonts/` as versioned
files, served immutable for a year. The variable font is instanced to fixed weights with the
optical-size axis kept (`fonttools varLib.instancer <file> wght=400`): `source-serif-4-latin-400-v2`
(47 kB, preloaded), `-700-v2` and `-400-italic-v2` (loaded only by pages with bold or italic serif
text). The full 200-900 file (120 kB) delayed the LCP photo by ~0.8 s in Lighthouse's mobile profile
(2026-11-09). Preloaded in `index.html`; `font-display: swap` with a size-adjusted fallback
(`'Source Serif 4 Fallback'` via `@font-face` with `size-adjust`) to avoid layout shift. Replacing
a font file means a new version suffix.

## Monogram and icons

The mark is the monogram "LS": an upright L in ink and an italic S in the accent, set from Source
Serif 4 so it belongs to the type of the page. Chosen on 2026-11-12 from five directions (monogram,
`ls_` prompt, seal, geometric tile, `surac.dev` wordmark); the others broke the principles above
(a second typeface, an accent area, ornament, or the domain in place of the name).

- **The header stays text.** The set name is already the wordmark; "LS" beside "Leonard Surac" would
  say the name twice. The monogram goes where the name does not fit: browser tab, home screen,
  mail signature.
- **Two cuts, like the font's optical sizes.** The display cut (L `wght` 600, italic S 400, `opsz`
  60, cap height 30/64; the spur of the L runs into the S, which carries a thin paper-coloured
  outline) is used from 180 px up: `apple-touch-icon.png`, `icon-192.png`, `icon-512.png`. The small
  cut (L 700, S 760, `opsz` 8, cap height 41/64, the S set apart) is used up to 48 px:
  `favicon.svg`, `favicon.ico` (16, 32, 48).
- The letters are outlines (paths), not text: a favicon cannot load the web font. `favicon.svg`
  switches to the dark tokens under `prefers-color-scheme: dark`; the PNGs are light.
- `site.webmanifest` names the 192 and 512 icons; the SSR server sends it as
  `application/manifest+json`. Built outside the repository from the variable font with fonttools
  and rendered with Chromium; to change the mark, rebuild every file from the parameters above.

## Layout

- Content width 1040px, 24px side gutters, single column below 760px.
- Sections are separated by 1px rules in `--color-rule`, 64px vertical rhythm (40px on phones).
- Section grid: 220px label column + content column on desktop; stacked on phones.
- Project cards: card surface, 1px rule, title in serif 26px, stack line in grotesk 13px, two text
  links. The thumbnail slot shows a real screenshot or nothing (no placeholder pattern in production).
- Fact bar (from mock-up B): four cells separated by rules, number + label, directly under the
  hero.
- Timeline: date column (grotesk 14px muted) + entry; dotted rules between entries.
- Skills: three columns, lists with hairline rows, "since year" right-aligned in muted grotesk.
- About page: the numbered section grid of the start page; serif text beside a captioned photo in a
  4:5 frame (the frame reserves the space, so nothing shifts while it loads). The photo's side
  alternates from section to section; on phones it sits above the text. Photos show the maintainer,
  his cats or his things, never other identifiable people.
- Case study order follows the story: problem, own role, solution and implementation (the case
  study text with its code excerpt), decisions with trade-offs, outcome.

## Links

Links to other sites open in a new tab with `rel="noopener noreferrer"`, in templates and in the
Markdown the API renders; site paths and `mailto:` open in place. An E2E test checks every link to
another origin on the main pages.

## Interaction and motion

- Hover: colour change on links and buttons, 120ms. Focus: 2px outline in `--color-focus` with
  2px offset, never removed.
- `prefers-reduced-motion: reduce` sets all transitions to 0.
- No scroll-triggered animation, no parallax, no sticky hero, no animated counters, no carousel.

## Accessibility

- WCAG 2.2 AA contrast for all text (checked for both themes). Accent on paper is 7.4:1;
  muted on paper is 5.0:1.
- Every interactive element reachable by keyboard; skip link to main content; landmarks (`header`,
  `main`, `nav`, `footer`); language attribute follows the locale; images have alt text from the
  content model.
- The site renders fully without JavaScript (SSR); hydration only enhances.

## Explicitly never

Gradient blobs, glassmorphism, neon on black, Tailwind default palette, Framer-Motion-style fade-ins,
typewriter headlines, "Hi, I'm … 👋", skill bars or percentage rings, hero video, cookie banner,
contact form, chat widget, dark-mode toggle, emoji as icons, stock illustrations, animated
background particles.
