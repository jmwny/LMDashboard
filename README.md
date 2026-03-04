# LM Dashboard

A locally hosted web link organizer and uptime monitor with a line-mode terminal aesthetic — Blazor Server, .NET 10.

<img width="1102" height="619" alt="image" src="https://github.com/user-attachments/assets/f2a9af38-fc15-47b3-903a-09f6d03a3262" />

---

## Background

The inspiration for this primarily comes from stumbling across a simulation of the first line-mode browser at [line-mode.cern.ch](https://line-mode.cern.ch/www/hypertext/WWW/TheProject.html), and this brought back a lot of memories.

My dad used to work for IBM back in the day on System/370 mainframes as a COBOL programmer. When I was much younger, he'd take me into the office on weekends and I'd sit and play Trek for hours on end... and get myself into trouble de-spooling random magnetic tape reels... but anyway.

I've also been wanting to write my own locally hosted web link organizer as a start page. I've been putting this off for quite some time as other projects just always seemed to take higher precedence.

The idea to combine the two is more than likely the result of nostalgia. Things just felt so new and fanciful back then... and I miss my dad. I miss those times we spent together in that cold, air-conditioned room while blowing up Klingons.

I really don't expect anyone to actually use this other than myself, and the rendering of how those old displays actually looked is far from true (colors? only if it's green!). I suppose it's really just a way to hold on to a little bit of something from decades past.

I'm not really sure how much further I'm going to take this project as it fits my needs for the time being.

---

## What it does

- **Link Management** — Add, edit and delete URLs with classification type (Internal / External).
- **Monitoring** — "Ping's" each link on a configurable interval and displays live HTTP status codes with response times.
- **Status Summary** — Displays combined OK FAIL REDIR PINGING WAITING states.
- **Filtering and Search** — Filters by type, grouped view, failing links, and text search by name or URL.
- **Effects** — Scanlines, vignette and randomized power surge/brownout animation with configurable timing.
- **Brightness Control** — Slider to dial in the phosphor brightness.

## Install

Here is the services file that I'm using; configure as needed and replace [XXX] with your system/install specific values. I also wouldn't expose this externally as it was written as an internal only tool.

```
[Unit]
Description=LMDashboard - Web Link Organizer
After=network.target

[Service]
WorkingDirectory=[INSTALL/BINARY DIR]
ExecStart=/usr/share/dotnet/dotnet [INSTALL/BINARY DIR]/LMDashboard.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=lmdashboard
User=[USER]
Group=[GROUP]

# Environment
# Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false
Environment=DOTNET_ROOT=/usr/share/dotnet
Environment=ASPNETCORE_URLS=http://0.0.0.0:5000

# Security hardening
NoNewPrivileges=true
PrivateTmp=true

[Install]
WantedBy=multi-user.target
```
