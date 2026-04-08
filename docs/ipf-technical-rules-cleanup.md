# IPF Technical Rules 2026 — PDF Cleanup Notes

The file `ipf-technical-rules-2026.md` was converted from the [official IPF Technical Rules PDF](https://www.powerlifting.sport/fileadmin/ipf/data/rules/technical-rules/english/2026_IPF_Technical_Rulebook__effective_01_March_2026__v3.pdf). PDF extraction introduced systematic artifacts that required cleanup.

> **Important:** When making changes to `ipf-technical-rules-2026.md`, always update this file to reflect what was changed and how.

## Artifacts fixed

### Spaced-out characters

The PDF extractor inserted spaces between individual characters in numbers, abbreviations, and some text runs.

| Before | After |
|--------|-------|
| `1 9 years` | `19 years` |
| `5 3 . 0 kg` | `53.0 kg` |
| `0 1 . 0 3 . 2 0 2 6` | `01.03.2026` |
| `i . e .` | `i.e.` |
| `e . g .` | `e.g.` |

Some lines were fully character-spaced, where every character was separated by a space and words by double spaces:

```
6 6 . 0  k g  C l a s s  f r o m  5 9 . 0 1  k g  u p  t o  6 6 . 0  k g
→ 66.0 kg Class from 59.01 kg up to 66.0 kg
```

**How:** Python script using regex. Detected fully char-spaced lines by ratio of single-char-space-char sequences, collapsed them by splitting on double spaces (word boundaries) and removing single spaces (intra-character). For normal lines, fixed `(\d) \. (\d)` and `(\d) (\d)` patterns iteratively.

### Numbered lists

List markers had spaces inserted by the extractor.

| Before | After |
|--------|-------|
| `1 ) The lifter...` | `1) The lifter...` |
| `( a ) The bar...` | `(a) The bar...` |
| `A . Squat` | `A. Squat` |

**How:** Regex on line-start `\d+ )` → `\d+)`, and `\( [a-p] \)` → `(\1)`. Parenthetical numbers like `( 8 )` and plurals like `( s )` were excluded.

### Spaced section headers

Uppercase words in headings were character-spaced.

| Before | After |
|--------|-------|
| `3 . 1 .  S U I T S` | `3.1. SUITS` |
| `2 . 6 .  B E N C H` | `2.6. BENCH` |

**How:** Same char-spacing collapse plus section number dot fix (`\d+\.\d+ .` → `\d+.\d+.`).

### Page headers and page numbers

Every page of the PDF had a date and section name header, plus a standalone page number. These appeared as:

```
01.03.2026
General rules of Powerlifting

21
```

**How:** Removed lines matching `^\s*01\.03\.2026\s*$` plus the following line if it matched a known section name. Removed standalone lines matching `^\d{1,2}\s*$`. One merged case (`Jury and technical committee 9. WORLD AND INTERNATIONAL RECORDS`) was split to preserve the heading.

### Mangled tables

The PDF extractor destroyed multi-column table layouts by merging column headers and interleaving cell content line by line.

**Disc weight tolerances** — column headers merged into `Face Value in KilosMaximumMinimum` and data rows were character-spaced with lost column boundaries. Reconstructed manually using known IPF values.

**Failure cards (RED/BLUE/YELLOW)** — three-column table (Squat / Bench Press / Deadlift) was flattened into sequential paragraphs with merged headers like `SQUATBENCH PRESSDEADLIFT`. Reconstructed as separate markdown tables per card color.

**Referee signals** — `LIFTCOMMENCEMENT COMPLETION` with merged cell content like `SQUATA visual signal...`. Reconstructed as a markdown table.

**Scoreboard template** — `LOTNAMENAT`, `SUBTOTALSUBTOTAL`. Reconstructed as markdown tables.

**Age categories** — `OPENfrom 1 January...` with category name merged into description. Reconstructed as a markdown table.

**Weight classes** — `MENWOMEN` header merged. Split into separate MEN and WOMEN tables.

### Extra spaces around punctuation

The PDF extractor inserted spaces before/after punctuation marks throughout the document (~1900 occurrences).

| Before | After |
|--------|-------|
| `the lifter ' s` | `the lifter's` |
| `body weight , age` | `body weight, age` |
| `will apply .` | `will apply.` |
| `as follows :` | `as follows:` |
| `lifter ; however` | `lifter; however` |
| `( Open , Sub-Junior )` | `(Open, Sub-Junior)` |
| `his / her` | `his/her` |
| `" he "` | `"he"` |
| `i.e. ,` | `i.e.,` |

**How:** Python script using regex. Fixed spaces before commas, periods, semicolons, colons, exclamation/question marks. Fixed spaces inside parentheses (`( word)` → `(word)`). Fixed spaces around slashes. Fixed spaces around right single quotes (possessives/contractions). Fixed spaces inside both straight and smart (U+201C/U+201D) double quotes.

### Spaced hyphens in compound words

The PDF extractor inserted spaces around hyphens in compound words.

| Before | After |
|--------|-------|
| `Sub - Junior` | `Sub-Junior` |
| `non - supportive` | `non-supportive` |
| `t - shirt` | `t-shirt` |
| `warm - up` | `warm-up` |
| `40 - 49` | `40-49` |

**How:** Python script with an explicit list of ~50 known compound words. Age ranges (`NN - NN`) fixed via regex.

### Mid-paragraph line breaks

The PDF extractor preserved original PDF line breaks, splitting paragraphs across multiple lines at the column width boundary (~80 chars). This made 1679-line file where most lines were fragments of longer paragraphs.

```
Before:
  The lifter's best valid attempt on each
  lift counts toward his competition total. If two or more lifters achieve the same total, the lighter
  lifter ranks above the heavier lifter.

After:
  The lifter's best valid attempt on each lift counts toward his competition total. If two or more lifters achieve the same total, the lighter lifter ranks above the heavier lifter.
```

**How:** Python script that joins continuation lines based on:
- Lines starting with a lowercase letter (strongest continuation signal)
- Lines following a previous line that ends with a hyphen (word split) or comma
- Lines following a previous line that ends without terminal punctuation (`.`, `!`, `?`, `:`)
- With exclusions for: section headers (ALL CAPS, numbered sections), list items (`1)`, `(a)`, `A.`, `- `), table rows, markdown elements, and standalone title lines (championship names, etc.)

Reduced the file from 1679 lines to ~712 lines.

### Manual post-fixes after line joining

The automated line joining incorrectly merged a few structural elements:
- **Table of Contents** — indented markdown list items (`   - 1.1 Age Categories`) were joined into a single line. Restored to a proper list.
- **Subtitle line** — `Effective: 01 March 2026` was joined to the preceding bold title. Restored line break.
- **Table preceding blank line** — the age categories table lost its blank-line separator from the preceding paragraph (`Other international events...`), breaking markdown table rendering. Restored blank line.

### Missing blank lines before section headers

After line joining, many section headers (numbered sections like `3.2. T-SHIRT`, ALL CAPS headers like `MEN & WOMEN`, bold labels like `**MEN**`) had no blank line separating them from the preceding paragraph content.

**How:** Python script that detects header lines (numbered sections, ALL CAPS lines, `**bold**` standalone lines, markdown `#` headings) and inserts a blank line before them when missing. Added 82 blank lines.

### Markdown heading formatting

Section headers were plain text (ALL CAPS or numbered), not markdown headings. Converted to proper heading hierarchy:

| Source format | Markdown | Example |
|---------------|----------|---------|
| `N. TITLE IN CAPS` | `## N. Title` | `## 3. Personal Equipment` |
| `N.M. TITLE IN CAPS` | `### N.M. Title` | `### 3.1. Suits` |
| `N.M.P. TITLE IN CAPS` | `#### N.M.P. Title` | `#### 4.1.1. Causes for Disqualification of a Squat` |

**How:** Python script that:
1. Split embedded top-level section headers (e.g., `...championships 2. EQUIPMENT AND SPECIFICATIONS Scales...`) onto their own lines — 8 sections were embedded mid-paragraph.
2. Removed stray page numbers merged with section headers (e.g., `13 3. PERSONAL EQUIPMENT` → `3. PERSONAL EQUIPMENT`).
3. Matched numbered section patterns and converted ALL CAPS titles to Title Case with appropriate `##`/`###`/`####` prefix.
4. Manual fixes for edge cases: title casing on `Cards/Paddles`, `Weighing In`, `Miscellaneous Rules (Loading Errors, ...)`. Added missing `## 1. General Rules of Powerlifting`.

Converted 48 section headers total.

### Displaced section content

The PDF extractor placed sub-section labels (e.g., `2.1. SCALES`, `2.2. PLATFORM`) in page margins/footers, so content was extracted before its heading. This left headings empty and their content sitting under the parent section or a preceding heading.

Affected sections: 2.1–2.3, 2.4–2.7, 3.4, 3.7, 3.12.

**How:** Python script with targeted find-and-replace to insert headings before their content and remove the orphaned empty headings. Manual fixes for:
- Removed duplicate `### 2.3. Bars and Discs` heading
- Restored `### 3.4. Briefs` heading
- Moved full `### 3.12. Inspection of Personal Equipment` content block (items (a)–(i)) from under 3.11 to under 3.12
- Joined split platform dimensions line (`2.5 m x 2.5 m ... 4.0 m x\n4.0 m`)
- Removed misplaced tie-breaking paragraph (from section 1) that appeared under section 2

### Displaced deadlift disqualification items and weighing-in content

The PDF extractor placed items 1–5 of Section 5 (Weighing In) between items 5 and 6–8 of Section 4.3.1 (Causes for Disqualification of a Deadlift), and put the Section 5 heading after all of them.

| Before | After |
|--------|-------|
| 4.3.1 items 1–5, then Weighing In items 1–5, then 4.3.1 items 6–8, then `## 5. Weighing In` heading | 4.3.1 items 1–8 together, then `## 5. Weighing In` heading with items 1–8 beneath it |

**How:** Manual reordering. Moved deadlift items 6–8 to follow item 5, moved weighing-in items 1–5 and amputee additions under the section heading.

### Orphaned line break in weighing-in section

"Platform Manager." was on its own line, separated from the sentence it belonged to (`A copy of this official document goes to the Jury, Speaker, and`). Joined onto the preceding line.

### Amputee weight additions table

The four amputee bodyweight additions were on a single line. Reformatted as a markdown table.

### Remaining stray page numbers

Three leftover page numbers that survived earlier cleanup:
- `13` between the scoreboard table and Section 3
- `36` merged with item 16 (`36 16. The Jury may utilize...`)
- `44` between Section 9 and Section 10

**How:** Manual removal.

### Missing tie-breaking paragraph

The paragraph after item 11 in General Rules describing tie-breaking procedure for teams ("In the case of a tie between two nations having the same number of first places...") was missing. Restored from the PDF.

### Failure card line breaks

`Failure no. 1 = red card Failure no. 2 = blue card Failure no. 3 = yellow card` was a single line. Split into three lines per the PDF.

### Remaining spaced hyphens and spacing artifacts

Fixed a few remaining spaced-character artifacts missed by earlier cleanup:
- `1 st`, `2 nd`, `3 rd` → `1st`, `2nd`, `3rd` (in items 4 and 11)
- `medal (s)` → `medal(s)`
- `t - shirt` → `t-shirt` (in coach section)
- Extra period after `age divisions. .` → `age divisions.`

### Speaker's card table

The Speaker's Card form was rendered as disconnected single-row tables with blank lines between every row, breaking markdown table rendering. The top section (personal info fields) had each field as a separate table. The attempts grid (SQUAT/BENCH PRESS/DEADLIFT × 4 columns) was similarly fragmented. The footer summary row had "GROUPING 1, 2, 3" merged into a single cell header instead of "GROUPING" as header with "1, 2, 3" as the cell value.

**How:** Removed blank lines between rows to form proper contiguous markdown tables. Fixed footer column headers.

### Unformatted subheadings

Several subheadings within sections were plain ALL CAPS text instead of markdown headings. Converted to `####` headings in Title Case:

| Before | After |
|--------|-------|
| `SUPPORTIVE` (under 3.1) | `#### Supportive` |
| `SINGLET (NON-SUPPORTIVE)` (under 3.1) | `#### Singlet (Non-Supportive)` |
| `WRIST` (under 3.9) | `#### Wrist` |
| `KNEE` (under 3.9) | `#### Knee` |
| `IPF RECOGNIZED POWERLIFTING BAR` (under 2.3) | `#### IPF Recognized Powerlifting Bar` |
| `GUIDELINE OF KNURLING DISTANCES` (under 2.3) | `#### Guideline of Knurling Distances` |
| `CHAMPIONSHIPS SCOREBOARD` (under 2.10) | `#### Championships Scoreboard` |
| `SINGLE LIFT BENCH PRESS CHAMPIONSHIPS` (under 4.2.2) | `#### Single Lift Bench Press Championships` |
| `SPEAKER'S CARD` (under 6.1) | `#### Speaker's Card` |
| `ATTEMPT CARDS` (under 6.1) | `#### Attempt Cards` |
| `MEN & WOMEN` (under 1.1) | `**MEN & WOMEN**` (bold label, not a heading) |

### Numbered items joined into run-on lines

The line joining step merged many numbered items (`N)` pattern) into single paragraphs. Affected areas:
- Section 5 (Weighing In) items 6–8 were on one line
- Section 6.3 items 2–4 were joined to the end of the coach paragraph
- Section 6.3 items 5–15 were a single massive paragraph
- Section 6.3 item 16 was joined to the preceding referee signals text

**How:** Manually split at each `N)` boundary.

### Referee items renumbered 1–10 instead of 21–30

The heading conversion step changed items 21–30 in Section 7 (Referees) to `1.`–`10.` markdown ordered list format, losing the original numbering. Restored to `21)`–`30)` per the PDF.

### Basis for Nomination list items merged

In Section 7 item 20(f), the "Basis for Nomination" sub-list had items 2 and 3 merged into `2) Priority ranking as a Category 3. Availability to referee at future international events.` due to line-break position. Split into separate items 2, 3, 4.

### Attempt Cards instructions collapsed

The Attempt Cards instructions (5 fields) were on a single line. Split into a bulleted list. Fixed `3 rd` → `3rd`.

### Championship list missing bullet points

The list of World Championships in item 2 of General Rules was rendered as plain lines without list markers. Added `- ` bullet prefixes to each entry.

### Coach staffing lists collapsed into single lines

The coach-to-athlete ratio lists in Section 6.3 item 1 and Section 10 item 3 were collapsed by line joining into single run-on lines (e.g., `1 Athlete = 3 Coaches 2 Athletes in the same group = 3 Coaches 2 Athletes in two different groups...`). Each list has three sub-lists (warm-up area, preparation/wrapping area Equipped, preparation area Classic) with 4–6 entries each.

**How:** Manually split back into separate lines per the PDF layout. Applied to both Section 6.3 and Section 10 (which duplicates the same content).

### Image placeholder text removed

The PDF contained images (equipment examples labeled NO/YES, bench press position photos labeled GOOD LIFT/NO LIFT, deadlift schematic diagram) that the extractor rendered as placeholder text: `NOYES`, `GOOD LIFT`, `NO LIFT`, `PICTURE A - PROPER STARTING POSITION & SETUP`, and multi-line diagram labels (`SCHEMATIC REPRESENTATION OF THE CORRECT`, `SHOULDER'S ABDUCTION IN THE FINAL POSITION OF THE DEADLIFT`, etc.). All removed.

### Garbled knurling diagram

The PDF extractor mangled the knurling distances diagram into `xxxxxxxxxxxxx xxxxxxxxxxxxx xxxx ... 120` plus `- 160 240 245 440 810 1320`. Replaced with a clean measurement list and diagram note.

### Numbering, placement, and formatting fixes

**Structural / numbering fixes:**

- **Section 1 (General Rules):** Split item 3 off from the end of the paragraph about combined championships, where `3)` was merged inline. Moved the age categories table (previously between items 2 and 3) to sit under the `### 1.1. Age Categories` heading. Moved the MEN weight classes table (previously between item 2 and the paragraph) to sit next to the WOMEN table under `### 1.2. Bodyweight Categories`.
- **Section 2.10 (Scoreboard):** Moved the introductory paragraph (`"A clearly visible and detailed scoreboard..."`) from before the `### 2.10. Scoreboard` heading to after it.
- **Section 3.3 (Supportive Shirts):** Deleted duplicate item `(c)` at line 295 that contained t-shirt wording (`"the t-shirt may be plain"`) copied from the T-Shirt section. Kept the correct `(c)` about supportive shirts.
- **Section 3.6 (Belt):** Removed stray fragment `" of the lifter's nation"` from the end of item (e) about tongue loops.
- **Section 3.7 (Shoes):** Joined split line `"Squat/Bench\nPress/Deadlift."` into a single line.
- **Section 3.8/3.9 (Knee Sleeves / Wraps):** Moved the two-sentence wraps introduction (`"Only wraps of one ply commercially woven elastic..."`) from the bottom of section 3.8 to just after the `### 3.9. Wraps` heading.
- **Section 3.12 (Inspection):** Relabeled second duplicate `(g)` to `(h)`. Split embedded `(i)` onto its own line. Renumbered subsequent items: `(h)` → `(j)`, `(i)` → `(k)`.
- **Section 4.1 (Squat):** Fixed duplicate item `1.` — second `1.` renumbered to `2.`, subsequent items renumbered: `2.` → `3.`, `3.` → `4.`, `4.` → `5.`, `5.` → `6.`.
- **Section 6 (Order of Competition):** Changed cross-reference `"the rules as stated in (m) above"` to `"(l) above"` to match the correct item about third-round deadlift changes. (Later updated to `"(m) above"` when Section 6.1 items were renumbered to fix the duplicate (b) — see below.)
- **Section 7 (Referees), item 20:** Fixed duplicate `(b)` — the second `(b)` (about taking the exam) relabeled to `(c)`, and all subsequent items shifted: `(c)` → `(d)`, `(d)` → `(e)`, `(e)` → `(f)`, `(f)` → `(g)`, `(g)` → `(h)`. Indented the "Basis for Nomination" sub-list (items 1–4) with 3 spaces so they nest under the parent item.
- **Section 10 (Coach Responsibilities):** Split merged items 4–7 from a single run-on line into separate paragraphs. Renumbered items `1.`–`6.` to `8.`–`13.` (continuing from item 7). Converted ALL CAPS inline headings to `####` subheadings in Title Case: `THE AIMS OF A COACH RESPONSIBILITY` → `#### The Aims of a Coach Responsibility`, `WHAT IS THE COACH RESPONSIBILITY?` → `#### What Is the Coach Responsibility?`, `BEING A COACH, YOU MUST ENSURE THE FOLLOWING` → `#### Being a Coach, You Must Ensure the Following`. Renumbered trailing items `7.`–`9.` to `14.`–`16.` (continuing from item 13).

**Formatting improvements (prose → tables):**

- **Section 2.6 (Bench):** Converted bench dimension prose block into a markdown table with columns Dimension and Specification.
- **Section 3.6 (Belt):** Converted numbered belt dimension list into a markdown table with columns Dimension and Maximum.
- **Section 6.3 and Section 10 (Coach staffing ratios):** Converted the three coach-to-athlete ratio lists (warm-up area, preparation/wrapping area Equipped, preparation area Classic) from `N Athlete = N Coaches` line format into markdown tables in both sections.
- **Section 7, item 18 (Referee dress code):** Converted the men/women winter/summer dress code prose into a markdown table with columns Season, Men, Women.

**Readability improvements (item spacing and bold markers):**

- **Lettered items `(a)`–`(z)`:** Bolded all line-start markers to `**(a)**` format and added blank lines between consecutive items. 160 markers bolded across the document. Inline markers within sentences (e.g., weighing-in item 5's clothing list) were left as plain text.
- **Numbered `N)` items:** Bolded all line-start `N)` markers to `**N)**` format and added blank lines between consecutive items. 33 markers bolded across sections 5, 6.3, 7, and 10.
- **Numbered `N.` items:** Added blank lines between consecutive `N.` items in sections 1, 2.3, 4.1, 4.2, 4.3, 6, 7, 8, 9, and 10.
- **First item spacing:** Added a blank line before the first `**(a)**` or `**N)**` marker in each block when preceded by prose (not a blank line or heading). 21 blank lines added.
- **How:** Python scripts matching markers at line start only (preserving inline markers), inserting blank lines by detecting consecutive items with look-back. Added 304 blank lines total.

### Inconsistent sub-item indentation

Lettered sub-items `**(a)**`–`**(h)**` under numbered parent items were inconsistently indented. Some sections (7.6, 7.7, 8.2.9, 9.1) already used 3-space indentation to show hierarchy; others did not.

**Affected areas:**
- Section 6.1 item (p): officials list (a)–(h) under the appointments paragraph
- Section 6.3 item 4: sub-items (a)–(e) for record attempt rules
- Section 6.3 item 5: sub-items (a)–(f) for loading error rules
- Section 6.3 item 16: sub-items (a)–(e) for instant replay rules
- Section 7 item 19: sub-items (a)–(d) for Category 2 qualifications
- Section 7 item 20: sub-items (a)–(h) for Category 1 qualifications, plus "Basis for Nomination" sub-list under (g)
- Section 7 item 21: sub-items (a)–(c) for examination criteria
- Section 7 item 22: sub-items (a)–(b) for testing procedures
- Section 7 item 28: sub-items (a)–(f) for registration
- Section 7 item 29: sub-items (a)–(b) for registrar duties

**How:** Added 3-space indentation to all affected sub-items to match the existing pattern. The "Basis for Nomination" bullet list under item 20(g) was re-indented to 6 spaces (nested under the 3-space parent).

### Inconsistent numbered item format (`N.` vs `**N)**`)

Earlier cleanup steps bolded `N)` markers to `**N)**` in some sections while other items in the same section used plain `N.` format. Standardized all numbered items to `N.` format (the dominant style across the document).

**Affected sections:**
- Section 1: item 3 was `**3)**`, items 1–2 were `N.`
- Section 5: items 6–8 were `**N)**`, items 1–5 were `N.`
- Section 6.3: items 2–16 were `**N)**`, item 1 was `N.`
- Section 7: items 21–30 were `**N)**`, items 1–20 were `N.`
- Section 10: items 4–7 were `**N)**`, items 8–16 were `N.`

**How:** Replaced `**N)** Text` with `N. Text` for all affected items.

### Missing blank line before table (Section 7 item 18)

The referee dress code table immediately followed the `18.` line without a blank line separator, which breaks markdown table rendering in some parsers. Added blank line. Also added missing blank line before item 19.

### Missing blank lines between lettered sub-items and following numbered items

Seven places where a numbered item `N.` immediately followed the last lettered sub-item `**(X)**` without a blank line separator. Added blank lines at:
- Section 1: item 2 after item 1's `**(f)**`
- Section 6.3: item 6 after item 5's `**(f)**`
- Section 7: item 7 after item 6's `**(d)**`, item 8 after item 7's `**(b)**`, item 29 after item 28's `**(f)**`, item 30 after item 29's `**(b)**`
- Section 9.1: item 3 after item 2's `**(i)**`

### Section 6.1 duplicate (b) and missing Attempt Cards table

The PDF has a duplicate `(b)` label in Section 6.1: items (a) and (b) appear, then the Speaker's Card and Attempt Cards forms interrupt the text, then `(b)` appears again for the groups rule instead of continuing as `(c)`. This is a labeling error in the PDF caused by the card illustrations breaking the text flow.

**Fixes:**
- Renumbered the second `(b)` to `(c)` and shifted all subsequent items: (c)→(d), (d)→(e), ..., (p)→(q). Updated internal cross-references: item (f) now references "(j)" instead of "(i)", item (n) references "(m)" instead of "(l)".
- Replaced the Attempt Cards bullet list with a markdown table matching the Speaker's Card treatment. The PDF shows a card form layout; the bullet list was an incomplete representation.

### Remaining spacing around smart quotes

12 instances of smart right double quote (U+201D) followed by a space before punctuation (`.` or `,`), e.g. `"Squat" .` → `"Squat".`, `"synthetic rubber" ,` → `"synthetic rubber",`.

**How:** Python regex replacing `\u201d [.,;:]` → `\u201d[.,;:]`.

### Acute accent used as apostrophe

One instance of acute accent (U+00B4) with surrounding spaces used instead of a right single quote: `Chief Referee ´ s` → `Chief Referee's`.

**How:** Python replace `\u00b4` → `\u2019`, then manual removal of surrounding spaces.

### Spaced character `o r`

`o r 5` → `or 5` in Section 4.1 item 6.

### Typos and capitalization errors in Section 3.14(f)

`T-shrt` → `T-shirt`, `In the mouth` → `in the mouth`, `If deemed` → `if deemed`, `Interfere` → `interfere`, `It to be` → `it to be`.

### Parenthetical spacing artifacts

- `( "quick release" referring to lever .)` → `("quick release" referring to lever.)` in Section 3.6(d).
- `etc .)` → `etc.)` in Section 10 item 9.

### Stutter artifact `c communication`

`facilitates c communication` → `facilitates communication` in Section 10 item 10.

### Missing possessive apostrophes and spaces

- `referees'decision` → `referees' decision` (Section 8.1 item 9)
- `arms'length` → `arms' length` (Section 4.2 items 7-8, two occurrences)
- `lifters'best` → `lifters' best` (Section 6.1(c))
- `referees'cards` → `referees' cards` (Section 8.1 item 14)
- `lifters names` → `lifters' names` (Section 2.10)

### Missing terminal punctuation

Added missing periods at end of sentences in: Section 1 item 12, Section 2.10, Section 4.1 item 6 (line break), Section 4.2.1 item 11, Section 4 item 4 (joined continuation), Section 7 items 5 and 17, Section 8.1 items 10 and 12, Section 8.2 items 3 and 8, Section 10 items 4-8, Section 3.9 Wrist.

### Missing colon on orphaned fragment

`In competitions in which both sexes are competing` → `In competitions in which both sexes are competing:` (Section 1 item 1).

### Mid-paragraph line breaks

- `floor. The\nsurface` → `floor. The surface` (Section 2.2)
- `designated as an\nEquipped` → `designated as an Equipped` (Section 3.1)
- `2.5 kg.\nUnless attempts` → `2.5 kg. Unless attempts` (Section 6.3 item 4)

### Missing `ed` suffix

`properly dress` → `properly dressed` (Section 10 item 5).

### Section 1 item reordering

PDF item 5 (eight competitors per nation) was displaced in the PDF extraction -- it appeared between items 11 and 12 instead of between items 4 and 6. Moved to correct position (now item 2 in the renumbered sequence) and renumbered subsequent items. Updated cross-reference in item 6 from "item 11" to "item 8" (team awards).

### Unicode normalization

Replaced all Unicode typographic characters with ASCII equivalents:
- 61 pairs of smart double quotes (U+201C/U+201D) -> straight double quotes (`"`)
- 104 right single quotes (U+2019) -> straight apostrophes (`'`)
- 5 en dashes (U+2013) -> hyphens (`-`)
- 1 em dash (U+2014) -> double hyphens (`--`)

### Continuation lines under numbered items not indented

Text, sub-items (`**(a)**`-`**(f)**`), tables, and examples that belong to a numbered parent item (e.g., `1.`, `10.`) were at root indentation level instead of being indented 3 spaces to nest under the parent in markdown. Affected areas include Section 1 item 1 (sub-items a-f and "In competitions..." line), item 2 (bullet list and continuation paragraph), item 10 (dress code continuation), Section 5 item 5 (amputee table and continuation), Section 6.3 item 1 (coach tables and paragraphs), item 4 (examples), item 15 (referee signals table), item 18 (dress code table), and various sub-item blocks throughout Sections 6-9.

**How:** Python script detecting un-indented non-blank lines following a numbered item start, adding 3-space indentation to all continuation lines until the next numbered item or heading. One false positive corrected: Section 2.3 `**(b)** Discs shall conform` is a root-level lettered item, not a sub-item of the preceding numbered list.

## Known remaining artifacts

- Spaced hyphens used as dashes/separators (e.g., `Length - not less than`) are left as-is since they function as list separators in the PDF, not compound word hyphens.
- Section 7 item 17: `three four referees` is the actual text in the PDF -- verified against the source PDF, not an extraction artifact.
