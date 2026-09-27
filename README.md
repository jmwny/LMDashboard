# LM Dashboard

A locally hosted start page and uptime monitor for the links on your network, with a nod to old line-mode terminals. Built with Blazor Server on .NET 10.

![The dashboard in its dark theme, listing internal and external links with their status, response time, and recent checks](docs/screenshot.png)

## Background

The inspiration for this primarily comes from stumbling across a simulation of the first line-mode browser at [line-mode.cern.ch](https://line-mode.cern.ch/www/hypertext/WWW/TheProject.html), and this brought back a lot of memories.

My dad used to work for IBM back in the day on System/370 mainframes as a COBOL programmer. When I was much younger, he'd take me into the office on weekends and I'd sit and play Trek for hours on end... and get myself into trouble de-spooling random magnetic tape reels... but anyway.

I've also been wanting to write my own locally hosted web link organizer as a start page. I've been putting this off for quite some time as other projects just always seemed to take higher precedence.

The idea to combine the two is more than likely the result of nostalgia. Things just felt so new and fanciful back then... and I miss my dad. I miss those times we spent together in that cold, air-conditioned room while blowing up Klingons.

I really don't expect anyone to actually use this other than myself, and the rendering of how those old displays actually looked is far from true (colors? only if it's green!). I suppose it's really just a way to hold on to a little bit of something from decades past.

I'm not really sure how much further I'm going to take this project as it fits my needs for the time being.

## What it does

- Keeps your links on one page, each marked as internal or external. Clicking a link opens it in a new tab.
- Checks each link on its own interval and shows the HTTP status, the response time, and a graph of the last 20 checks, with failed checks in red.
- Shows at the top how many links are up, redirecting, down, waiting for a first check, or paused.
- Filters by type, groups internal and external links, shows only failing links, and searches by name or URL.
- Comes with dark, light, and Classic themes. Classic is the green-phosphor look the project started with, with optional scanlines and vignette and a brightness slider.

## How checks work

Each check is an HTTP GET. The dashboard records the status code as soon as the response headers arrive and gives up after 10 seconds.

- Redirects aren't followed, so a 301 or 302 shows up as a redirect instead of as the status of the page it points to.
- Internal links accept self-signed certificates, which home-lab services often use. External links get full certificate validation, so an expired or invalid certificate shows up as a failure.
- New links are checked every 30 seconds if they're internal and every 5 minutes if they're external. You can change the interval for each link, down to 1 second.

Check history lives in memory, so it starts over when the app restarts. Links and settings are saved to disk.

## Running it locally

You'll need the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

```bash
git clone https://github.com/jmwny/LMDashboard.git
cd LMDashboard
dotnet run --project LMDashboard --launch-profile http
```

Then open http://localhost:5264.

Links and display settings are saved as `links.json` and `preferences.json` in a `Data` folder under the app's content root. With `dotnet run` that's `LMDashboard/Data`. For a published build it's the folder the app runs from.

## Allowed addresses

The dashboard only answers requests addressed to a host name or IP address you've allowed. This keeps a malicious website from reaching it through your browser with DNS rebinding. Both lists are in `appsettings.json`:

```json
"HostAccess": {
  "Hosts": [ "localhost" ],
  "Networks": [ "10.5.1.0/24", "127.0.0.0/8", "::1/128" ]
}
```

Change `Networks` to match your LAN, and add any host name you use to reach the dashboard to `Hosts`. Requests to any other address get a 400 error.

## Installing on Linux

The server needs the ASP.NET Core 10 runtime. Publish the app to the folder you'll run it from:

```bash
dotnet publish LMDashboard -c Release -o [INSTALL/BINARY DIR]
```

Here's the systemd service file I use. Replace the [BRACKETED] values with ones for your system, and make sure `[USER]` can write to the install folder, since that's where the `Data` folder goes.

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

Save it as `/etc/systemd/system/lmdashboard.service`, then start it:

```bash
sudo systemctl daemon-reload
sudo systemctl enable --now lmdashboard
```

There's no login, so anyone who can reach the dashboard can add, edit, and delete links. I wrote it as an internal-only tool and wouldn't expose it to the internet.

## Fonts

The app bundles IBM Plex Sans and IBM Plex Mono, both under the SIL Open Font License. See [OFL.txt](LMDashboard/wwwroot/fonts/OFL.txt).
