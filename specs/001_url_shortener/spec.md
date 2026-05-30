# Feature Specification: Url Shortener

[↑ Back to specs index](../specs.md)

**Feature Branch**: `001-url-shortener`
**Created**: 2026-05-22
**Status**: Draft
**Input**: "simple url shortener feature, user paste a link get a shorten version based on the current domain plus a random string that must be unique"

## User Scenarios & Testing

> Stories are prioritized (P1 highest). Each must be **independently testable** — implementing one story alone should still deliver a usable slice.
> Each story must satisfy **INVEST**: Independent, Negotiable, Valuable, Estimable, Small, Testable.

### User Story 1 — Shorten a URL and follow it end-to-end (Priority: P1)

An anonymous visitor opens the web app, pastes a long `http://` or `https://` URL into a single-field form, and submits it. The service generates a 6-character random alphanumeric code that is unique across all stored links, persists the mapping, and returns the resulting short URL (`<current-domain>/<code>`) to the UI. Anyone — the same user, or someone the link was shared with — can then visit `<current-domain>/<code>` and be redirected to the original long URL.

**Why this priority**: This is the entire MVP. Without both halves (shorten and follow), the feature has zero user value. Polish and error handling layer on top.

**Independent Test**: Submit a valid long URL via the web UI; confirm the response contains a short URL ending in a 6-character `[a-zA-Z0-9]` code. Then issue a GET to that short URL and confirm the response is an HTTP redirect (301 or 302) whose `Location` header matches the original long URL.

**Acceptance Scenarios**:

1. **Given** the shortener web page is loaded and the database contains no record for the URL `https://example.com/some/long/path?q=1`, **When** the user pastes that URL into the form and submits, **Then** the service responds with HTTP 201 and a JSON body containing a `short_url` field of the form `https://<current-domain>/<code>` where `<code>` matches `^[a-zA-Z0-9]{6}$`, and the UI displays that short URL.
2. **Given** a short code `aB3xZ9` already exists in storage mapped to `https://example.com/target`, **When** any client issues `GET /aB3xZ9`, **Then** the service responds with an HTTP redirect (301 or 302) whose `Location` header equals `https://example.com/target`.
3. **Given** the same long URL `https://example.com/foo` has been shortened previously and a record already exists, **When** the user submits `https://example.com/foo` again, **Then** the service generates a brand-new code distinct from the previous one and stores a second mapping row.

---

### User Story 2 — Reject invalid input with a clear error (Priority: P2)

A user submits something that isn't a well-formed `http://` or `https://` URL — empty input, a bare word, a `javascript:` URL, malformed syntax. The service rejects the submission with HTTP 400 and a human-readable error message that the UI surfaces inline.

**Why this priority**: The happy path (P1) is the MVP. Robust validation prevents broken redirects and obvious abuse vectors but doesn't change what the feature *is*. Ships right after P1.

**Independent Test**: For each of these inputs — empty string, `not a url`, `javascript:alert(1)`, `ftp://example.com`, `https://` (no host) — POST to the shorten endpoint and confirm the response is HTTP 400 with a JSON body containing an `error` field whose value is a non-empty string. Confirm no row is added to storage.

**Acceptance Scenarios**:

1. **Given** the shortener web page is loaded, **When** the user submits an empty long URL, **Then** the service responds HTTP 400 with `{"error": "<reason>"}` and the UI shows the error next to the form field without clearing existing text.
2. **Given** the shortener web page is loaded, **When** the user submits `javascript:alert(1)`, **Then** the service responds HTTP 400 with an error message indicating only `http://` and `https://` URLs are accepted.
3. **Given** the shortener web page is loaded, **When** the user submits a string with no URL scheme such as `example.com/path`, **Then** the service responds HTTP 400 and the UI surfaces the message.

---

### User Story 3 — Copy the short URL with one click (Priority: P3)

After a successful shorten, the UI shows the resulting short URL in a styled display next to a "Copy" button. Clicking the button copies the short URL to the system clipboard and gives visible confirmation (e.g., the button label changes to "Copied!" for a moment).

**Why this priority**: Pure UX polish on top of the working P1 loop. The feature is usable without it (users can select+copy manually), but every comparable product includes it.

**Independent Test**: With a previously-generated short URL displayed in the result area, click the "Copy" button. Confirm the clipboard now contains the short URL text and that a visible confirmation appears for at least 1 second.

**Acceptance Scenarios**:

