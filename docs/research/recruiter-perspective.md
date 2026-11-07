# Report: Was Recruiter und Hiring Manager von Leonards Bewerbungs-Webseite brauchen

**Methodik-Hinweis vorab:** Reddit war in dieser Session technisch nicht erreichbar (reddit.com, old.reddit.com und Redlib-Mirrors blockiert; Suchmaschinen liefern keine Reddit-Threads). Ersatz: Hacker-News-Threads (direkt gelesen, teils mit Algolia-API), Team Blind, dev.to-Survey unter 60+ Hiring Managern, deutsche Foren (java-forum.org), deutsche Karriereportale (StepStone, Karrierebibel, Haufe, IQB), offizielle Arbeitgeberseiten (dmTECH, inovex) und Studien (StepStone/MindTake, Gisma, Indeed/Censuswide, Robert Half, Statista, Stack Overflow). Viele englische „Portfolio-Guides“ sind SEO-Vendor-Content ohne Primärdaten – ich markiere das jeweils. Echte Hirer-Aussagen sind als solche gekennzeichnet (HN-Username, Rolle).

---

## A) Was Recruiter / HR beim 30-Sekunden-Screening brauchen (priorisiert)

**Grundbefund: HR klickt den Link selten; wenn, dann kurz. Die Seite muss in <30 s alles liefern, was sonst im Lebenslauf steht – und den Lebenslauf selbst.**

