# LM Dashboard

## Tagline

A self-hosted start page and uptime monitor for the links on my network, styled after old line-mode terminals.

## Short description

For a project card or list view.

LM Dashboard keeps the links on my home network on one page and checks each one on its own schedule. Every row shows the HTTP status, the response time, and a graph of the last 20 checks. It comes with dark and light themes plus a green-screen Classic theme. Built with Blazor Server on .NET 10.

## Full description

LM Dashboard is the start page I use for the services I run at home. Each link is marked internal or external, and the dashboard checks it with an HTTP GET on an interval I set per link. A row shows the status code, how long the server took to answer, and a small bar graph of the last 20 checks, with failed checks in red. The header counts how many links are up, redirecting, down, waiting for a first check, or paused. When something breaks, the Failing only filter cuts the list down to the links that need attention.

The project started when I came across CERN's simulation of the first line-mode browser. It took me back to weekends at my dad's office at IBM, where he wrote COBOL for System/370 mainframes and I played Trek for hours. The Classic theme is that green-phosphor look, with optional scanlines, a vignette, and a brightness slider. The dark and light themes keep the monospace data and the blinking cursor and leave out the CRT effects.

## Features

- Internal and external links, grouped by type or filtered, with search by name or URL
- A check interval for each link, down to 1 second. New internal links start at 30 seconds and external links at 5 minutes.
- Redirects aren't followed, so a 301 or 302 shows up as a redirect instead of as the status of the page it points to.
- Internal links accept self-signed certificates, which home-lab services often use. External links get full certificate validation, so an expired or invalid certificate shows up as a failure.
- Links and settings are saved as JSON files. Check history lives in memory and starts over when the app restarts.
- The app only answers requests addressed to host names and networks on an allowlist, which stops other websites from reaching it through DNS rebinding.
- Runs on Windows or Linux, either on an installed .NET runtime or as a self-contained build under systemd.

## How it works

A background service wakes once a second and starts a check for every link whose interval has passed. It schedules from when the last check started, so a slow server doesn't stretch the interval.

Checks go through two named HTTP clients. The internal one skips certificate validation and the external one keeps it. Neither follows redirects, and both give up after 10 seconds. The status is recorded as soon as the response headers arrive, so the response time doesn't include downloading the page.

Checks can update the store several times a second. The page batches those changes into one refresh every 250 ms, and each row tracks a version number so Blazor only re-renders the rows whose link changed. The search box waits for a pause in typing before it sends anything to the server, and the brightness slider previews in the browser and sends only the final value when you release it.

## Built with

C#, .NET 10, ASP.NET Core Blazor Server with interactive server rendering, and plain CSS. The fonts are IBM Plex Sans and IBM Plex Mono.

Source: https://github.com/jmwny/LMDashboard (MIT License)

## Screenshots

All images are captured at 2x for high-density screens. The mobile shots are at 3x.

| File | Caption | Alt text |
| --- | --- | --- |
| `01-dashboard-dark.png` | The default dark theme, with links grouped into internal and external. | LM Dashboard in its dark theme, listing home-lab services with their status, response time, and a bar graph of recent checks. Two services show failed checks in red. |
| `01-dashboard-dark-full.png` | The full dashboard in the dark theme. | The full LM Dashboard page in the dark theme, showing nine internal links and five external links. |
| `02-dashboard-light.png` | The light theme. | LM Dashboard in its light theme, with the same links and status graphs on a light background. |
| `02-dashboard-light-full.png` | The full dashboard in the light theme. | The full LM Dashboard page in the light theme. |
| `03-dashboard-classic.png` | The Classic theme: green text on black, with scanlines. | LM Dashboard in the Classic theme, with green monospace text on black, bracketed buttons, and faint scanlines. |
| `03-dashboard-classic-full.png` | The full dashboard in the Classic theme, with the brightness, scanline, and vignette controls in the footer. | The full LM Dashboard page in the Classic theme, with a brightness slider and scanline and vignette toggles at the bottom. |
| `04-edit-link-dialog.png` | Editing a link: name, URL, type, check interval, and monitoring. | The Edit link dialog over the dashboard, with fields for name, URL, internal or external type, check interval in seconds, and a monitoring checkbox. |
| `05-failing-only.png` | The Failing only filter shows the links that need attention. | LM Dashboard filtered to failing links: a backup server that is unreachable and a Nextcloud server returning 503 Service Unavailable. |
| `06-mobile.png` | The layout on a phone. | LM Dashboard on a phone-sized screen, with the toolbar wrapped onto several lines and each link's actions below its status. |
| `06-mobile-full.png` | The full page on a phone. | The full LM Dashboard page on a phone-sized screen. |
| `07-social-card-1200x630.png` | Link preview image (1200 × 630 at 2x). | LM Dashboard in its dark theme, showing the status summary, toolbar, and the first several internal links. |