1. **Given** the UI is showing a freshly generated short URL `https://<current-domain>/aB3xZ9`, **When** the user clicks the "Copy" button next to it, **Then** the browser clipboard contains the exact string `https://<current-domain>/aB3xZ9` and the button briefly shows a "Copied!" confirmation.

---

### Edge Cases

- The random code generator produces a code that already exists in storage. The service must detect the collision and retry with a new code until a unique one is found, without surfacing the retry to the user.
- A long URL exceeds the configured maximum length. The service must reject it as invalid (HTTP 400) rather than silently truncating.
- Two requests concurrently shorten the same long URL. Each must receive its own distinct code (the "always new code" rule) and both rows must persist without losing either.
- A visitor requests a short code in the wrong character set (e.g., `GET /aB3xZ-` with a hyphen) or wrong length. The service responds 404 (does not exist) without attempting a lookup.
- A long URL contains query strings, fragments, percent-encoding, or unicode in the path/host. The service preserves the original URL byte-for-byte on redirect.

## Requirements

### Functional Requirements

> Written in EARS notation.

- **FR-001**: Users MUST be able to submit a long URL through the web UI.
- **FR-002**: System MUST validate that the submitted long URL is a non-empty, well-formed absolute URL whose scheme is `http` or `https`.
- **FR-003**: If the submitted long URL fails validation, System MUST respond HTTP 400 with a JSON body of the form `{"error": "<human-readable reason>"}` and MUST NOT persist anything.
- **FR-004**: When a valid long URL is submitted, System MUST generate a random short code that is exactly 6 characters long and drawn from the alphabet `[a-zA-Z0-9]`.
- **FR-005**: System MUST guarantee that every persisted short code is unique across all stored mappings; on collision during generation, System MUST retry until a unique code is produced.
- **FR-006**: When a valid long URL is submitted, System MUST persist the mapping (short code, long URL, creation timestamp) durably before responding.
- **FR-007**: When a valid long URL is submitted, System MUST respond HTTP 201 with a JSON body containing the resulting `short_url` composed as `<current-domain>/<code>`, where `<current-domain>` is the public origin the service is served from.
- **FR-008**: Each shorten request MUST generate a new short code, even if the submitted long URL has been shortened previously — the service MUST NOT deduplicate on long URL.
- **FR-009**: When a client issues an HTTP GET to `<current-domain>/<code>` for an existing code, System MUST respond with an HTTP redirect (301 or 302) whose `Location` header is the stored long URL, byte-for-byte.
- **FR-010**: If the requested short code does not exist in storage, System MUST respond HTTP 404 with a simple "not found" message.
- **FR-011**: System MUST NOT expire, delete, or modify stored short links automatically.
- **FR-012**: System MUST reject long URLs that exceed a configured maximum length [NEEDS CLARIFICATION: maximum long URL length, default 2048 chars].
- **FR-013**: After a successful shorten, the Web UI MUST display the resulting short URL alongside a Copy button that places the short URL on the system clipboard and gives visible confirmation.
- **FR-014**: System MUST log and surface backend errors per the project constitution's "fail loudly" principle rather than silently returning a 500 with no detail in server logs.

### Key Entities

- **Link**: A persisted mapping from a generated short code to a long URL.
  - `short_code`: 6-character string drawn from `[a-zA-Z0-9]`. Unique across all rows. Primary identifier.
  - `long_url`: The original URL submitted by the user. Stored exactly as accepted (after validation).
  - `created_at`: Timestamp when the mapping was persisted.

## Success Criteria

Measurable, technology-agnostic outcomes.

- **SC-001**: 95th-percentile latency for a successful shorten request (valid input through HTTP 201 response) is under 200 ms at up to 100 concurrent requests per second.
- **SC-002**: 95th-percentile latency for a redirect lookup (`GET /<code>` through `301`/`302` response) is under 100 ms at up to 100 requests per second.
- **SC-003**: The system stores and serves at least 100,000 short links without any measurable degradation in SC-001 or SC-002.
- **SC-004**: At the 100,000-link scale, the rate of short-code generation attempts that hit a collision and require a retry stays below 0.001%.
- **SC-005**: 100% of submissions that fail validation receive an HTTP 400 with a non-empty `error` field; zero such submissions cause a row to be written to storage.
- **SC-006**: A first-time user can paste a URL, receive a short URL, and copy it to the clipboard in under 10 seconds end-to-end on the web UI.