Belege zur Zeit:
- Eye-Tracking StepStone Österreich/MindTake 2018 (n=33): Personaler schauen im Schnitt **43 Sekunden** auf einen Lebenslauf, davon 22,3 s auf Berufserfahrung, ~7 s auf die aktuelle Stelle, 6,1 s Ausbildung, 4,7 s Soft Skills; das Foto bekommt den ersten Blick (1,8 s). Selbstschätzung der Personaler: 2 Minuten. ([Haufe](https://www.haufe.de/personal/hr-management/recruiting-auf-was-personaler-im-lebenslauf-achten_80_475630.html), [IQB](https://iqb.de/karrieremagazin/bewerbung/darauf-achten-personaler-wirklich-der-lebenslauf/))
- Ladders 2012 (n=30 Recruiter): ~6 s Erstsichtung, 80 % der Zeit auf Name, aktuelle/vorherige Titel+Firma, Daten, Ausbildung. Die kursierende „7,4 s“-Zahl ist eine spätere Ladders-Variante; Methodik dünn. ([recruiter.com](https://www.recruiter.com/i/theladders-reveals-research-reporting-that-resumes-spend-six-seconds-with-a-recruiter/))
- Indeed/Censuswide Okt. 2026 (n=300 HR-Entscheider DE): **35 % verwenden maximal eine Minute auf die Erstsichtung**; 56 % haben mehr Bewerbungen als vor 12 Monaten; 42 % nutzen offiziell Software/KI zur Vorauswahl. ([Indeed Newsroom](https://de.indeed.com/news/releases/ki-im-recruiting-wie-hr-abteilungen-auf-steigende-bewerbungszahlen-reagieren))
- Klickverhalten: Blind-Recruiter: „can't remember the last time I clicked a link in any of them“ ([Blind](https://www.teamblind.com/post/do-recruiters-or-hiring-managers-check-github-sfrfrpxj)); HN beager: „most pure recruiters aren't looking at your GitHub profile other than ensuring that you've provided one“ ([HN](https://news.ycombinator.com/item?id=19413348)); java-forum ARadauer (Fachbereich, entscheidet mit): „Die Personalabteilung nicht, aber die Entscheider in der Fachabteilung schon.“ ([java-forum.org](https://www.java-forum.org/thema/bewerben-nach-dem-studium-auf-eigene-projekte-referenzieren.185477/))
- Gegenposition: Profy.dev-Umfrage 2021 (60+ Hiring Manager, React-Umfeld): „The overwhelming majority of hiring managers look at your website... but don't give a crap“ – man schaut, es ändert aber die Chancen kaum. ([dev.to/profydev](https://dev.to/profydev/this-survey-among-60-hiring-managers-reveals-don-t-waste-your-time-on-a-react-portfolio-website-17ge))

**Prioritätenliste HR:**

1. **Identität + Rolle + Stack + Erfahrung in einer Zeile, ohne Scrollen.** „Your title and primary stack should be visible without scrolling“ ([dev.to Checklist](https://dev.to/alx094/what-to-include-in-a-developer-portfolio-a-recruiter-readable-checklist-1nj7)); techinterview.org: Name, 1–3 Sätze Intro, was du baust, klare Navigation; Ziel „reviewer can extract everything they need in 30 seconds“ ([techinterview.org](https://www.techinterview.org/post/3233474627/personal-website-portfolio-engineers/)).
2. **Vollständige Kontaktdaten direkt sichtbar** (Name, Ort, E-Mail, Handy). StepStone: Kontaktdaten müssen auf der Seite stehen, „damit niemand zurück ins E-Mail-Programm muss“. ([StepStone Bewerbungshomepage](https://www.stepstone.de/magazin/artikel/bewerbungshomepage))
3. **Lebenslauf als PDF, ein Klick.** Der Lebenslauf ist für 68 % der Personaler das wichtigste Dokument; Anschreiben 22 %, Zeugnisse 10 %. ([Haufe](https://www.haufe.de/personal/hr-management/recruiting-auf-was-personaler-im-lebenslauf-achten_80_475630.html)) Karrierebibel: „Wichtige Unterlagen sollten als PDF zum Download bereitstehen.“ ([Karrierebibel](https://karrierebibel.de/bewerbungshomepage/))
4. **Flache Struktur, klassische Reihenfolge wie eine Bewerbungsmappe.** StepStone: „Von Unterebenen oder gar Unterunterebenen ist abzuraten“; Personaler mit vielen Bewerbungen empfindet eine verschachtelte Seite als „Zeitfresser“. Karrierebibel: „Je einfacher und gewohnter die Struktur, desto besser!“
5. **Keyword-dichte erste Seite** (Screener pattern-matchen): HN ponyous: „make the first page keyword heavy“, Review läuft „through at least 2 levels“ ([HN 21191906](https://news.ycombinator.com/item?id=21191906)); HN simoleone (Recruiter): Recruiter sind „basically a pattern-matching robot“ ([HN 19413348](https://news.ycombinator.com/item?id=19413348)). Konkret: „C#“, „.NET 8“, „ASP.NET Core“, „Angular“, „PostgreSQL“, „Docker“, „IHK Fachinformatiker Anwendungsentwicklung“, „5 Jahre“ wörtlich auf der Startseite.
6. **Smartphone-tauglich.** Karrierebibel fordert Responsive Design explizit; >2/3 der Bewerbungen werden am Bildschirm gelesen, unter 36-Jährige 80 % ([Haufe](https://www.haufe.de/personal/hr-management/recruiting-auf-was-personaler-im-lebenslauf-achten_80_475630.html)).
7. **Aktualität + Fehlerfreiheit.** Rechtschreibfehler sind für 64 % ein „klares No-Go“ (Haufe); ~1/3 lehnen bei einem einzigen Fehler ab, ~70 % bei >3 Fehlern (IQB).
8. **Foto** – in DE/Konzernumfeld weiterhin erwartet (Details in E).

---

## B) Was Hiring Manager / Tech Leads brauchen (priorisiert)

**Grundbefund: Für Techniker ist GitHub das entscheidende Signal, die Seite ist der Hub/Tie-Breaker.** Reihenfolge laut Standout (Recruiting-Firma, Vendor): LinkedIn → Lebenslauf → GitHub (30 s–3 min) → Personal Site (<30 s); „a two-year contribution graph with thoughtful PR descriptions outscores any project hero shot on a personal site“ ([standout.work](https://standout.work/blog/portfolio-for-senior-software-engineers)). techinterview.org: „Most reviewers don't click through to backend engineers' personal sites“; für Frontend/Product Engineers lohnt die Seite, weil sie selbst Arbeitsprobe ist. Leonard ist Fullstack mit Angular – die Seite *ist* eine Angular/Frontend-Arbeitsprobe, das rechtfertigt sie.

**Prioritätenliste Tech:**

1. **3–5 echte, erklärte Projekte – nicht Tutorials.** Pro Projekt: Problem, Eigenanteil, Stack, Entscheidungen/Tradeoffs, Ergebnis, Link Demo + Repo. HN Traster: häufigster Fehler „Not being specific about what the 'team' achieved and what you contributed“; HN Jemaclus (Hiring Manager, mehrere Startups): „I'd hope to see some code in a public repo“, „maybe a full project with links to demos or live sites“ ([HN 21191906](https://news.ycombinator.com/item?id=21191906)). cvlotse.de (DE): Eigenleistung von Teamreview/Veröffentlichung trennen ([cvlotse](https://cvlotse.de/ratgeber/bewerbung-softwareentwickler)). techinterview.org: „Interviewers will drill into whichever part you claim was hard“; „A project they cannot run gets read as a project that does not work“ ([techinterview AI-Portfolio](https://www.techinterview.org/post/3233475399/ai-portfolio-built-with-ai-2026-interviews/)).
2. **README-Qualität.** HN zepolen (Recruiter): „I've found the README is one of the best indicators of the level of detail a person applies to their work“; gargarplex (Recruiter+Programmierer): unbearbeitete Framework-Default-READMEs sind ein Red Flag ([HN 19413348](https://news.ycombinator.com/item?id=19413348)). GitHub selbst: „Expect that they will only look at your projects for a couple minutes“ ([GitHub Docs](https://docs.github.com/en/account-and-profile/tutorials/using-your-github-profile-to-enhance-your-resume)).
3. **Commit-Historie als Arbeitsweise-Signal.** HN makecheck: „extremely offputting to see some obvious code dump, with like 2 commits in total“; prakhunov: „I actually look into their commit history and see if they are using the various best practices of git“ ([HN 16224401](https://news.ycombinator.com/item?id=16224401), [HN 19413348](https://news.ycombinator.com/item?id=19413348)).
4. **Tests, CI, Deployment-Config.** HN botskonet/scardine: „use CI“, „automated testing“, „test coverage“; Prepto (Vendor, aber mit HN-Quellen): „CI presence is the strongest signal of lived production experience“ ([prepto.tech](https://prepto.tech/blog/github-portfolio-senior-backend-engineer-2026)). → Homelab (Docker, Postgres, Backups, Healthchecks, Uptime Kuma) ist genau dieses Signal und sollte als „Betrieb“-Sektion sichtbar sein, nicht als Hobby-Fußnote.
5. **Sauberer Code, nicht Boilerplate.** HN wccrawford (HM): „Clean code that's more than just boilerplate“; andrewingram: „The main thing I don't want to see is a bunch of repos where you're just taking a library/framework for a spin.“
6. **Stack mit Erfahrungsjahren, nicht Skill-Balken.** HN Traster: „Languages, technologies used, with relative experience in each“ und Warnung: „you're damaging your chances if you come across as too much of an all rounder.“ Skill-Prozentbalken werden mehrfach als Negativsignal genannt ([techinterview](https://www.techinterview.org/post/3233474627/personal-website-portfolio-engineers/), [Profy-Nachbesprechung](https://soychristian.pages.dev/contents/developer-portfolio-guide)).
7. **Persönlichkeit / Kommunikation.** HN Jemaclus: „Do I want to sit next to you for the next six months?“ … „don't be a robot“; HN ratedgene: Blog/Site, die Interessen artikuliert → „I add points to them being a communicator“ ([HN 41656015](https://news.ycombinator.com/item?id=41656015)). Capgemini DE: persönliche Interessen sind „gute Gesprächsstarter“ ([Capgemini](https://www.capgemini.com/de-de/insights/blog/bewerben-mit-github/)). → Schachverein-Webseite ist hier Gold: echtes Produkt mit echten Nutzern, Betrieb, Admin-API, SEO.
8. **Aktualität der gepinnten Repos.** Capgemini: für Senior-Bewerber keine veralteten Projekte pinnen; Beiträge aus den letzten 12 Monaten; max. 6 Pins. Prepto: „A todo app from a 7-YOE engineer is a negative signal“; „Pinned repos last touched two or more years ago“ = ignoriert.
9. **Der Code der Seite selbst wird angeschaut – und bewertet.** dev.to-Kommentar Jon Randy (Hirer): hat Kandidaten abgelehnt wegen Portfolio-Seiten, die „overengineered, using inappropriate technology for such a simple site, poor code etc.“ waren ([dev.to/profydev](https://dev.to/profydev/this-survey-among-60-hiring-managers-reveals-don-t-waste-your-time-on-a-react-portfolio-website-17ge)). → Wenn der Code public ist: Tech-Wahl im README begründen, Umfang proportional halten.

---

## C) Red Flags / was schadet (mit Zitaten)

| Red Flag | Quelle (Hirer-Aussage = H, Ratgeber = R) |
|---|---|
| **Leere / verlassene Seite.** „with an empty template and a single entry dated 4 years ago saying 'TODO', then it will count against you“ | flpm (H), [HN 41656015](https://news.ycombinator.com/item?id=41656015) |
| **Schlechte Seite schlimmer als keine.** „Besser keine Website, als eine schlechte“ / „Eine schwache oder lieblos zusammengeklickte Seite schadet mehr, als sie nützt“ | [StepStone](https://www.stepstone.de/magazin/artikel/bewerbungshomepage) (R), [berufe.eu](https://www.berufe.eu/die-bewerbungshomepage-top-oder-flop/) (R) |
| **Effekte, Animationen, große Medien.** „Zu viele Effekte, Animationen oder Farben wirken unseriös“; Videos/große Medien → Ladezeit, laufen evtl. nicht auf Firmenrechnern | [Karrierebibel](https://karrierebibel.de/bewerbungshomepage/), StepStone (R); techinterview: „Heavy animation, parallax, and scroll-jacking“ |
| **Defekte Links, Ladezeit, Rechtschreibfehler** „ruinieren den Eindruck“; Rechtschreibfehler 64 % No-Go | Karrierebibel (R), [Haufe](https://www.haufe.de/personal/hr-management/recruiting-auf-was-personaler-im-lebenslauf-achten_80_475630.html) (Studie) |
| **Privates: Familie, Religion, Politik** gehört nicht auf die Seite | Karrierebibel, StepStone (R) |
| **Superlative/Floskeln** („Experte“, „Guru“), Mission Statements („I believe in the power of technology…“), lange Textblöcke | Karrierebibel (R), techinterview (R) |
| **Tutorial-Klone, unmodifizierte Forks, leere Repos.** „Lots of unmodified forks or tutorials“ = „suspicious“; „100% clones of various tutorials“ senken Vertrauen | vorpalhex (H) [HN 16224401](https://news.ycombinator.com/item?id=16224401), prakhunov (H) [HN 19413348](https://news.ycombinator.com/item?id=19413348) |
| **Ein „initial commit“ mit fertiger App** – „reads as 'generated somewhere else and dropped in here'“ | [SlateGit](https://slategit.com/blog/ai-generated-github-projects-recruiters-suspicious-2026) (Vendor) |
| **Leeres GitHub verlinken.** „don't link to an empty GitHub page on your resume“; „I penalize people slightly for an empty GitHub if they put it on their resume“ | ncallaway, travisjungroth (H) [HN 19413348](https://news.ycombinator.com/item?id=19413348) |
| **Over-Engineering der Seite selbst** (s. B9) | Jon Randy (H) |
| **Alles-Könner-Liste, vager Team-Credit** | Traster (H) [HN 21191906](https://news.ycombinator.com/item?id=21191906) |
| **Skill-Balken / Tech-Wall** | techinterview, standout (R) |
| **Kontaktformular statt E-Mail.** „skip web forms because recruiters don't use them“ | techinterview (R); dev.to-Checklist (R) |
| **Baukasten-Optik.** „Eine offensichtliche Baukasten-Seite von der Stange wirkt selten beeindruckend“ | berufe.eu (R) |
| **Anstößige Projektnamen.** Agentur fand „F*ckAMP“ problematisch | btasker (Kandidat, Recruiter-Feedback) HN 41656015 |
| **Offensichtlich KI-generierter Text.** 41 % der deutschen HR-Entscheider sortieren erkennbar KI-geschriebene Bewerbungen sofort aus | [Indeed/Censuswide 2026](https://de.indeed.com/news/releases/ki-im-recruiting-wie-hr-abteilungen-auf-steigende-bewerbungszahlen-reagieren) (Studie) |
| **Veraltete Inhalte.** „A portfolio that hasn't moved in a year looks abandoned“; techinterview: „reads as abandoned“ | R |
| **Firmenspezifische Begrüßung** bei Mehrfachbewerbung | StepStone (R) |

---

## D) GitHub/Code-Erwartungen inkl. AI-assisted Coding

**Was geprüft wird (Konsens über HN-Hirer, Capgemini, Prepto, Underdog):**
- Profil-README mit Name, Fokus, Kontakt; echtes Foto, vernünftiger Handle (beager, HN).
- 3–6 gepinnte, nicht geforkte, dokumentierte Repos, zuletzt <12 Monate berührt (Capgemini: „Nutze kein leeres Profil“; „Nicht die Anzahl der bearbeiteten Codes zählt, sondern die Qualität deiner Mitarbeit“).
- README pro Repo: Problem, Screenshots, How-to-run (ein Befehl), Architektur-Entscheidungen, bekannte Grenzen. Prepto: „A README or design doc that takes a position and names trade-offs.“
- Inkrementelle Commits mit aussagekräftigen Messages; Tests; CI (GitHub Actions); Dockerfile/Compose.
- Interaktion mit anderen (Issues/PRs): scardine bestraft „unprofessional, disrespectful or toxic behavior while interacting with the community“.
- Gegenstimmen (nicht ignorieren): davb: „I never look at them, at all … fairly poor signal“; Edd314159: „I will never, ever penalise a candidate for an empty GitHub profile“; dominotw: „Everyone asks for github profile but no one actually has the time to analyze it.“ → GitHub ist Bonus-Signal für die, die hinschauen; es darf keine Zeit von Lebenslauf/Projektbeschreibung abziehen.

**AI-assisted Code (2025/2026):**
- Hiring-Manager-Stimme (Annie Flippo, Data-Team-Leitung/CDO): Side Projects nach 2023 sind „no longer a reliable source of the candidate's ingenuity“, „We have to believe they're most likely done by AI vibe coding“; Take-Homes abgeschafft, stattdessen Live-Coding + mehr Runden. ([acflippo.substack](https://acflippo.substack.com/p/ai-broke-recruiting))
- Robert Half (März 2026, n>2.000 US-Hiring-Manager): 65 % sagen, KI-gestützte Bewerbungen erschweren Skill-Verifikation; Caveat im Release: „Not all AI-generated applications are inaccurate or misleading.“ ([Robert Half](https://press.roberthalf.com/2026-03-10-Robert-Half-survey-67-of-HR-leaders-report-AI-generated-applications-are-slowing-hiring))
- Stack Overflow 2025: ~51 % der Profis nutzen KI täglich, aber 46 % misstrauen der Genauigkeit, 66 % „almost right, but not quite“. ([the-decoder](https://the-decoder.com/developers-rely-on-ai-tools-more-than-ever-but-trust-is-slipping/)) → KI-Nutzung ist Normalität; das Problem ist fehlende Kontrolle, nicht die Nutzung.
- Deutschland: Statista/Haufe Jan 2026 (n=500 Bewerbende): 58 % nutzen KI; 60 % meinen, der Einsatz solle unerkennbar bleiben; nur 32 % glauben, Arbeitgeber befürworten es ([Haufe](https://www.haufe.de/personal/hr-management/ki-im-bewerbungsprozess-ki-einsatz-durch-bewerber_80_676886.html)). StepStone AT 2025: jede zweite Bewerbung mit KI; 80 % der Recruiter bewerten eingehende Qualität als durchschnittlich oder schlechter, 72 % loben aber professionelleren Look ([StepStone AT](https://www.stepstone.at/Ueber-StepStone/pressebereich/studie-jede-zweite-bewerbung-mit-hilfe-von-ki-erstellt-recruiterinnen-fehlt-individualitat/)). dpa-Umfrage DAX April 2024: keines von 16 Unternehmen nannte KI-Nutzung als Ausschlussgrund ([skill-sprinters Zusammenfassung](https://skill-sprinters.de/blog/news/bitkom-umfrage-ki-bewerbungsprozess-2026/)).
- Transparenz ja/nein: SlateGit (Vendor, aber plausibel): „Recruiters are not anti-AI. They are anti-'I cannot explain what I claim to have built.'“ und „Minimizing significant AI use in an interview is worse than admitting it and explaining your role in the build.“ Empfehlung: README-Hinweis, der die KI-Anteile konkret benennt. techinterview.org: dokumentiere Modell/Tooling, Architektur und „Why AI was the right tool (not always)“; Interviewer bohren bei allem nach, was als „hard“ deklariert ist. Learnist/Argoland (R): eine Zeile pro Projekt, Aufgabe + menschliche Prüfung nennen.
- Negativ-Muster: „got Cursor'd“ = 30–50 frische Repos mit je einem Commit; generisches KI-README-Boilerplate; unbearbeitete Erklär-Kommentare für Basissyntax (SlateGit, Prepto).

**Ableitung für Leonard:** KI-Nutzung offen, aber *spezifisch* deklarieren (Repo-README + Projektkarte: „Scaffolding/Boilerplate und Tests teilweise mit Claude Code; Architektur, Datenmodell, Deployment und Review durch mich“). Historie nicht squashen. Nicht deklarieren: generische KI-Prosa im Lebenslauf/Anschreiben – das ist das, was die 41 % aussortieren. Im Gespräch: jedes gepinnte Projekt 5 Minuten frei erklären können.

---

## E) Deutschland-spezifisch

**Foto**
- Rechtlich nicht Pflicht (AGG); Fehlen darf nicht nachteilig gewertet werden ([bewerbungsgenius](https://bewerbungsgenius.de/ratgeber/bewerbungsfoto-ja-oder-nein-was-in-deutschland-oesterreich-und-der-schweiz-wirklich-gilt), Bundesagentur via abi.de).
- Praxis: StepStone AT 2018: Mehrheit der Entscheider lehnt Bewerbungen ohne Foto weiterhin ab; 42 %: schlechtes Foto = schlechte Bewerbung; kritisiert werden Selfies (39 %), Urlaubsfotos (36 %) ([Haufe](https://www.haufe.de/personal/hr-management/recruiting-auf-was-personaler-im-lebenslauf-achten_80_475630.html)). Tech/Startups/International: weniger erwartet; Banken/Versicherungen/Industrie: erwartet. Zielarbeitgeber (Atruvia = Genossenschaftsbanken-IT, Bechtle, Würth IT, dm) sind eher traditionell → **professionelles Foto rein** (Startseite + Lebenslauf), kein Selfie.

**Sprache**
- Deutsche Anzeige → deutscher Lebenslauf, englische Fachbegriffe ok; Englisch als GER-Niveau angeben (z.B. C1), nicht „fließend“ ([novoresume](https://novoresume.com/de/ressourcen/lebenslauf-beispiele/softwareentwickler), [StepStone](https://www.stepstone.de/magazin/artikel/lebenslauf-auf-englisch)). Konzerne erwarten oft Deutsch C1/C2 ([owlapply](https://owlapply.com/de/lebenslauf-beispiele/softwareentwickler)). → Seite **Deutsch als Default**, optional EN-Toggle (inovex/Chrono24 international); Code/README/Commits auf Englisch (Branchenstandard).

**Lebenslauf-Form**
- Tabellarisch, antichronologisch, 1–2 Seiten (bei 5 Jahren: 2), PDF ([Karrierebibel](https://karrierebibel.de/lebenslauf-chronologisch/), [Randstad](https://www.randstad.de/karriere/bewerbungsratgeber/lebenslauf/)). dmTECH fordert „klare, antichronologische Struktur mit kurzen Bullet Points“ ([dmTECH Bewerbungstipps](https://www.dmtech.tech/de/bewerbungstipps/)).

**Anschreiben**
- Gisma 2024 (n=68, davon 40 DAX): 56 verlangen Anschreiben, 12 optional ([IQB](https://iqb.de/karrieremagazin/bewerbung/darauf-achten-personaler-wirklich-der-lebenslauf/)). dmTECH: „Optional freuen wir uns über Dein Anschreiben“; nur Lebenslauf Pflicht; Upload-Limit 8 MB, Formate pdf/doc/docx/jpeg/png. Indeed 2026: 54 % sagen, Anschreiben haben Aussagekraft verloren; 46 % wünschen kürzere Formate ohne Anschreiben. → Kurzes, selbst geschriebenes Anschreiben bereithalten; nicht auf der Seite, sondern pro Bewerbung. Die Seite ersetzt es nicht („eine Bewerbungshomepage kann niemals die eigentliche Bewerbung ersetzen“, StepStone).

**Zeugnisse / Anlagen**
- Gisma 2024: 65 von 68 Unternehmen würden auf Zeugnisse verzichten; Personaler gewichten Zeugnisse mit 10 %. Konvention bleibt: Arbeitszeugnisse der letzten 5–10 Jahre, max. ~3; Anlagen ≤10 Seiten; Gesamtbewerbung als **eine PDF** bei E-Mail, in Portalen getrennte Felder; Größe: Vorgabe der Anzeige, sonst ≤5 MB Richtwert, 10 MB Obergrenze ([lebenslauf.de](https://www.lebenslauf.de/ratgeber/anhaenge/), [korrektur.de](https://korrektur.de/bewerbungsunterlagen-als-pdf-zusammenstellen), [Karrierebibel Anlagen](https://karrierebibel.de/bewerbung-anlagenverzeichnis/)). → Auf der Seite: Lebenslauf frei; Zeugnisse als ein PDF, aber **nicht öffentlich** (DSGVO: Zeugnisse enthalten Arbeitgeber-/Personendaten; studiwork und Karrierebibel empfehlen Passwortschutz oder „auf Anfrage“). ZIP wird nirgends empfohlen – PDF-Bündel ist Standard.

**Impressum (§ 5 DDG, seit 14.05.2024 statt TMG)**
- Rein private/familiäre Seiten ausgenommen; e-recht24: „Jede Webseite, die nicht rein privat ist, benötigt ein Impressum“ ([e-recht24](https://www.e-recht24.de/artikel/datenschutz/209.html)). studiwork: Bewerbungshomepage braucht „de jure eigentlich kein Impressum“ ([studiwork](https://www.studiwork.com/job-und-karriere/bewerbungshomepage-eigenwerbung-per-online-visitenkarte/)); berufe.eu: richtet sich an Arbeitgeber, „im Zweifel ist ein Impressum daher die sichere Wahl“; StepStone-FAQ und Karrierebibel: Impressum nötig (Name, Adresse, E-Mail; Telefon laut e-recht24 nicht zwingend bei anderer schneller Kontaktmöglichkeit). → **Impressum anlegen.** Kostet nichts, Personaler wollen die Anschrift ohnehin, Abmahnrisiko entfällt.

**Datenschutz**
- Datenschutzerklärung praktisch immer nötig, auch privat, weil Server-Logs/IP personenbezogen sind: „Auch Privatpersonen brauchen für ihre Webseite … in der Regel eine Datenschutzerklärung“; max. 2 Klicks von jeder Seite erreichbar ([e-recht24 private Website](https://www.e-recht24.de/datenschutz/13237-datenschutzerklaerung-private-website.html)); berufe.eu: Art. 13 DSGVO „fast immer“.
- Kontaktformular: Datenschutzhinweis am Formular + Link; Pflicht-Checkbox für bloße Beantwortung nicht nötig ([fabian-reiter](https://fabian-reiter.com/blog/dsgvo-konformes-kontaktformular-2026/), [nevik](https://nevik.de/blog/posts/kontaktformular-dsgvo-pflichtangaben/)). Empfehlung: **kein Formular**, nur mailto (siehe C).
- Analytics: Google Analytics nur mit Einwilligung ([IHK Regensburg](https://www.ihk.de/regensburg/fachthemen/recht/online-recht-und-datenschutz/eu-datenschutzgrundverordnung/anforderungen-an-websites-nach-der-ds-gvo-4158848)). → Entweder gar nicht oder cookieless/self-hosted (Umami/Plausible auf dem Homelab) ohne Banner; trotzdem in der Erklärung nennen.
- Externe Fonts/CDNs/Karten → Drittlandtransfer → Einwilligung oder self-host ([datenschutz.org](https://www.datenschutz.org/datenschutzerklaerung-website/)). Cloudflare/GitHub Pages als Host → in der Erklärung mit Rechtsgrundlage (Art. 6 Abs. 1 f, SCC) nennen ([e-recht24 Cloudflare](https://www.e-recht24.de/dsg/12626-cloudflare.html)).
- Keine Cookies → kein Banner. Das ist auch ein Qualitätssignal.
- BFSG (seit 28.06.2025): rein private Seiten nicht erfasst ([7P-Checkliste](https://7p-group.com/wp-content/uploads/2025/10/Barrierefreiheits-Checkliste_V02.pdf)); Barrierefreiheit trotzdem als Handwerkssignal (Kontrast, Tastatur, alt-Texte) umsetzen – Profy-Nachbesprechung nennt „poor accessibility“ als Negativpunkt.

---

## F) ATS-taugliches PDF: konkrete Regeln

Evidenz ist überwiegend Vendor-Content (Jobscan, karriere-jet, bewerbung.net), aber konsistent; ein Primärtest (Jobscan mit Lever) zeigt, dass bei Spalten-Layout nur die Berufserfahrung erkannt wurde.

1. **Einspaltig.** Parser lesen linear; mehrspaltig → „text-layer scrambling“ ([Jobscan](https://www.jobscan.co/blog/resume-tables-columns-ats/)); DE: „Mehrspaltige Layouts, Tabellen und Icons … stören diese Logik“ ([bewerbung.net](https://bewerbung.net/cv-lebenslauf-parsing)).
2. **Keine Tabellen, Textboxen, Icons, Grafiken, Skill-Balken.** Telefon-Icon statt Wort „Telefon“ bricht Parsing.
3. **Kontaktdaten im Fließtext, nicht in Kopf-/Fußzeile** ([resumly](https://www.resumly.ai/blog/formatting-resume-pdfs-best-practices-to-avoid-ats-errors)).
4. **Links: volle URL als sichtbarer Text + klickbar**, z.B. `github.com/leonard…`, `https://…`; nicht „GitHub | LinkedIn | Portfolio“ in einer Zeile mit Pipes; keine Icon-Links ([atsverification](https://atsverification.com/blog/do-ats-read-hyperlinks-resume/), [hireflow](https://hireflow.net/blog/how-to-include-links-in-an-ats-resume-safely)).
5. **Standard-Überschriften**: Berufserfahrung, Ausbildung, Kenntnisse, Sprachen, Projekte. Keine kreativen Labels.
6. **Keywords wörtlich aus der Anzeige**, Abkürzungen ausschreiben (z.B. „ASP.NET Core“ und „C#“, „Angular“ mit Version; „IHK Fachinformatiker für Anwendungsentwicklung“) ([karriere-jet](https://www.karriere-jet.de/content/cv-parsing/)).
7. **Standardfonts** (Arial/Calibri/Helvetica/Georgia), 10–12 pt, keine Ligatur-Spielereien.
8. **Echtes Text-PDF**, kein „Drucken als Bild“; Test: Text markieren/kopieren → muss in Lesereihenfolge herauskommen. PDF wird von deutschen Parsern akzeptiert ([bewerbung.co](https://bewerbung.co/cv-parsing)); DOCX nur, wenn Portal es verlangt.
9. **Pro Station:** Firma, Ort, Zeitraum (MM/JJJJ–MM/JJJJ), Titel, 3–5 Bullets mit Stack + Ergebnis.
10. **Dateiname** `Lebenslauf_Vorname_Nachname.pdf`, <5 MB (dmTECH-Limit 8 MB), 2 Seiten.
11. Für den Menschen zusätzlich dieselbe Struktur hübsch? Nein – **ein** Dokument, das beides kann: klares einspaltiges Layout mit dezenter Typografie. Zwei Versionen erzeugen Inkonsistenzrisiko (Karrierebibel: Aktualität/Konsistenz).

Generierung aus der Webseite: gleiche Datenquelle (JSON/Markdown) → HTML-CV-Seite → Print-CSS → PDF. Vermeidet Drift zwischen Seite und PDF.

---

## G) Empfohlene Seitenstruktur (aus den Quellen abgeleitet)

**Above the fold (ohne Scrollen, Desktop + Phone):**
- Foto (professionell, klein), Name
- Eine Zeile: „Fullstack-Entwickler · C#/.NET & Angular · 5 Jahre · Raum Stuttgart/Karlsruhe“
- Eine Zeile Verfügbarkeit/Suchziel (z.B. „offen für Festanstellung, hybrid“)
- Zwei Buttons: **Lebenslauf (PDF)** · **E-Mail** ; daneben Textlinks GitHub, LinkedIn
- Kein Hero-Video, keine Intro-Animation (Karrierebibel, StepStone, techinterview)

**Sektionen in dieser Reihenfolge (Einseiter mit Ankern, flache Navigation):**
1. **Kurzprofil** (3–5 Sätze): was du baust, womit, in welchem Kontext; IHK-Abschluss; Sprachen mit GER-Niveau. Keine Floskeln.
2. **Projekte** (3–5 Karten, beste zuerst; sk-sulzfeld.de als Leitprojekt): Problem → Eigenanteil → Stack → 2–3 Entscheidungen/Tradeoffs → Status/Jahr → Links Demo + Repo → ggf. eine Zeile „KI-Einsatz: …“. Rest in einer kompakten Liste.
3. **Berufserfahrung** tabellarisch antichronologisch (gleicher Inhalt wie PDF).
4. **Kenntnisse** gruppiert (Backend/Frontend/Datenbank/Infra), mit Jahren oder „produktiv seit“, keine Balken.
5. **Betrieb & Infrastruktur** (Homelab): Docker Compose, Postgres, Backups mit Healthchecks, Monitoring, CI/CD – als Produktionskompetenz formuliert, 1 Screenshot/Diagramm max.
6. **Schreiben** (optional, nur wenn ≥3 gute Artikel; „3 strong posts beat 30 weak ones“).
7. **Kontakt**: E-Mail (mailto), Telefon, Ort, LinkedIn. Kein Formular.
8. **Downloads**: Lebenslauf DE (PDF), optional Lebenslauf EN, optional Kurzprofil-One-Pager; Zeugnisse „auf Anfrage“ oder passwortgeschütztes PDF-Bündel (Passwort im Anschreiben).
9. **Footer**: Impressum, Datenschutz, „Quellcode dieser Seite auf GitHub“, Stand-Datum.

**Technik-Vorgaben aus den Quellen:** statisch/SSR, LCP <2,5 s, Seitengewicht klein (Standout: <200 KB als Zielwert – ambitioniert, aber Richtung stimmt), responsive, self-hosted Fonts, keine Cookies, `prefers-color-scheme` respektieren (ein auffälliger Toggle wird von Standout als Deko-Negativ gelistet – Streitpunkt, siehe H), Druck-CSS, Domain mit eigenem Namen (StepStone/studiwork), README des Site-Repos erklärt bewusst schlanke Technikwahl (gegen „over-engineered“-Vorwurf). Vierteljährliches Pflege-Ritual (techinterview: 15 min/Quartal, sonst Seite lassen).

---

## H) Offene Streitpunkte

1. **Schaut überhaupt jemand?** Profy 2021: Mehrheit schaut, Wirkung ~0. HN flpm: „I always look“; bschmidt1: FE-Devs ohne Site werden gar nicht interviewt. Dagegen blueappconfig: alle 3 Arbeitgeber „didn't look“; Blind-Recruiter klickt nie; techinterview: Backend-Reviewer klicken selten. Aufgelöst für Leonard: Fullstack mit Angular + echte Produkte → Seite lohnt, aber nur als schneller Hub; Hauptinvestition in Lebenslauf + 3 Repos.
2. **Foto:** rechtlich frei, Praxis in DE-Konzernen pro Foto; Tech/International contra. Für die genannte Zielliste: Foto.
3. **Dark-Mode-Toggle:** Standout (Vendor) nennt ihn als Deko-Negativ; keine andere Quelle stört sich daran. Kompromiss: System-Präferenz automatisch, Toggle unauffällig oder weglassen.
4. **Passwortschutz:** Karrierebibel empfiehlt ihn für Datenschutz; techinterview/Dribbble-Hirer warnen vor Zugangshürden. Kompromiss: Seite offen, nur Zeugnisse geschützt.
5. **Kontaktformular vs. E-Mail:** deutsche Ratgeber neutral (Formular ok mit Hinweis); englische Hirer-nahe Quellen: Recruiter nutzen Formulare nicht. → E-Mail.
6. **Impressum bei privater Bewerbungsseite:** studiwork „de jure nicht nötig“ vs. StepStone/Karrierebibel „Pflicht“ vs. berufe.eu „im Zweifel ja“. Keine Rechtsprechung zu Bewerbungsseiten gefunden. → Anlegen.
7. **PDF vs. DOCX:** US-Vendoren teils pro DOCX; deutsche Quellen: PDF ist Standard und wird geparst. → PDF.
8. **KI-Transparenz:** SlateGit/techinterview/Learnist: offenlegen; deutsche Bewerbende verstecken es mehrheitlich (60 %); Indeed: 41 % sortieren *erkennbar* KI-geschriebene Bewerbungen aus. Differenzierung: Code-Projekte mit konkretem Hinweis = Plus; KI-Prosa in Anschreiben/Lebenslauf = Minus. Keine Studie misst die Wirkung eines README-Disclaimers.
9. **Anschreiben:** Gisma: 56/68 verlangen es; Indeed 2026: 54 % halten es für wertlos; dmTECH optional. → Kurz, selbst geschrieben, pro Bewerbung.
10. **Commit-Historie squashen?** SlateGit: „The 'ugly' commits are the proof“; andere Guides empfehlen saubere Historie. HN edw519: ein „perfect repository“ wirkt gefaked. → Nicht squashen, aber Messages sauber halten.
11. **Zeugnisse:** 65/68 Firmen würden verzichten, 10 % Gewicht – trotzdem fragen Portale danach. → Bereithalten, nicht ins Schaufenster.
12. **Zahlen zur Sichtdauer** (6 s / 7,4 s / 43 s / 60–90 s / ≤1 min) widersprechen sich methodisch; alle sagen: <1 Minute für die erste Entscheidung.

---

## I) Quellenliste

**Hirer-Aussagen (Primär, HN/Blind/Foren)**
- https://news.ycombinator.com/item?id=41656015 – Ask HN: Did your personal website help you get hired?
- https://news.ycombinator.com/item?id=21191906 – Ask HN: Hiring managers, describe the best developer portfolio
- https://news.ycombinator.com/item?id=19413348 – Ask HN: What do recruiters look for in a GitHub profile?
- https://news.ycombinator.com/item?id=16224401 – Ask HN: What do you look for in an applicant's GitHub?
- https://www.teamblind.com/post/do-you-look-at-candidates-portfolio-zlbuydfd
- https://www.teamblind.com/post/do-recruiters-or-hiring-managers-check-github-sfrfrpxj
- https://www.java-forum.org/thema/bewerben-nach-dem-studium-auf-eigene-projekte-referenzieren.185477/
- https://dev.to/profydev/this-survey-among-60-hiring-managers-reveals-don-t-waste-your-time-on-a-react-portfolio-website-17ge
- https://acflippo.substack.com/p/ai-broke-recruiting

**Studien / Umfragen**
- https://www.haufe.de/personal/hr-management/recruiting-auf-was-personaler-im-lebenslauf-achten_80_475630.html (StepStone AT/MindTake Eye-Tracking)
- https://iqb.de/karrieremagazin/bewerbung/darauf-achten-personaler-wirklich-der-lebenslauf/ (Gisma 2024, StepStone)
- https://de.indeed.com/news/releases/ki-im-recruiting-wie-hr-abteilungen-auf-steigende-bewerbungszahlen-reagieren (Indeed/Censuswide 2026)
- https://press.roberthalf.com/2026-03-10-Robert-Half-survey-67-of-HR-leaders-report-AI-generated-applications-are-slowing-hiring
- https://www.haufe.de/personal/hr-management/ki-im-bewerbungsprozess-ki-einsatz-durch-bewerber_80_676886.html (Statista 2026)
- https://www.stepstone.at/Ueber-StepStone/pressebereich/studie-jede-zweite-bewerbung-mit-hilfe-von-ki-erstellt-recruiterinnen-fehlt-individualitat/
- https://the-decoder.com/developers-rely-on-ai-tools-more-than-ever-but-trust-is-slipping/ (Stack Overflow 2025)
- https://www.recruiter.com/i/theladders-reveals-research-reporting-that-resumes-spend-six-seconds-with-a-recruiter/
- https://skill-sprinters.de/blog/news/bitkom-umfrage-ki-bewerbungsprozess-2026/ (Bitkom, dpa-DAX-Umfrage)

**Deutsche Ratgeber / Arbeitgeberseiten**
- https://www.stepstone.de/magazin/artikel/bewerbungshomepage
- https://karrierebibel.de/bewerbungshomepage/
- https://www.berufe.eu/die-bewerbungshomepage-top-oder-flop/
- https://www.studiwork.com/job-und-karriere/bewerbungshomepage-eigenwerbung-per-online-visitenkarte/
- https://www.capgemini.com/de-de/insights/blog/bewerben-mit-github/
- https://cvlotse.de/ratgeber/bewerbung-softwareentwickler
- https://www.dmtech.tech/de/bewerbungstipps/
- https://www.inovex.de/de/karriere/einstieg-fuer-studierende/
- https://www.stepstone.de/magazin/artikel/lebenslauf-auf-englisch
- https://novoresume.com/de/ressourcen/lebenslauf-beispiele/softwareentwickler
- https://owlapply.com/de/lebenslauf-beispiele/softwareentwickler
- https://karrierebibel.de/lebenslauf-chronologisch/
- https://www.lebenslauf.de/ratgeber/anhaenge/
- https://korrektur.de/bewerbungsunterlagen-als-pdf-zusammenstellen
- https://karrierebibel.de/bewerbung-anlagenverzeichnis/
- https://bewerbungsgenius.de/ratgeber/bewerbungsfoto-ja-oder-nein-was-in-deutschland-oesterreich-und-der-schweiz-wirklich-gilt
- https://www.profiling-institut.de/bewerbung/bewerbungsfoto/

**Recht (Impressum, DSGVO, BFSG)**
- https://www.e-recht24.de/artikel/datenschutz/209.html
- https://www.e-recht24.de/datenschutz/13237-datenschutzerklaerung-private-website.html
- https://www.it-recht-kanzlei.de/website-impressumspflicht-digitale-dienste-gesetz.html
- https://www.ihk.de/regensburg/fachthemen/recht/online-recht-und-datenschutz/eu-datenschutzgrundverordnung/anforderungen-an-websites-nach-der-ds-gvo-4158848
- https://fabian-reiter.com/blog/dsgvo-konformes-kontaktformular-2026/
- https://nevik.de/blog/posts/kontaktformular-dsgvo-pflichtangaben/
- https://www.datenschutz.org/datenschutzerklaerung-website/
- https://www.e-recht24.de/dsg/12626-cloudflare.html
- https://7p-group.com/wp-content/uploads/2025/10/Barrierefreiheits-Checkliste_V02.pdf

**ATS / CV-Parsing**
- https://www.jobscan.co/blog/resume-tables-columns-ats/
- https://bewerbung.net/cv-lebenslauf-parsing
- https://www.karriere-jet.de/content/cv-parsing/
- https://bewerbung.co/cv-parsing
- https://atsverification.com/blog/do-ats-read-hyperlinks-resume/
- https://hireflow.net/blog/how-to-include-links-in-an-ats-resume-safely
- https://www.resumly.ai/blog/formatting-resume-pdfs-best-practices-to-avoid-ats-errors

**Portfolio-/GitHub-Guides (Vendor, mit Vorsicht)**
- https://www.techinterview.org/post/3233474627/personal-website-portfolio-engineers/
- https://www.techinterview.org/post/3233475399/ai-portfolio-built-with-ai-2026-interviews/
- https://standout.work/blog/portfolio-for-senior-software-engineers
- https://prepto.tech/blog/github-portfolio-senior-backend-engineer-2026
- https://slategit.com/blog/ai-generated-github-projects-recruiters-suspicious-2026
- https://dev.to/alx094/what-to-include-in-a-developer-portfolio-a-recruiter-readable-checklist-1nj7
- https://dev.to/dev_tips/recruiters-arent-clicking-your-portfoliotheyre-looking-here-instead-2884
- https://docs.github.com/en/account-and-profile/tutorials/using-your-github-profile-to-enhance-your-resume
- https://myseera.com/blog/personal-website-for-job-seekers
- https://www.learnist.org/signs-ai-portfolio-project-wont-impress-hiring-managers/

**Kritische Einordnung:** Die stärksten Belege sind die HN-Hirer-Threads, die StepStone-Eye-Tracking-Studie, Gisma 2024, Indeed/Censuswide 2026 und die Arbeitgeberseite dmTECH. Prozentzahlen aus Vendor-Blogs („73 % laut Stack Overflow“, „84 %“) ließen sich nicht verifizieren und wurden bewusst nicht übernommen. Reddit-Threads konnten nicht gelesen werden; die dort kolportierte Haltung („I never click portfolios“, Hiring Manager via dev.to) deckt sich mit Blind/HN.
